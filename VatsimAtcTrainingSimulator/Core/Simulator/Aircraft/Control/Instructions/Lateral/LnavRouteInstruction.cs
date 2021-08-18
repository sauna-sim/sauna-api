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

        private void ActivateNextLeg(AircraftFms fms)
        {
            if (fms.ActiveLeg == null)
            {
                fms.ActivateNextLeg();
                fms.ActiveLeg.WaypointPassed += OnWaypointPassed;
            }
            else
            {
                IRouteLeg nextLeg = fms.GetFirstLeg();
                if (fms.ActiveLeg.EndPoint != null && nextLeg != null && nextLeg.StartPoint != null && _currentLeg.EndPoint.Point.Equals(nextLeg.StartPoint.Point))
                {
                    fms.ActiveLeg.WaypointPassed -= OnWaypointPassed;
                    fms.ActivateNextLeg();
                    fms.ActiveLeg.WaypointPassed += OnWaypointPassed;
                }
            }
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            if (fms.ActiveLeg == null)
            {
                IRouteLeg leg = fms.GetFirstLeg();

                // If there is no leg, hold current track
                if (leg == null)
                {
                    new TrackHoldInstruction(position.Track_True).UpdatePosition(ref position, ref fms, posCalcInterval);
                    return;
                }

                ActivateNextLeg(fms);
            }

            _currentLeg = fms.ActiveLeg;

            // Check if next leg should be activated
            if (!_suspended)
            {
                IRouteLeg nextLeg = fms.GetFirstLeg();

                // Activate next leg if applicable
                if (fms.ActiveLeg.EndPoint != null && fms.ActiveLeg.EndPoint.PointType == RoutePointTypeEnum.FLY_BY && 
                    nextLeg != null && nextLeg.ShouldActivateLeg(position, fms, posCalcInterval) && 
                    nextLeg.StartPoint != null && Math.Abs(fms.ActiveLeg.FinalTrueCourse - nextLeg.InitialTrueCourse) > 0.5)
                {
                    ActivateNextLeg(fms);
                }
            }

            // Update position
            fms.ActiveLeg.UpdatePosition(ref position, ref fms, posCalcInterval);
        }

        public void OnWaypointPassed(object sender, WaypointPassedEventArgs e)
        {
            if (!_suspended)
            {
                ActivateNextLeg(e.FMS);
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
