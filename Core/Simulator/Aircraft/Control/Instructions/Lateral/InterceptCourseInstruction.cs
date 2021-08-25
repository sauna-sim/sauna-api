using AviationSimulation.GeoTools;
using AviationSimulation.MathTools;
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
    public enum CourseInterceptPhaseEnum
    {
        INTERCEPTING,
        CAPTURING,
        CAPTURED
    }

    public class InterceptCourseInstruction : ILateralControlInstruction
    {
        private const double MAX_INTC_ANGLE = 45;
        private const double MIN_XTK_M = 3;
        private const double MAX_CRS_DEV = 0.1;
        private const double MAX_INTC_XTK_M = MathUtil.CONV_FACTOR_NMI_M;

        private double previousTrack;
        private double previousTas;
        private double previousWindDir;
        private double previousWindSpd;

        private double leadInDistance;
        private double radiusOfTurn = -1;
        private GeoPoint intersection;
        private double requiredTrueCourse;
        private double xTk = 0;
        private double aTk = 0;
        private double _magneticCourse = -1;
        private double _trueCourse = -1;
        private TrackHoldInstruction instr;

        private CourseInterceptPhaseEnum _phase;


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
            _phase = CourseInterceptPhaseEnum.INTERCEPTING;
        }

        public InterceptCourseInstruction(IRoutePoint assignedWp, double magneticCourse) : this(assignedWp)
        {
            MagneticCourse = magneticCourse;
        }

        public LateralControlMode Type => LateralControlMode.COURSE_INTERCEPT;

        private bool ShouldCaptureCourse(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            CalculateLeadTurnDistance(position);

            leadInDistance = Math.Max(MathUtil.ConvertMetersToNauticalMiles(MIN_XTK_M), leadInDistance);

            GeoPoint aircraftPoint = new GeoPoint(position.Latitude, position.Longitude, position.AbsoluteAltitude);
            aircraftPoint.MoveByNMi(position.Track_True, GeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval));

            double dist = GeoPoint.FlatDistanceNMi(aircraftPoint, intersection);

            return dist < leadInDistance || Math.Abs(xTk) <= MIN_XTK_M;
        }

        private void CalculateLeadTurnDistance(AircraftPosition position)
        {
            if (previousTrack != position.Track_True || previousTas != position.TrueAirSpeed ||
                previousWindDir != position.WindDirection || previousWindSpd != position.WindSpeed)
            {
                previousTrack = position.Track_True;
                previousTas = position.TrueAirSpeed;
                previousWindDir = position.WindDirection;
                previousWindSpd = position.WindSpeed;

                leadInDistance = GeoUtil.CalculateTurnLeadDistance(position.PositionGeoPoint, AssignedWaypoint.PointPosition, previousTrack,
                    previousTas, _trueCourse, previousWindDir, previousWindSpd, out radiusOfTurn, out intersection);
            }
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            if (_trueCourse < 0)
            {
                return false;
            }

            UpdateInfo(position, ref fms);

            return ShouldCaptureCourse(position, fms, posCalcInterval);
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

            if (aTrackM <= MIN_XTK_M && MIN_XTK_M <= aTk)
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

            if (instr == null)
            {
                instr = new TrackHoldInstruction(requiredTrueCourse, radiusOfTurn);
            }

            // Determine which phase to run
            switch (_phase)
            {
                case CourseInterceptPhaseEnum.INTERCEPTING:
                    HandleIntercepting(ref position, ref fms, posCalcInterval);
                    break;
                case CourseInterceptPhaseEnum.CAPTURING:
                    HandleCapturing(ref position, ref fms, posCalcInterval);
                    break;
                default:
                    HandleCaptured(ref position, ref fms, posCalcInterval);
                    break;
            }

            // Update position
            instr.UpdatePosition(ref position, ref fms, posCalcInterval);
        }

        private void HandleIntercepting(ref AircraftPosition position, ref AircraftFms fms, int posCalcInvl)
        {
            // Set phase just in case
            _phase = CourseInterceptPhaseEnum.INTERCEPTING;

            // Check if we should capture
            if (ShouldCaptureCourse(position, fms, posCalcInvl))
            {
                // Sequence to capturing
                HandleCapturing(ref position, ref fms, posCalcInvl);
                return;
            }

            // Calculate and check intercept angle
            double intcAngle = GetNewInterceptCourse();
            double intcDiff = GeoUtil.CalculateTurnAmount(instr.AssignedTrack, intcAngle);

            if ((xTk > 0 && intcDiff < 0) ||    // Right of course and left turn is required OR
                (xTk < 0 && intcDiff > 0))      // Left of course and right turn is required                
            {
                // Update intercept angle
                instr = new TrackHoldInstruction(intcAngle);
            }
        }

        private void HandleCapturing(ref AircraftPosition position, ref AircraftFms fms, int posCalcInvl)
        {
            // Set phase just in case
            _phase = CourseInterceptPhaseEnum.CAPTURING;

            // If we are on requried course
            double courseDiff = GeoUtil.CalculateTurnAmount(requiredTrueCourse, position.Track_True);
            if (Math.Abs(courseDiff) <= MAX_CRS_DEV)
            {
                // Sequence to captured
                HandleCaptured(ref position, ref fms, posCalcInvl);
                return;
            }

            // Make sure required course is set
            double reqCourseDiff = GeoUtil.CalculateTurnAmount(instr.AssignedTrack, requiredTrueCourse);
            if (Math.Abs(reqCourseDiff) > Double.Epsilon)
            {
                instr = new TrackHoldInstruction(requiredTrueCourse, radiusOfTurn);
            }
        }

        private void HandleCaptured(ref AircraftPosition position, ref AircraftFms fms, int posCalcInvl)
        {
            // Set phase just in case
            _phase = CourseInterceptPhaseEnum.CAPTURED;

            // If aircraft needs to intercept
            if (Math.Abs(xTk) > MIN_XTK_M)
            {
                // Sequence to intercepting
                HandleIntercepting(ref position, ref fms, posCalcInvl);
                return;
            }

            // Make sure required course is set
            double reqCourseDiff = GeoUtil.CalculateTurnAmount(instr.AssignedTrack, requiredTrueCourse);
            if (Math.Abs(reqCourseDiff) > Double.Epsilon)
            {
                instr = new TrackHoldInstruction(requiredTrueCourse);
            }
        }

        private double GetNewInterceptCourse()
        {
            // Recalculate intercept course
            double offset = Math.Round(
                Math.Max(
                    Math.Min(
                        (Math.Abs(xTk) / MAX_INTC_XTK_M) * MAX_INTC_ANGLE,
                        MAX_INTC_ANGLE),
                    MAX_CRS_DEV)
                , 1, MidpointRounding.AwayFromZero);

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
            return $"NAV {AssignedWaypoint.PointName} - {_magneticCourse:000.0} | {xTk:0.0}m | {_phase}";
        }
    }
}
