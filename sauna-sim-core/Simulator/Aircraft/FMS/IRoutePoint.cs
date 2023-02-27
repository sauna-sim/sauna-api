using AviationCalcUtilNet.GeoTools;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public interface IRoutePoint
    {
        GeoPoint PointPosition { get; }

        string PointName { get; }
    }
}
