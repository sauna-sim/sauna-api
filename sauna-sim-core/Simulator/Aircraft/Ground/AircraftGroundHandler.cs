using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
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
            else
            {

            }
        }

        private void HandleOnTakeoff(int intervalMs)
        {            
            double t = intervalMs / 1000.0;

            

            if(TakeoffPhase == TakeoffPhaseType.LINEUP)
            {
                // CurPos -> Rwy Threshold pos
                var rwyThreshold = _parentAircraft.Fms.ActiveLeg.StartPoint.Point.PointPosition;
                double bearing = GeoPoint.FinalBearing(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                _parentAircraft.Position.Track_True = bearing;
                double distance = GeoPoint.FlatDistanceM(_parentAircraft.Position.PositionGeoPoint, rwyThreshold);
                if (distance < 1)
                {
                    _parentAircraft.Position.GroundSpeed = 0;
                    _parentAircraft.Position.Track_True = _parentAircraft.Fms.ActiveLeg.InitialTrueCourse;
                    _parentAircraft.Position.Latitude = rwyThreshold.Lat;
                    _parentAircraft.Position.Longitude = rwyThreshold.Lon;
                    TakeoffPhase = TakeoffPhaseType.THRUSTSET;
                }
                else
                {
                    _parentAircraft.Position.GroundSpeed = 10;
                }
            }
            else if(TakeoffPhase == TakeoffPhaseType.THRUSTSET)
            {
                //When aircraft is on Rwy Threshold Pos, TAKEOFF set
                //Thrust 100, Config 1, RWY track,
                if (_parentAircraft.Position.IndicatedAirSpeed < 140)
                {
                    _parentAircraft.Data.SpeedBrakePos = 0;
                    _parentAircraft.Data.ThrustLeverPos = 100;                  

                    double accel = 2;
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
                if (_parentAircraft.Position.Pitch < 4)
                {
                    _parentAircraft.Position.PitchRate = 2;
                    _parentAircraft.Position.Pitch += PerfDataHandler.CalculateDisplacement(_parentAircraft.Position.PitchRate, 0, t);
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
                if (_parentAircraft.Position.TrueAltitude > _parentAircraft.airportElev + 50)
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
                    var alt = _parentAircraft.airportElev + 50;

                    (double targetPitch, double targetVs) = GetTargetPitchAndVsForAlt(alt);

                    // Find time to get to 50ft.
                    var deltaAlt = alt - _parentAircraft.Position.IndicatedAltitude;

                    // Find Vi, Vf, and time
                    var vi = _parentAircraft.Position.VerticalSpeed / 60.0;
                    var vf = targetVs / 60.0;
                    var time = deltaAlt / (0.5 * (vf + vi));

                    // Figure out pitch velocity to get smooth pitch and alt change
                    var pitchVel = (targetPitch - _parentAircraft.Position.Pitch) / time;
                    var altAccel = PerfDataHandler.CalculateAcceleration(vi, vf, time);

                    var finalVs = PerfDataHandler.CalculateFinalVelocity(vi, altAccel, t) * 60;

                    // Calculate fwd accel
                    var fwdAccel = PerfDataHandler.CalculateAcceleration(MathUtil.ConvertKtsToMpers(_parentAircraft.Position.IndicatedAirSpeed), MathUtil.ConvertKtsToMpers(180), time);

                    _parentAircraft.Position.Forward_Acceleration = fwdAccel;
                    _parentAircraft.Position.VerticalSpeed = finalVs;
                    _parentAircraft.Position.PitchRate = pitchVel;
                    _parentAircraft.Position.Pitch += PerfDataHandler.CalculateDisplacement(pitchVel, 0, t);                    
                }
            }

            _parentAircraft.Position.Heading_True = _parentAircraft.Position.Track_True;
        }

        private (double targetPitch, double targetVs) GetTargetPitchAndVsForAlt(double alt)
        {
            var gribPoint = _parentAircraft.Position.GribPoint;
            double altDens;
            if (gribPoint != null)
            {
                double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(alt), gribPoint.GeoPotentialHeight_M, gribPoint.Temp_K);
                double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(alt), gribPoint.GeoPotentialHeight_M, gribPoint.Level_hPa * 100, T);
                altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
            } else
            {
                double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(alt), 0, AtmosUtil.ISA_STD_TEMP_K);
                double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(alt), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
                altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
            }

            var neededPitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, _parentAircraft.Data.ThrustLeverPos / 100.0, 0, 180, altDens, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0);
            var (_, vs) = PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, neededPitch, _parentAircraft.Data.ThrustLeverPos / 100.0, 180, altDens, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, 0);

            return (neededPitch, vs);
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
