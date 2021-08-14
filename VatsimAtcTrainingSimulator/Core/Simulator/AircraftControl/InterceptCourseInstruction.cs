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
        private LatLonAltPoint intersection;
        public Waypoint AssignedWaypoint { get; private set; }
        public double Course { get; private set;}

        private double trueCourse;

        public InterceptCourseInstruction(Waypoint assignedWp, double course)
        {
            AssignedWaypoint = assignedWp;
            Course = course;
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
                trueCourse = AcftGeoUtil.NormalizeHeading(Course + declin);

                previousTrack = position.Track_True;
                intersection = AcftGeoUtil.FindIntersection(position, AssignedWaypoint, trueCourse);

                // Find degrees to turn
                double theta = Math.Abs(AcftGeoUtil.CalculateTurnAmount(position.Track_True, trueCourse));

                // Calculate radius of turn
                double r = AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(position.GroundSpeed, 25, 3), position.GroundSpeed);

                leadInDistance = AcftGeoUtil.CalculateTurnLeadDistance(intersection, theta, r);
            }

            if (leadInDistance < 0)
            {
                return false;
            }

            LatLonAltPoint aircraftPoint = new LatLonAltPoint(position.Latitude, position.Longitude, position.AbsoluteAltitude);
            aircraftPoint.MoveByNMi(position.Track_True, AcftGeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval));

            double dist = AcftGeoUtil.CalculateFlatDistanceNMi(aircraftPoint.Lat, aircraftPoint.Lon, intersection.Lat, intersection.Lon);

            return dist < leadInDistance;
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            new TrackHoldInstruction(trueCourse).UpdatePosition(ref position, posCalcInterval);
        }

        public override string ToString()
        {
            return $"NAV {AssignedWaypoint.Identifier} - {Course} ({leadInDistance})";
        }
    }
}
