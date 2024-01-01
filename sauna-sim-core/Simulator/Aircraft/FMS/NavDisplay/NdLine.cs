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
			StartPoint = (GeoPoint)start.Clone();
			EndPoint = (GeoPoint)end.Clone();
		}
	}
}

