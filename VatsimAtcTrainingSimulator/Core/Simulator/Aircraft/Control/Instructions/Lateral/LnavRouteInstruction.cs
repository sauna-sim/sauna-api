using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
{
    public class LnavRouteInstruction : ILateralControlInstruction
    {
        private bool _suspended;
        private IRouteLeg _currentLeg;

        public LateralControlMode Type => LateralControlMode.NAV_ROUTE;

        public LnavRouteInstruction()
        {
            _suspended = false;
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            IRouteLeg leg = fms.GetFirstLeg();

            if (leg == null)
            {
                return false;
            }

            return leg.ShouldActivateLeg(position, fms, posCalcInterval);
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            if (_currentLeg == null)
            {
                IRouteLeg leg = fms.GetFirstLeg();

                // If there is no leg, hold current track
                if (leg == null)
                {
                    new TrackHoldInstruction(position.Track_True).UpdatePosition(ref position, ref fms, posCalcInterval);
                    return;
                }

                _currentLeg = leg;
                _currentLeg.WaypointPassed += OnWaypointPassed;
                fms.RemoveFirstLeg();
            }

            // Check if next leg should be activated
            if (!_suspended)
            {
                IRouteLeg leg = fms.GetFirstLeg();

                // Activate next leg if applicable
                if (leg != null && (leg.EndPoint == null || leg.EndPoint.PointType == RoutePointTypeEnum.FLY_BY) && leg.ShouldActivateLeg(position, fms, posCalcInterval))
                {
                    _currentLeg.WaypointPassed -= OnWaypointPassed;
                    _currentLeg = leg;
                    _currentLeg.WaypointPassed += OnWaypointPassed;
                    fms.RemoveFirstLeg();
                }
            }

            // Update position
            _currentLeg.UpdatePosition(ref position, ref fms, posCalcInterval);
        }

        public void OnWaypointPassed(object sender, WaypointPassedEventArgs e)
        {
            if (!_suspended)
            {
                IRouteLeg leg = e.FMS.GetFirstLeg();

                // Activate next leg if applicable
                if (leg != null)
                {
                    _currentLeg.WaypointPassed -= OnWaypointPassed;
                    _currentLeg = leg;
                    _currentLeg.WaypointPassed += OnWaypointPassed;
                    e.FMS.RemoveFirstLeg();
                }
            }
        }

        public override string ToString()
        {
            if (_currentLeg != null)
            {
                return $"LNAV {_currentLeg}";
            }

            return "LNAV";
        }
    }
}
