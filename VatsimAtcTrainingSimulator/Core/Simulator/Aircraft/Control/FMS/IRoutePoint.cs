using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public interface IRoutePoint
    {
        GeoPoint PointPosition { get; }

        string PointName { get; }
    }
}
