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
    public class InterceptCourseInstruction : ILateralControlInstruction
    {
        private double previousTrack;
        private double leadInDistance;
        private GeoPoint intersection;
        public Waypoint AssignedWaypoint { get; private set; }
        public double MagneticCourse { get; private set;}

        public double TrueCourse { get; set; }

        public InterceptCourseInstruction(Waypoint assignedWp, double course)
        {
            AssignedWaypoint = assignedWp;
            MagneticCourse = course;
        }

        public LateralControlMode Type => LateralControlMode.COURSE_INTERCEPT;

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            if (previousTrack != position.Track_True)
            {
                // Get true course
                Coordinate coord = new Coordinate(position.Latitude, position.Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, position.IndicatedAltitude / 3.28084, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                TrueCourse = AcftGeoUtil.NormalizeHeading(MagneticCourse + declin);

                previousTrack = position.Track_True;
                intersection = AcftGeoUtil.FindIntersection(position, AssignedWaypoint, TrueCourse);

                // Find degrees to turn
                double theta = Math.Abs(AcftGeoUtil.CalculateTurnAmount(position.Track_True, TrueCourse));

                // Calculate radius of turn
                double r = AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(position.GroundSpeed, 25, 3), position.GroundSpeed);

                leadInDistance = AcftGeoUtil.CalculateTurnLeadDistance(intersection, theta, r);
            }

            if (leadInDistance < 0)
            {
                return false;
            }

            GeoPoint aircraftPoint = new GeoPoint(position.Latitude, position.Longitude, position.AbsoluteAltitude);
            aircraftPoint.MoveByNMi(position.Track_True, AcftGeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval));

            double dist = GeoPoint.FlatDistanceNMi(aircraftPoint, intersection);

            return dist < leadInDistance;
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            new TrackHoldInstruction(TrueCourse).UpdatePosition(ref position, posCalcInterval);
        }

        public override string ToString()
        {
            return $"NAV {AssignedWaypoint.Identifier} - {MagneticCourse} ({leadInDistance})";
        }
    }
}
