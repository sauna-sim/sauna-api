using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;

namespace SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Vertical
{
    public class GlidePathInstruction : IVerticalControlInstruction
    {
        private GeoPoint _startPoint;
        private double _gpAngle;

        public GlidePathInstruction(GeoPoint startPoint, double gpAngle)
        {
            _startPoint = new GeoPoint(startPoint);
            _gpAngle = gpAngle;
        }

        public VerticalControlMode Type => VerticalControlMode.GLIDESLOPE;

        private double GetAltAtPosition(GeoPoint point)
        {
            // Calculate glideslope altitude
            point.Alt = _startPoint.Alt;
            double distanceM = _startPoint - point;
            double deltaAltM = distanceM * Math.Tan(MathUtil.ConvertDegreesToRadians(_gpAngle));

            return _startPoint.Alt + MathUtil.ConvertMetersToFeet(deltaAltM);
        }

        private double GetAltAtNextPosition(AircraftPosition pos, int posCalcInterval)
        {
            GeoPoint acftPos = pos.PositionGeoPoint;
            acftPos.MoveByNMi(pos.Track_True, GeoUtil.CalculateDistanceTravelledNMi(pos.GroundSpeed, posCalcInterval));

            // Calculate glideslope altitude
            return GetAltAtPosition(acftPos);
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            double curAlt = position.TrueAltitude;
            double verticalSpeed = position.VerticalSpeed;
            double nextGpAlt = GetAltAtNextPosition(position, posCalcInterval);

            // Calculate whether altitude will be passed based on V/S.
            double nextAlt = curAlt + (verticalSpeed * posCalcInterval / (60 * 1000));

            return (nextAlt <= nextGpAlt && nextGpAlt <= curAlt) ||
                (nextAlt >= nextGpAlt && nextGpAlt >= curAlt);
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Calculate glideslope altitude
            position.TrueAltitude = GetAltAtPosition(position.PositionGeoPoint);
        }

        public override string ToString()
        {
            return $"APP G/P: {_gpAngle}degrees";
        }
    }
}
