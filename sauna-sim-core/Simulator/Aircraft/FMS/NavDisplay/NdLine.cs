using System;
using AviationCalcUtilNet.GeoTools;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay
{
	public class NdLine
	{
		public GeoPoint StartPoint { get; set; }
		public GeoPoint EndPoint { get; set; }

		public NdLine(GeoPoint start, GeoPoint end)
		{
			StartPoint = new GeoPoint(start);
			EndPoint = new GeoPoint(end);
		}
	}
}

