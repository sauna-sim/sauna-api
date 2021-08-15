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
        private const double MIN_INTERCEPT_ANGLE = 0.1;
        private const double MAX_INTERCEPT_ANGLE = 45;
        private const double MIN_XTK_THRESHOLD_M = 3;
        private const double MAX_INTERCEPT_XTK_M = 1852;

        private double previousTrack;
        private double previousGroundSpeed;

        private double leadInDistance;
        private GeoPoint intersection;
        private double requiredTrueCourse;
        private double trackToHold = -1;
        private double xTk = 0;
        private double _magneticCourse = -1;
        private double _trueCourse = -1;
        private TrackHoldInstruction instr;

        public Waypoint AssignedWaypoint { get; private set; }

        public double MagneticCourse
        {
            get => _magneticCourse;
            private set
            {
                _magneticCourse = AcftGeoUtil.NormalizeHeading(Math.Round(value, 1, MidpointRounding.AwayFromZero));

                // Calculate True Course
                Coordinate coord = new Coordinate(AssignedWaypoint.Latitude, AssignedWaypoint.Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                _trueCourse = AcftGeoUtil.NormalizeHeading(_magneticCourse + declin);
            }
        }

        public double TrueCourse
        {
            get => _trueCourse;
            set
            {
                _trueCourse = Math.Round(value, 1, MidpointRounding.AwayFromZero);

                // Calculate Magnetic Course
                Coordinate coord = new Coordinate(AssignedWaypoint.Latitude, AssignedWaypoint.Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                _magneticCourse = AcftGeoUtil.NormalizeHeading(_trueCourse - declin);
            }
        }

        public InterceptCourseInstruction(Waypoint assignedWp)
        {
            AssignedWaypoint = assignedWp;
        }

        public InterceptCourseInstruction(Waypoint assignedWp, double magneticCourse) : this(assignedWp)
        {
            MagneticCourse = magneticCourse;
        }

        public LateralControlMode Type => LateralControlMode.COURSE_INTERCEPT;

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            if (_trueCourse < 0)
            {
                return false;
            }

            UpdateInfo(position);

            if (previousTrack != position.Track_True || previousGroundSpeed != position.GroundSpeed)
            {
                previousTrack = position.Track_True;
                previousGroundSpeed = position.GroundSpeed;
                intersection = AcftGeoUtil.FindIntersection(position, AssignedWaypoint, _trueCourse);

                // Find degrees to turn
                double theta = Math.Abs(AcftGeoUtil.CalculateTurnAmount(previousTrack, _trueCourse));

                // Calculate radius of turn
                double r = AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(previousGroundSpeed, 25, 3), previousGroundSpeed);

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

        private void UpdateInfo(AcftData pos)
        {
            if (_trueCourse < 0)
            {
                return;
            }

            // Check cross track error
            GeoPoint aircraftPoint = new GeoPoint(pos.Latitude, pos.Longitude, pos.AbsoluteAltitude);
            xTk = AcftGeoUtil.CalculateCrossTrackErrorM(aircraftPoint, new GeoPoint(AssignedWaypoint.Latitude, AssignedWaypoint.Longitude), _trueCourse, ref requiredTrueCourse);
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            if (_trueCourse < 0)
            {
                return;
            }

            UpdateInfo(position);

            if (trackToHold == -1)
            {
                trackToHold = requiredTrueCourse;
            }

            if (instr == null)
            {
                instr = new TrackHoldInstruction(trackToHold);
            }

            // Calculate track difference
            double trackDiff = AcftGeoUtil.CalculateTurnAmount(position.Track_True, trackToHold);

            // Calculate course difference
            double courseDiff = AcftGeoUtil.CalculateTurnAmount(trackToHold, requiredTrueCourse);

            // Intercept if the conditions are met
            if (Math.Abs(courseDiff) > Double.Epsilon && ShouldActivateInstruction(position, posCalcInterval))
            {
                trackToHold = requiredTrueCourse;
                instr = new TrackHoldInstruction(trackToHold);
            }
            else if (Math.Abs(xTk) > MIN_XTK_THRESHOLD_M && Math.Abs(trackDiff) <= Double.Epsilon &&
              (Math.Abs(courseDiff) < Double.Epsilon || (courseDiff < 0 && xTk > 0) || (courseDiff > 0 && xTk < 0)))
            {
                // Recalculate intercept course
                double offset = Math.Round(Math.Min((Math.Abs(xTk) / MAX_INTERCEPT_XTK_M) * MAX_INTERCEPT_ANGLE, MAX_INTERCEPT_ANGLE), 1, MidpointRounding.AwayFromZero);

                if (xTk > 0)
                {
                    trackToHold = AcftGeoUtil.NormalizeHeading(requiredTrueCourse - offset);
                }
                else
                {
                    trackToHold = AcftGeoUtil.NormalizeHeading(requiredTrueCourse + offset);
                }
                instr = new TrackHoldInstruction(trackToHold);
            }

            instr.UpdatePosition(ref position, posCalcInterval);
        }

        public override string ToString()
        {
            return $"NAV {AssignedWaypoint.Identifier} - {_magneticCourse:000.0} | {_trueCourse:000.0} | {requiredTrueCourse:000.0} | {trackToHold:000.0} | {xTk:0.0}m";
        }
    }
}
