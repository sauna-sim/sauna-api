using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public enum RouteLegTypeEnum
    {
        COURSE_TO_ALT,
        COURSE_TO_FIX,
        DIRECT_TO_FIX,
        FIX_TO_ALT,
        FIX_TO_MANUAL,
        HOLD_TO_ALT,
        HOLD_TO_FIX,
        HOLD_TO_MANUAL,
        INITIAL_FIX,
        TRACK_TO_FIX,
        RADIUS_TO_FIX,
        HEADING_TO_ALT,
        HEADING_TO_INTC,
        HEADING_TO_DME,
        HEADING_TO_MANUAL,
        DISCO
    }

    public enum BearingTypeEnum
    {
        MAGNETIC,
        TRUE
    }

    public class WaypointPassedEventArgs : EventArgs
    {
        public IRoutePoint RoutePoint { get; set; }

        public WaypointPassedEventArgs(IRoutePoint routePoint)
        {
            RoutePoint = routePoint;
        }
    }

    /// <summary>
    /// Interface that represents a route leg.
    /// </summary>
    public interface IRouteLeg
    {
        /// <summary>
        /// Get the point at which this leg begins. If there is none, null is returned.
        /// </summary>
        FmsPoint StartPoint { get; }

        /// <summary>
        /// Get the point at which this leg terminates. If there is none, null is returned.
        /// </summary>
        FmsPoint EndPoint { get; }

        /// <summary>
        /// Gets the initial true course for this leg. If there is none, -1 is returned.
        /// </summary>
        double InitialTrueCourse { get; }

        /// <summary>
        /// Gets the final true course for this leg. If there is none, -1 is returned.
        /// </summary>
        double FinalTrueCourse { get; }

        /// <summary>
        /// Gets the total leg length. 0 for none.
        /// </summary>
        double LegLength { get; }

        /// <summary>
        /// Route Leg Type
        /// </summary>
        RouteLegTypeEnum LegType { get; }

        List<NdLine> UiLines { get; }

        /// <summary>
        /// Determines whether the current leg has terminated and if control should be passed to next instruction
        /// </summary>
        /// <param name="aircraft">The current aircraft.</param>
        /// <returns><c>bool</c> Whether or not the leg has terminated.</returns>
        bool HasLegTerminated(SimAircraft aircraft);

        /// <summary>
        /// Processes the Leg
        /// </summary>
        /// <param name="aircraft">The current aircraft.</param>
        /// <param name="intervalMs">Time (ms) before next position update.</param>
        void ProcessLeg(SimAircraft aircraft, int intervalMs);

        /// <summary>
        /// Obtains course info for LNAV Autopilot guidance.
        /// </summary>
        /// <param name="aircraft">The current aircraft.</param>
        /// <returns><c>(double, double, double, double)</c> Required True Course (m), Cross Track Error (m), Along Track Distance (m), Turn Radius (m) (0 if N/A, -ive for left, +ive for right)</returns>
        (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft);

        /// <summary>
        /// Determines whether or not the current leg should be activated
        /// </summary>
        /// <param name="aircraft">The current aircraft.</param>
        /// <param name="intervalMs">Time (ms) before next position update.</param>
        /// <returns><c>bool</c> Whether or not turn should be initiated to this leg.</returns>
        bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs);
    }
}
