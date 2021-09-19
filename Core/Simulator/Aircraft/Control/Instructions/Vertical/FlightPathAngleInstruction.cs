using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.Instructions.Vertical
{
    public class FlightPathAngleInstruction : IVerticalControlInstruction
    {
        decimal _fpa;

        public FlightPathAngleInstruction(decimal fpa)
        {
            this._fpa = fpa;
        }

        public VerticalControlMode Type => VerticalControlMode.FLIGHT_PATH_ANGLE;

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            // Should never be auto activated
            return false;
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            double deltaAltNMi = GeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval) * Math.Tan(MathUtil.ConvertDegreesToRadians((double)_fpa));
            double deltaAltFt = MathUtil.ConvertMetersToFeet(MathUtil.ConvertNauticalMilesToMeters(deltaAltNMi));

            // Update altitude
            position.AbsoluteAltitude += deltaAltFt;

            // Update V/S
            position.VerticalSpeed = deltaAltFt * 60000 / posCalcInterval;
        }

        public override string ToString()
        {
            return $"FPA Hold: {_fpa}degrees";
        }
    }
}
