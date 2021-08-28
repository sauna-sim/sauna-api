using AviationCalcUtilManaged.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public interface IRoutePoint
    {
        GeoPoint PointPosition { get; }

        string PointName { get; }
    }
}
