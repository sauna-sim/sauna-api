using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public enum RouteLegTypeEnum
    {
        MANUAL_SEQUENCE,
        POINT_TO_POINT,
        COURSE_TO_POINT,
        POINT_TO_ALTITUDE,
        RADIUS_TO_POINT,
        HOLD,
        DME_ARC
    }

    public class WaypointPassedEventArgs: EventArgs
    {
        public AircraftFms FMS { get; private set; }

        public WaypointPassedEventArgs(AircraftFms fms)
        {
            FMS = fms;
        }
    }

    /// <summary>
    /// Interface that represents a route leg.
    /// </summary>
    public interface IRouteLeg
    {
        FmsPoint StartPoint { get; }

        FmsPoint EndPoint { get; }

        event EventHandler<WaypointPassedEventArgs> WaypointPassed;

        /// <summary>
        /// Route leg's current lateral instruction
        /// </summary>
        ILateralControlInstruction Instruction { get; }

        /// <summary>
        /// Route Leg Type
        /// </summary>
        RouteLegTypeEnum LegType { get; }

        /// <summary>
        /// Determines whether or not the current leg should be activated
        /// </summary>
        /// <param name="pos"><c>AircraftPosition</c> The aircraft's current position</param>
        /// <param name="posCalcIntvl"><c>int</c> Time (ms) before next position update.</param>
        /// <returns></returns>
        bool ShouldActivateLeg(AircraftPosition pos, AircraftFms fms, int posCalcIntvl);

        /// <summary>
        /// Updates aircraft's position for the next position update.
        /// </summary>
        /// <param name="pos"><c>AircraftPosition</c> The aircraft's position</param>
        /// <param name="posCalcIntvl"><c>int</c> Time (ms) before next position update.</param>
        void UpdatePosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl);
    }
}
