using System;
using AviationCalcUtilNet.GeoTools;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay
{
	public class NdArc : NdLine
	{
		public GeoPoint Center { get; set; }
		public double Radius_m { get; set; }
		public double StartTrueBearing { get; set; }
		public double EndTrueBearing { get; set; }
		public bool Clockwise { get; set; }

		public NdArc(GeoPoint start, GeoPoint end, GeoPoint center, double radiusM, double startTrueBearing, double endTrueBearing, bool clockwise) : base(start, end)
		{
			Radius_m = radiusM;
			StartTrueBearing = startTrueBearing;
			EndTrueBearing = endTrueBearing;
			Clockwise = clockwise;
			Center = new GeoPoint(center)
		}
	}
}

