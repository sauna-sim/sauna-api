using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using NavData_Interface.DataSources;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Waypoints;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.FMS.VNAV;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using NUnit.Framework.Internal;

namespace sauna_tests
{
    [Explicit]
    public class FmsTests
    {
        private MagneticTileManager _magTileManager;

        [SetUp]
        public void Setup()
        {
            var model = MagneticModel.FromFile("WMM.COF");
            _magTileManager = new MagneticTileManager(ref model);
        }

        private List<IRouteLeg> TestBuildRouteLegs()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            var navDataInterface = new DFDSource("e_dfd_2412.s3db");
            
            // KATL - KBOS
            // 27R PLMMR3 BURGG Q22 RBV Q419 JFK ROBUC3 ILS 4R
            
            // Legs
            var legs = new List<IRouteLeg>();

            // SID
            var sid = navDataInterface.GetSidByAirportAndIdentifier("KATL", "PLMMR3");
            sid.selectTransition("BURGG");
            sid.selectRunwayTransition("27R");
            
            foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(sid.GetEnumerator(), _magTileManager))
            {
                legs.Add(leg);
            }
            
            // Q22
            var position = legs[legs.Count - 1].EndPoint.Point.PointPosition;
            var q22 = navDataInterface.GetAirwayFromIdentifierAndFixes(
                "Q22",
                navDataInterface.GetClosestFixByIdentifier(position, "BURGG"),
                navDataInterface.GetClosestFixByIdentifier(position, "RBV"));
            
            foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(q22.GetEnumerator(), _magTileManager))
            {
                legs.Add(leg);
            }
            
            position = legs[legs.Count - 1].EndPoint.Point.PointPosition;
            var q419 = navDataInterface.GetAirwayFromIdentifierAndFixes(
                "Q419",
                navDataInterface.GetClosestFixByIdentifier(position, "RBV"),
                navDataInterface.GetClosestFixByIdentifier(position, "JFK"));
            
            foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(q419.GetEnumerator(), _magTileManager))
            {
                legs.Add(leg);
            }
            
            // STAR
            var star = navDataInterface.GetStarByAirportAndIdentifier("KBOS", "ROBUC3");
            star.selectTransition("JFK");
            star.selectRunwayTransition("04R");
            
            foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(star.GetEnumerator(), _magTileManager))
            {
                legs.Add(leg);
            }
            
            // Approach
            var apch = navDataInterface.GetApproachByAirportAndIdentifier("KBOS", "I04R");
            apch.SelectVia("GOSHI");
            
            foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(apch.GetEnumerator(), _magTileManager))
            {
                legs.Add(leg);
            }

            return legs;
        }

        [Test]
        public void TestVnav1()
        {
            var legs = TestBuildRouteLegs();
            
            PerfData perfData = PerfDataHandler.LookupForAircraft("A320");

            PerfInit init = new PerfInit()
            {
                ClimbKias = perfData.Climb_KIAS,
                ClimbMach = (int)(perfData.Climb_Mach * 100),
                CruiseKias = perfData.Cruise_KIAS,
                CruiseMach = (int)(perfData.Cruise_Mach * 100),
                DescentKias = perfData.Descent_KIAS,
                DescentMach = (int)(perfData.Descent_Mach * 100),
                CruiseAlt = 35000,
                LimitAlt = 10000,
                LimitSpeed = 250,

                // TODO: Change to be based on Departure/Arrival airport
                TransitionAlt = 18000,
                TransitionLevel = 18000
            };

            var iterator = new FmsVnavLegIterator
            {
                Index = legs.Count - 1,
                NextLegIndex = legs.Count,
                MoveDir = 0,
                AlongTrackDistance = Length.FromMeters(0),
                ApchAngle = null,
                DistanceToRwy = null,
                EarlyUpperAlt = null,
                EarlyUpperAltIndex = -1,
                EarlySpeedSearch = false,
                EarlySpeed = -1,
                EarlySpeedIndex = -1,
                EarlyUpperAltSearch = false,
                DecelDist = null,
                DecelSpeed = -1,
                Finished = false
            };

            // Loop through legs from last to first ending either when first leg is reached or cruise alt is reached
            while (iterator.Index > -1 && !iterator.Finished)
            {
                IRouteLeg? getLeg(int index)
                {
                    if (index < 0 || index >= legs.Count)
                    {
                        return null;
                    }
                    return legs[index];
                }

                StringBuilder sb = new StringBuilder();
                foreach (var leg in legs)
                {
                    if (leg.EndPoint == null || leg.LegLength <= Length.FromMeters(0))
                    {
                        continue;
                    }

                    if (leg.EndPoint.VnavPoints != null && leg.EndPoint.VnavPoints.Count > 0)
                    {
                        for (int i = leg.EndPoint.VnavPoints.Count - 1; i >= 0; i--)
                        {
                            var vnavPt = leg.EndPoint.VnavPoints[i];

                            sb.Append("\t\t");

                            if (vnavPt.Angle > Angle.FromRadians(0))
                            {
                                sb.Append($"{vnavPt.Angle.Degrees:N1} -> ");
                            }

                            sb.Append($"{vnavPt.Speed:N0}>{vnavPt.CmdSpeed}/{vnavPt.Alt.Feet:N0}ft ");
                            
                            if (vnavPt.AlongTrackDistance <= Length.FromMeters(0))
                            {
                                sb.Append($"X {leg.EndPoint.Point.PointName}");
                            }
                            else
                            {
                                sb.Append($"({vnavPt.AlongTrackDistance.NauticalMiles:N1}NM)");
                            }
                        }
                    }
                    else
                    {
                        sb.Append("\t\t");
                        sb.Append(leg.EndPoint.Point.PointName);
                    }
                }
                System.Diagnostics.Debug.WriteLine(sb.ToString());
                iterator = VnavDescentUtil.ProcessLegForDescent(iterator, getLeg, perfData, init, perfData.MTOW_kg, Length.FromFeet(0));
            }

            Console.WriteLine(legs);
        }
    }
}
