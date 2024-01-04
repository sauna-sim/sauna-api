using AviationCalcUtilNet.Atmos;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using AviationCalcUtilNet.Units;
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

            

            if(TakeoffPhase == TakeoffPhaseType.LINEUP)
            {
                // CurPos -> Rwy Threshold pos
                var rwyThreshold = _parentAircraft.Fms.ActiveLeg.StartPoint.Point.PointPosition;
                Bearing bearing = GeoPoint.FinalBearing(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                _parentAircraft.Position.Track_True = bearing;
                Length distance = GeoPoint.FlatDistance(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                if (distance < Length.FromMeters(10))
                {
                    _parentAircraft.Position.GroundSpeed = Velocity.FromKnots(0);
                    _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.InitialTrueCourse;
                    TakeoffPhase = TakeoffPhaseType.THRUSTSET;
                }
                else
                {
                    _parentAircraft.Position.GroundSpeed = Velocity.FromKnots(10);
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.THRUSTSET)
            {
                //When aircraft is on Rwy Threshold Pos, TAKEOFF set
                //Thrust 100, Config 1, RWY track,
                if (_parentAircraft.Position.IndicatedAirSpeed < Velocity.FromKnots(140))
                {
                    _parentAircraft.Data.Config = 1;
                    _parentAircraft.Data.SpeedBrakePos = 0;
                    _parentAircraft.Data.ThrustLeverPos = 100;                    

                    double accel = 2;
                    Velocity vi = _parentAircraft.Position.GroundSpeed;
                    Velocity vf = Velocity.FromMetersPerSecond(PerfDataHandler.CalculateFinalVelocity(vi.MetersPerSecond, accel, t));

                    accel = PerfDataHandler.CalculateAcceleration(vi.MetersPerSecond, vf.MetersPerSecond, t);
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
                if (_parentAircraft.Position.IndicatedAirSpeed < Velocity.FromKnots(160))
                {
                    if (_parentAircraft.Position.Pitch < Angle.FromDegrees(4))
                    {
                        _parentAircraft.Position.PitchRate = AngularVelocity.FromDegreesPerSecond(2);
                        _parentAircraft.Position.Pitch += Angle.FromRadians(PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.PitchRate.RadiansPerSecond, 0, t));
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
                if (_parentAircraft.Position.TrueAltitude > Length.FromFeet(_parentAircraft.RelaventAirport.Elevation + 500))
                {
                    _parentAircraft.Data.Config = 0;
                    _parentAircraft.Position.OnGround = false;
                    
                    _parentAircraft.Autopilot.SelectedSpeed = 180;
                    _parentAircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
                    _parentAircraft.Autopilot.AddArmedVerticalMode(VerticalModeType.FLCH);
                    _parentAircraft.FlightPhase = FlightPhaseType.IN_FLIGHT;
                    return;
                }
                else
                {
                    var gribPoint = _parentAircraft.Position.GribPoint;
                    var alt = Length.FromFeet(_parentAircraft.RelaventAirport.Elevation + 500);
                    Length altDens;
                    if (gribPoint != null)
                    {
                        Temperature T = AtmosUtil.CalculateTempAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.Temp);
                        Pressure p = AtmosUtil.CalculatePressureAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.LevelPressure, T);
                        altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                    }
                    else
                    {
                        Temperature T = AtmosUtil.CalculateTempAtAlt(alt, (Length)0, AtmosUtil.ISA_STD_TEMP);
                        Pressure p = AtmosUtil.CalculatePressureAtAlt(alt, (Length)0, AtmosUtil.ISA_STD_PRES, T);
                        altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                    }


                    var neededPitch = Angle.FromDegrees(PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, _parentAircraft.Data.ThrustLeverPos / 100.0, 0, 180, altDens.Feet, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0));
                    var neededVs = Velocity.FromFeetPerMinute(PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, neededPitch.Degrees, _parentAircraft.Data.ThrustLeverPos / 100.0, 180, altDens.Feet, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0).vs);
                    _parentAircraft.Position.VerticalSpeed = neededVs;
                    _parentAircraft.Position.Pitch = neededPitch;                    
                }
            }

            _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
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
            if (_parentAircraft.Position.Pitch.Radians > 0)
            {
                _parentAircraft.Position.PitchRate = AngularVelocity.FromDegreesPerSecond(-0.6);
                _parentAircraft.Position.Pitch += Angle.FromRadians(PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.PitchRate.RadiansPerSecond, 0, t));
            }
            else
            {
                _parentAircraft.Position.PitchRate = (AngularVelocity)0;
                _parentAircraft.Position.Pitch = (Angle)0;
            }
            if (_parentAircraft.Position.Heading_True != _parentAircraft.Position.Track_True)
            {
                if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                {
                    _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(-1);
                    _parentAircraft.Position.Heading_True += Angle.FromRadians(PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate.RadiansPerSecond, 0, t));
                    if (_parentAircraft.Position.Heading_True < _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = (AngularVelocity)0;
                    }
                }
                else
                {
                    _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(1);
                    _parentAircraft.Position.Heading_True += Angle.FromRadians(PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.YawRate.RadiansPerSecond, 0, t));
                    if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = (AngularVelocity)0;
                    }
                }

            }

            _parentAircraft.Position.Bank = (Angle)0;
            _parentAircraft.Data.ThrustLeverPos = 0;

            //Calculating final velocity
            Velocity vi = _parentAircraft.Position.GroundSpeed;
            Velocity vf = Velocity.FromMetersPerSecond(PerfDataHandler.CalculateFinalVelocity(vi.MetersPerSecond, accel, t));

            if (vf <= 0)
            {
                vf = 0;
                accel = PerfDataHandler.CalculateAcceleration(vi, vf, t);
            }
            _parentAircraft.Position.Forward_Acceleration = accel;            
        }

    }
}
