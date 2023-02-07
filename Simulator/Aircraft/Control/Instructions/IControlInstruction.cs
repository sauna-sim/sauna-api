using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Aircraft
{
    public interface IControlInstruction
    {
        /// <summary>
        /// Determines whether this instruction should be activated
        /// </summary>
        /// <param name="position"><c>AircraftPosition</c> The aircraft's current position</param>
        /// <param name="fms"><c>AircraftFms</c> The aircraft's fms.</param>
        /// <param name="posCalcInterval"><c>int</c> Time (ms) before next position update.</param>
        /// <returns><c>bool</c> Whether or not the instruction should be activated</returns>
        bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval);

        /// <summary>
        /// Updates the position and control state of the aircraft
        /// </summary>
        /// <param name="position"><c>AircraftPosition</c> The aircraft's position.</param>
        /// <param name="fms"><c>AircraftFms</c> The aircraft's fms.</param>
        /// <param name="posCalcInterval"><c>int</c> Time (ms) before next position update.</param>
        void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval);
    }
}
