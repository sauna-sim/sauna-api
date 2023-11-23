using System;
using System.Collections.Generic;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
	public class DiscoLeg : IRouteLeg
	{
        private double _initialTrueCourse;

		public DiscoLeg(double initialTrueCourse)
		{
            _initialTrueCourse = initialTrueCourse;
		}

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _initialTrueCourse;

        public double FinalTrueCourse => _initialTrueCourse;

        public double LegLength => 0;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.DISCO;

        public List<NdLine> UiLines => new List<NdLine>();

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            return (_initialTrueCourse, 0, 0, 0);
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            return false;
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            return false;
        }

        public void InitializeLeg(SimAircraft aircraft)
        {
        }

        public override string ToString()
        {
            return "===(DISCO)===";
        }
    }
}

