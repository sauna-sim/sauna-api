using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.Ground
{
    public enum GroundPhaseType
    {
        LAND,
        TAKEOFF,
        GROUND
    }

    public enum TakeoffPhaseType
    {
        LINEUP,
        THRUSTSET,
        ROTATE,
        CLIMB
    }

    public class AircraftGroundHandler
    {

        private SimAircraft _parentAircraft;
        public GroundPhaseType GroundPhase { get; set; }
        public TakeoffPhaseType TakeoffPhase { get; set; }

        public AircraftGroundHandler(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
            TakeoffPhase = TakeoffPhaseType.LINEUP;
        }

        public void OnPositionUpdate(int intervalMs)
        {
            if(GroundPhase == GroundPhaseType.LAND)
            {
                HandleOnLand(intervalMs);
            }
            else if(GroundPhase == GroundPhaseType.TAKEOFF)
            {
                // Update FMS
                _parentAircraft.Fms.OnPositionUpdate(intervalMs);

                // Update Takeoff Logic
                HandleOnTakeoff(intervalMs);
            }
        }

        private void HandleOnTakeoff(int intervalMs)
        {            
            double t = intervalMs / 1000.0;

            // CurPos -> Rwy Threshold pos
            var rwyThreshold = _parentAircraft.Fms.ActiveLeg.StartPoint.Point.PointPosition;
            double bearing = GeoPoint.FinalBearing(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
            _parentAircraft.Position.Track_True = bearing;
            double distance = GeoPoint.DistanceM(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);

            if(TakeoffPhase == TakeoffPhaseType.LINEUP)
            {
                if (distance < 10)
                {
                    _parentAircraft.Position.GroundSpeed = 0;
                    _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.FinalTrueCourse;
                    TakeoffPhase = TakeoffPhaseType.THRUSTSET;
                }
                else
                {

                    _parentAircraft.Position.GroundSpeed = 10;

                    if (_parentAircraft.Position.Track_True < _parentAircraft.Fms.ActiveLeg.FinalTrueCourse)
                    {
                        _parentAircraft.Position.YawRate = 1;
                        _parentAircraft.Position.Track_True += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate, 0, t);
                        if (_parentAircraft.Position.Track_True > _parentAircraft.Fms.ActiveLeg.FinalTrueCourse)
                        {
                            _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.FinalTrueCourse;
                            _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                            _parentAircraft.Position.YawRate = 0;
                        }
                    }
                    else
                    {
                        _parentAircraft.Position.YawRate = -1;
                        _parentAircraft.Position.Track_True += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate, 0, t);
                        if (_parentAircraft.Position.Track_True < _parentAircraft.Fms.ActiveLeg.FinalTrueCourse)
                        {
                            _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.FinalTrueCourse;
                            _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                            _parentAircraft.Position.YawRate = 0;
                        }
                    }
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.THRUSTSET)
            {
                //When aircraft is on Rwy Threshold Pos, TAKEOFF set
                //Thrust 100, Config 1, RWY track,
                if (_parentAircraft.Position.IndicatedAirSpeed < 140)
                {
                    _parentAircraft.Data.Config = 1;
                    _parentAircraft.Data.SpeedBrakePos = 0;
                    _parentAircraft.Data.ThrustLeverPos = 100;
                    _parentAircraft.Position.OnGround = false;

                    double accel = 10;
                    double curGs = _parentAircraft.Position.GroundSpeed;
                    double vi = MathUtil.ConvertKtsToMpers(curGs);
                    double vf = PerfDataHandler.CalculateFinalVelocity(vi, accel, t);

                    accel = PerfDataHandler.CalculateAcceleration(vi, vf, t);
                    _parentAircraft.Position.Forward_Acceleration = accel;
                }
                else
                {
                    TakeoffPhase = TakeoffPhaseType.ROTATE;
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.ROTATE)
            {
                //Speed 140 (vr), pitch 3deg
                if (_parentAircraft.Position.IndicatedAirSpeed < 160)
                {
                    if (_parentAircraft.Position.Pitch < 4)
                    {
                        _parentAircraft.Position.PitchRate = 2;
                        _parentAircraft.Position.Pitch += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.PitchRate, 0, t);
                    }
                }
                else
                {
                    TakeoffPhase = TakeoffPhaseType.CLIMB;
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.CLIMB)
            {
                // Cur alt > airport elev + 700ft, INFLIGHT AP FLCH (180kts), TRK rwy trk
                if (_parentAircraft.Position.TrueAltitude > _parentAircraft.airportElev + 700)
                {
                    _parentAircraft.Data.Config = 0;

                    _parentAircraft.Fms.ActivateNextLeg();
                    _parentAircraft.Autopilot.SelectedSpeed = 180;
                    _parentAircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
                    _parentAircraft.Autopilot.AddArmedVerticalMode(VerticalModeType.FLCH);
                    _parentAircraft.FlightPhase = FlightPhaseType.IN_FLIGHT;
                }
            }                     
        }

        private void HandleOnLand(int intervalMs)
        {
            if (_parentAircraft.Position.OnGround == false)
            {
                _parentAircraft.Position.OnGround = true;
            }

            if (_parentAircraft.Data.SpeedBrakePos <= 0)
            {
                _parentAircraft.Data.SpeedBrakePos = 1;
            }
            
            double t = intervalMs / 1000.0;
            double accel = -2;
            // Calculate Pitch, Bank, and Thrust Lever Position
            if (_parentAircraft.Position.Pitch > 0)
            {
                _parentAircraft.Position.PitchRate = -0.6;
                _parentAircraft.Position.Pitch += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.PitchRate, 0, t);
            }
            else
            {
                _parentAircraft.Position.PitchRate = 0;
                _parentAircraft.Position.Pitch = 0;
            }
            if (_parentAircraft.Position.Heading_True != _parentAircraft.Position.Track_True)
            {
                if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                {
                    _parentAircraft.Position.YawRate = -1;
                    _parentAircraft.Position.Heading_True += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate, 0, t);
                    if (_parentAircraft.Position.Heading_True < _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = 0;
                    }
                }
                else
                {
                    _parentAircraft.Position.YawRate = 1;
                    _parentAircraft.Position.Heading_True += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate, 0, t);
                    if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = 0;
                    }
                }

            }

            _parentAircraft.Position.Bank = 0;
            _parentAircraft.Data.ThrustLeverPos = 0;
            double curGs = _parentAircraft.Position.GroundSpeed;
            //Calculating final velocity
            double vi = MathUtil.ConvertKtsToMpers(curGs);
            double vf = PerfDataHandler.CalculateFinalVelocity(vi, accel, t);

            if (vf <= 0)
            {
                vf = 0;
                accel = PerfDataHandler.CalculateAcceleration(vi, vf, t);
            }
            _parentAircraft.Position.Forward_Acceleration = accel;            
        }

    }
}
