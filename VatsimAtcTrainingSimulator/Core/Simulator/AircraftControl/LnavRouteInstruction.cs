using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class LnavRouteInstruction : ILateralControlInstruction
    {
        public LateralControlMode Type => LateralControlMode.NAV_ROUTE;

        public InterceptCourseInstruction Instruction { get; private set; }
        private InterceptCourseInstruction nextInstruction;
        private double _initMagneticCourse = -1;
        private double _initTrueCourse = -1;

        public double InitialMagneticCourse
        {
            get => _initMagneticCourse;
            private set
            {
                _initMagneticCourse = AcftGeoUtil.NormalizeHeading(Math.Round(value, 1, MidpointRounding.AwayFromZero));

                _initTrueCourse = -1;
            }
        }

        public double InitialTrueCourse
        {
            get => _initTrueCourse;
            set
            {
                _initTrueCourse = AcftGeoUtil.NormalizeHeading(Math.Round(value, 1, MidpointRounding.AwayFromZero));

                _initMagneticCourse = -1;
            }
        }

        public LnavRouteInstruction()
        {

        }

        public LnavRouteInstruction(double initialMagneticCourse)
        {
            _initMagneticCourse = AcftGeoUtil.NormalizeHeading(Math.Round(initialMagneticCourse, 1, MidpointRounding.AwayFromZero));
        }

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            if (position.Route.Count > 0)
            {
                // Get first waypoint
                Waypoint wp = DataHandler.GetClosestWaypointByIdentifier(position.Route.First.Value, position.Latitude, position.Longitude);

                if (wp != null && (Instruction == null || Instruction.AssignedWaypoint != wp))
                {
                    if (_initMagneticCourse < 0 && _initTrueCourse < 0)
                    {
                        return false;
                    }
                    else if (_initTrueCourse < 0)
                    {
                        // Calculate True Course
                        Coordinate coord = new Coordinate(wp.Latitude, wp.Longitude, DateTime.UtcNow);
                        Magnetic m = new Magnetic(coord, DataModel.WMM2015);
                        double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                        _initTrueCourse = AcftGeoUtil.NormalizeHeading(_initMagneticCourse + declin);                        
                    }
                    else if (_initMagneticCourse < 0)
                    {
                        // Calculate Magnetic Course
                        Coordinate coord = new Coordinate(wp.Latitude, wp.Longitude, DateTime.UtcNow);
                        Magnetic m = new Magnetic(coord, DataModel.WMM2015);
                        double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                        _initMagneticCourse = AcftGeoUtil.NormalizeHeading(_initTrueCourse - declin);
                    }

                    Instruction = new InterceptCourseInstruction(wp)
                    {
                        TrueCourse = _initTrueCourse
                    };
                }
            }

            if (Instruction == null)
            {
                return false;
            }

            return Instruction.ShouldActivateInstruction(position, posCalcInterval);
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            if (Instruction == null)
            {
                if (!ShouldActivateInstruction(position, posCalcInterval))
                {
                    return;
                }
            }

            // Queue next leg up
            if (position.Route.Count < 2)
            {
                nextInstruction = null;
            }
            else
            {
                Waypoint wp;
                do
                {
                    // Get next waypoint
                    wp = DataHandler.GetClosestWaypointByIdentifier(position.Route.First.Next.Value, position.Latitude, position.Longitude);

                    if (wp == null)
                    {
                        position.Route.Remove(position.Route.First.Next);
                    }
                } while (position.Route.Count >= 2 && wp == null);

                if (wp == null)
                {
                    nextInstruction = null;
                }
                else if (nextInstruction == null || nextInstruction.AssignedWaypoint != wp)
                {
                    nextInstruction = new InterceptCourseInstruction(wp)
                    {
                        TrueCourse = GeoPoint.FinalBearing(
                        new GeoPoint(Instruction.AssignedWaypoint.Latitude, Instruction.AssignedWaypoint.Longitude),
                        new GeoPoint(wp.Latitude, wp.Longitude))
                    };
                }
            }

            // Decide whether or not to activate next leg.
            if (nextInstruction != null && nextInstruction.ShouldActivateInstruction(position, posCalcInterval))
            {
                // Remove first waypoint
                if (position.Route.Count > 0 && position.Route.First.Value == Instruction.AssignedWaypoint.Identifier)
                {
                    position.Route.RemoveFirst();
                }
                Instruction = nextInstruction;
            }

            // Update position
            Instruction.UpdatePosition(ref position, posCalcInterval);
        }

        public override string ToString()
        {
            if (Instruction != null)
            {
                return Instruction.ToString();
            }
            return "NAV Route";
        }
    }
}
