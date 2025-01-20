using AviationCalcUtilNet.Atmos;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Math;
using AviationCalcUtilNet.MathTools;
using AviationCalcUtilNet.Physics;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
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
        TAXI,
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
            GroundPhase = GroundPhaseType.GROUND;
            TakeoffPhase = TakeoffPhaseType.TAXI;
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

            if(TakeoffPhase == TakeoffPhaseType.TAXI)
            {
                TakeoffPhase = TakeoffPhaseType.LINEUP;
            }    
            if(TakeoffPhase == TakeoffPhaseType.LINEUP)
            {
                // CurPos -> Rwy Threshold pos
                var rwyThreshold = _parentAircraft.Fms.ActiveLeg.StartPoint.Point.PointPosition;
                Bearing bearing = GeoPoint.FinalBearing(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                _parentAircraft.Position.Track_True = bearing;
                Length distance = GeoPoint.FlatDistance(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                if (distance < Length.FromMeters(1))
                {
                    _parentAircraft.Position.GroundSpeed = Velocity.FromKnots(0);
                    _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.InitialTrueCourse;
                    _parentAircraft.Position.Latitude = rwyThreshold.Lat;
                    _parentAircraft.Position.Longitude = rwyThreshold.Lon;
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
                    _parentAircraft.Data.SpeedBrakePos = 0;
                    _parentAircraft.Data.ThrustLeverPos = 100;                  

                    Acceleration accel = Acceleration.FromMetersPerSecondSquared(2);
                    Velocity curGs = _parentAircraft.Position.GroundSpeed;
                    Velocity vi = curGs;
                    Velocity vf = PhysicsUtil.KinematicsFinalVelocity(vi, accel, TimeSpan.FromSeconds(t));

                    accel = PhysicsUtil.KinematicsAcceleration(vi, vf, TimeSpan.FromSeconds(t));
                    _parentAircraft.Position.Forward_Acceleration = accel;
                }
                else
                {
                    TakeoffPhase = TakeoffPhaseType.ROTATE;
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.ROTATE)
            {
                if (_parentAircraft.Position.Pitch < Angle.FromDegrees(4))
                {
                    _parentAircraft.Position.PitchRate = AngularVelocity.FromDegreesPerSecond(2);
                    _parentAircraft.Position.Pitch += Angle.FromRadians((PhysicsUtil.KinematicsDisplacement2(_parentAircraft.Position.PitchRate.RadiansPerSecond, 0, t)));
                }
                else
                {
                    _parentAircraft.Position.OnGround = false;
                    TakeoffPhase = TakeoffPhaseType.CLIMB;
                    _parentAircraft.Fms.PhaseType = FmsPhaseType.CLIMB;
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.CLIMB)
            {
                // Cur alt > airport elev + 700ft, INFLIGHT AP FLCH (180kts), TRK rwy trk
                if (_parentAircraft.Position.TrueAltitude > _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(50))
                {
                    _parentAircraft.Data.Config = 1;

                    _parentAircraft.Autopilot.SelectedSpeed = 180;
                    _parentAircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
                    _parentAircraft.Autopilot.AddArmedVerticalMode(VerticalModeType.FLCH);
                    _parentAircraft.Autopilot.SelectedSpeedMode = Autopilot.McpSpeedSelectorType.FMS;
                    _parentAircraft.FlightPhase = FlightPhaseType.IN_FLIGHT;
                    return;
                }
                else
                {
                    var alt = _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(50);

                    (Angle targetPitch, Velocity targetVs) = GetTargetPitchAndVsForAlt(alt);

                    // Find time to get to 50ft.
                    var deltaAlt = alt - _parentAircraft.Position.IndicatedAltitude;

                    // Find Vi, Vf, and time
                    Velocity vi = _parentAircraft.Position.VerticalSpeed;
                    Velocity vf = targetVs;
                    TimeSpan time = PhysicsUtil.KinematicsTime1(deltaAlt, vi, vf);

                    // Figure out pitch velocity to get smooth pitch and alt change
                    var pitchVel = (targetPitch - _parentAircraft.Position.Pitch) / time;
                    var altAccel = PhysicsUtil.KinematicsAcceleration(vi, vf, time);

                    var finalVs = PhysicsUtil.KinematicsFinalVelocity(vi, altAccel, TimeSpan.FromSeconds(t));

                    // Calculate fwd accel
                    var fwdAccel = PhysicsUtil.KinematicsAcceleration(_parentAircraft.Position.IndicatedAirSpeed,Velocity.FromKnots(180), time);

                    _parentAircraft.Position.Forward_Acceleration = fwdAccel;
                    _parentAircraft.Position.VerticalSpeed = finalVs;
                    _parentAircraft.Position.PitchRate = pitchVel;
                    _parentAircraft.Position.Pitch += Angle.FromRadians(PhysicsUtil.KinematicsDisplacement2(pitchVel.RadiansPerSecond, 0, t));                    
                }
            }

            if(_parentAircraft.Position.OnGround)
            {
                _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
            }            
        }

        private (Angle targetPitch, Velocity targetVs) GetTargetPitchAndVsForAlt(Length alt)
        {
            var gribPoint = _parentAircraft.Position.GribPoint;
            Length altDens;
            if (gribPoint != null)
            {
                Temperature T = AtmosUtil.CalculateTempAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.Temp);
                Pressure p = AtmosUtil.CalculatePressureAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.LevelPressure, T);
                altDens = AtmosUtil.CalculateDensityAltitude(p, T);
            } else
            {
                Temperature T = AtmosUtil.CalculateTempAtAlt(alt, Length.FromMeters(0), AtmosUtil.ISA_STD_TEMP);
                Pressure p = AtmosUtil.CalculatePressureAtAlt(alt, Length.FromMeters(0), AtmosUtil.ISA_STD_PRES, T);
                altDens = AtmosUtil.CalculateDensityAltitude(p, T);
            }

            var neededPitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, _parentAircraft.Data.ThrustLeverPos / 100.0, 0, 180, altDens.Feet, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0);
            var (_, vs) = PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, neededPitch, _parentAircraft.Data.ThrustLeverPos / 100.0, 180, altDens.Feet, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0);

            return (Angle.FromDegrees(neededPitch), Velocity.FromFeetPerMinute(vs));
        }

        private void HandleOnLand(int intervalMs)
        {
            if (_parentAircraft.Position.OnGround == false)
            {
                _parentAircraft.Position.OnGround = true;
            }

            
            _parentAircraft.Data.SpeedBrakePos = 1;
            _parentAircraft.Data.ThrustReverse = true;
                       

            double t = intervalMs / 1000.0;
            Acceleration accel = Acceleration.FromMetersPerSecondSquared(-2);
            // Calculate Pitch, Bank, and Thrust Lever Position
            if (_parentAircraft.Position.Pitch.Radians > 0)
            {
                _parentAircraft.Position.PitchRate = AngularVelocity.FromDegreesPerSecond(-0.6);
                _parentAircraft.Position.Pitch += Angle.FromRadians(PhysicsUtil.KinematicsDisplacement2(_parentAircraft.Position.PitchRate.RadiansPerSecond, 0, t));
            }
            else
            {
                _parentAircraft.Position.PitchRate = AngularVelocity.FromDegreesPerSecond(0);
                _parentAircraft.Position.Pitch = Angle.FromDegrees(0);
            }
            if (_parentAircraft.Position.Heading_True != _parentAircraft.Position.Track_True)
            {
                if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                {
                    _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(-1);
                    _parentAircraft.Position.Heading_True += Angle.FromRadians(PhysicsUtil.KinematicsDisplacement2(_parentAircraft.Position.YawRate.RadiansPerSecond, 0, t));
                    if (_parentAircraft.Position.Heading_True < _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(0);
                    }
                }
                else
                {
                    _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(1);
                    _parentAircraft.Position.Heading_True += Angle.FromRadians(PhysicsUtil.KinematicsDisplacement2(_parentAircraft.Position.YawRate.RadiansPerSecond, 0, t));
                    if (_parentAircraft.Position.Heading_True > _parentAircraft.Position.Track_True)
                    {
                        _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
                        _parentAircraft.Position.YawRate = AngularVelocity.FromDegreesPerSecond(0);
                    }
                }

            }

            _parentAircraft.Position.Bank = Angle.FromDegrees(0);
            _parentAircraft.Data.ThrustLeverPos = 0;
            Velocity curGs = _parentAircraft.Position.GroundSpeed;
            //Calculating final velocity
            Velocity vi = curGs;
            Velocity vf = PhysicsUtil.KinematicsFinalVelocity(vi, accel, TimeSpan.FromSeconds(t));

            if (vf <= Velocity.FromMetersPerSecond(0))
            {
                vf = Velocity.FromMetersPerSecond(0);
                accel = PhysicsUtil.KinematicsAcceleration(vi, vf, TimeSpan.FromSeconds(t));
            }
            _parentAircraft.Position.Forward_Acceleration = accel;            
        }

    }
}
