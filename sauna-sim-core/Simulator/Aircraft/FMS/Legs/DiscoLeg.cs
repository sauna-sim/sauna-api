using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
	public class DiscoLeg : IRouteLeg
	{
        private Bearing _initialTrueCourse;

		public DiscoLeg(Bearing initialTrueCourse)
		{
            _initialTrueCourse = initialTrueCourse;
		}

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => null;

        public Bearing InitialTrueCourse => _initialTrueCourse;

        public Bearing FinalTrueCourse => _initialTrueCourse;

        public Length LegLength => (Length) 0;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.DISCO;

        public List<NdLine> UiLines => new List<NdLine>();

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            return (_initialTrueCourse, (Length) 0, (Length) 0, (Length)0);
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

