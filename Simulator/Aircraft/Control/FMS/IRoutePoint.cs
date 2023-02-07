using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS
{
    public interface IRoutePoint
    {
        GeoPoint PointPosition { get; }

        string PointName { get; }
    }
}
