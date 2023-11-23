using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    /// <summary>
    /// Provides lateral and vertical guidance to follow a specific ILS approach.
    /// This is not meant to be sequenced into a route; this is mostly for autopilot interal use.
    /// It will probably be removed after refactoring VNAV.
    /// </summary>
    public class IlsApproachLeg : IRouteLeg
    {
        private FmsPoint _startPoint;

        private FmsPoint _endPoint;

        private double locCourse;

        public FmsPoint StartPoint { get; set; }

        public FmsPoint EndPoint => throw new NotImplementedException();

        public double InitialTrueCourse => throw new NotImplementedException();

        public double FinalTrueCourse => throw new NotImplementedException();

        public double LegLength => throw new NotImplementedException();

        public RouteLegTypeEnum LegType => throw new NotImplementedException();

        public List<NdLine> UiLines => throw new NotImplementedException();

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            throw new NotImplementedException();
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            throw new NotImplementedException();
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            throw new NotImplementedException();
        }
    }
}
