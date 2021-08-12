using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public interface IControlInstruction
    {
        /// <summary>
        /// Determines whether or not the current instruction should be activated
        /// </summary>
        /// <returns>bool if </returns>
        bool ShouldActivateInstruction(AcftData position, int posCalcInterval);

        /// <summary>
        /// Updates the latitude and longitude for the next position update.
        /// Activates the next command if applicable
        /// </summary>
        /// <param name="position"></param>
        /// <param name="posCalcInterval"></param>
        void UpdatePosition(ref AcftData position, int posCalcInterval);
    }
}
