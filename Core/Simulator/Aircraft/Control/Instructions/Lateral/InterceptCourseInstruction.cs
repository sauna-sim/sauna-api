using AviationSimulation.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
{
    public class InterceptCourseInstruction : ILateralControlInstruction
    {
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
        private double aTk = 0;
        private double _magneticCourse = -1;
        private double _trueCourse = -1;
        private TrackHoldInstruction instr;

        public IRoutePoint AssignedWaypoint { get; private set; }

        public double MagneticCourse
        {
            get => _magneticCourse;
            private set
            {
                _magneticCourse = GeoUtil.NormalizeHeading(Math.Round(value, 1, MidpointRounding.AwayFromZero));

                // Calculate True Course
                _trueCourse = GeoUtil.MagneticToTrue(_magneticCourse, AssignedWaypoint.PointPosition);
            }
        }

        public double TrueCourse
        {
            get => _trueCourse;
            set
            {
                _trueCourse = GeoUtil.NormalizeHeading(Math.Round(value, 1, MidpointRounding.AwayFromZero));

                // Calculate Magnetic Course
                _magneticCourse = GeoUtil.TrueToMagnetic(_trueCourse, AssignedWaypoint.PointPosition);
            }
        }

        public double AlongTrackM => aTk;

        public InterceptCourseInstruction(IRoutePoint assignedWp)
        {
            AssignedWaypoint = assignedWp;
        }

        public InterceptCourseInstruction(IRoutePoint assignedWp, double magneticCourse) : this(assignedWp)
        {
            MagneticCourse = magneticCourse;
        }

        public LateralControlMode Type => LateralControlMode.COURSE_INTERCEPT;

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            if (_trueCourse < 0)
            {
                return false;
            }

            UpdateInfo(position, ref fms);

            if (previousTrack != position.Track_True || previousGroundSpeed != position.GroundSpeed)
            {
                previousTrack = position.Track_True;
                previousGroundSpeed = position.GroundSpeed;
                intersection = GeoUtil.FindIntersection(position.PositionGeoPoint, AssignedWaypoint.PointPosition, previousTrack, _trueCourse);

                // Find degrees to turn
                double theta = Math.Abs(GeoUtil.CalculateTurnAmount(previousTrack, _trueCourse));

                // Calculate radius of turn
                double r = GeoUtil.CalculateRadiusOfTurn(GeoUtil.CalculateBankAngle(previousGroundSpeed, 25, 3), previousGroundSpeed);

                leadInDistance = GeoUtil.CalculateTurnLeadDistance(intersection, theta, r);
            }

            if (leadInDistance < 0)
            {
                return false;
            }

            GeoPoint aircraftPoint = new GeoPoint(position.Latitude, position.Longitude, position.AbsoluteAltitude);
            aircraftPoint.MoveByNMi(position.Track_True, GeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval));

            double dist = GeoPoint.FlatDistanceNMi(aircraftPoint, intersection);

            return dist < leadInDistance;
        }

        public void UpdateInfo(AircraftPosition pos, ref AircraftFms fms)
        {
            if (_trueCourse < 0)
            {
                return;
            }

            // Check cross track error
            GeoPoint aircraftPoint = new GeoPoint(pos.Latitude, pos.Longitude, pos.AbsoluteAltitude);
            double aTrackM;
            xTk = GeoUtil.CalculateCrossTrackErrorM(aircraftPoint, AssignedWaypoint.PointPosition, _trueCourse, out requiredTrueCourse, out aTrackM);

            if (aTrackM <= MIN_XTK_THRESHOLD_M && MIN_XTK_THRESHOLD_M <= aTk)
            {
                fms.WaypointPassed?.Invoke(this, new WaypointPassedEventArgs(AssignedWaypoint));
            }

            aTk = aTrackM;
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            if (_trueCourse < 0)
            {
                return;
            }

            UpdateInfo(position, ref fms);

            if (trackToHold == -1)
            {
                trackToHold = requiredTrueCourse;
            }

            if (instr == null)
            {
                instr = new TrackHoldInstruction(trackToHold);
            }

            // Calculate track difference
            double trackDiff = GeoUtil.CalculateTurnAmount(position.Track_True, trackToHold);

            // Calculate course difference
            double newInterceptCourse = GetNewInterceptCourse();
            double courseDiff = GeoUtil.CalculateTurnAmount(trackToHold, newInterceptCourse);

            //if (Math.Abs(xTk) > MIN_XTK_THRESHOLD_M)
            //{
            //    Console.WriteLine($"{_magneticCourse:000} to {AssignedWaypoint.PointName}:\tRequired:\t{requiredTrueCourse:000}\tActual:\t{position.Track_True:000}\tTo Hold:\t{trackToHold:000}xTk:\t{xTk}m");
            //}

            // Intercept if the conditions are met
            if (Math.Abs(courseDiff) > Double.Epsilon && ShouldActivateInstruction(position, fms, posCalcInterval))
            {
                trackToHold = requiredTrueCourse;
                instr = new TrackHoldInstruction(trackToHold);
            }
            else if (Math.Abs(xTk) > MIN_XTK_THRESHOLD_M && Math.Abs(trackDiff) <= Double.Epsilon &&
              (Math.Abs(courseDiff) < Double.Epsilon || (courseDiff < 0 && xTk > 0) || (courseDiff > 0 && xTk < 0)))
            {
                trackToHold = newInterceptCourse;
                instr = new TrackHoldInstruction(trackToHold);
            }

            instr.UpdatePosition(ref position, ref fms, posCalcInterval);
        }

        private double GetNewInterceptCourse()
        {
            // Recalculate intercept course
            double offset = Math.Round(Math.Min((Math.Abs(xTk) / MAX_INTERCEPT_XTK_M) * MAX_INTERCEPT_ANGLE, MAX_INTERCEPT_ANGLE), 1, MidpointRounding.AwayFromZero);

            if (xTk > 0)
            {
                return GeoUtil.NormalizeHeading(requiredTrueCourse - offset);
            }
            else
            {
                return GeoUtil.NormalizeHeading(requiredTrueCourse + offset);
            }
        }

        public override string ToString()
        {
            return $"NAV {AssignedWaypoint.PointName} - {_magneticCourse:000.0} | {xTk:0.0}m";
        }
    }
}
