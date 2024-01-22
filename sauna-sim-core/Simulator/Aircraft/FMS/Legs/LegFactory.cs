using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Magnetic;
using NavData_Interface.Objects.LegCollections.Legs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    internal static class LegFactory
    {
        internal static IList<IRouteLeg> RouteLegsFromNavDataLegs(IList<Leg> leg, MagneticTileManager mcManager)
        {
            List<IRouteLeg> routeLegs = new List<IRouteLeg>();

            IEnumerator<Leg> enumerator = leg.GetEnumerator();

            Leg previousLeg = null;

            while (enumerator.MoveNext())
            {
                Leg currentLeg = enumerator.Current;

                switch (currentLeg.Type)
                {
                    case LegType.INITIAL_FIX:
                        // Ignore leg. Will be useful only for the next leg to process.
                        break;
                    case LegType.TRACK_TO_FIX:
                        // We SHOULD have a previous leg.
                        {
                            FmsPoint point1 = FmsPointFromNavDataLeg(previousLeg);
                            FmsPoint point2 = FmsPointFromNavDataLeg(currentLeg);

                            routeLegs.Add(new TrackToFixLeg(point1, point2));
                            break;
                        }
                    case LegType.COURSE_TO_FIX:
                        {
                            FmsPoint endPoint = FmsPointFromNavDataLeg(currentLeg);

                            routeLegs.Add(new CourseToFixLeg(endPoint, BearingTypeEnum.MAGNETIC, currentLeg.OutboundMagneticCourse, mcManager));
                            break;
                        }
                    case LegType.DIRECT_TO_FIX:
                        {
                            FmsPoint directPoint = FmsPointFromNavDataLeg(currentLeg);

                            routeLegs.Add(new DirectToFixLeg());
                            break;
                        }
                    case LegType.FIX_TO_ALT:
                        {
                            FmsPoint startPoint = FmsPointFromNavDataLeg(previousLeg);
                            new FixToAltLeg(startPoint, BearingTypeEnum.MAGNETIC, currentLeg.OutboundMagneticCourse, currentLeg.UpperAltitudeConstraint)
                        }
                }

                previousLeg = currentLeg;
            }
        }

        internal static FmsPoint FmsPointFromNavDataLeg(Leg leg)
        {
            FmsPoint point = new FmsPoint(new RouteWaypoint(leg.EndPoint),
                leg.EndPointDescription.IsFlyOver ? RoutePointTypeEnum.FLY_OVER : RoutePointTypeEnum.FLY_BY
                );

            point.LowerAltitudeConstraint = leg.LowerAltitudeConstraint;
            point.UpperAltitudeConstraint = leg.UpperAltitudeConstraint;

            point.SpeedConstraint = leg.SpeedRestriction;
            switch (leg.SpeedType)
            {
                case SpeedRestrictionType.BELOW:
                    point.SpeedConstraintType = ConstraintType.LESS;
                    break;
                case SpeedRestrictionType.AT:
                    point.SpeedConstraintType = ConstraintType.EXACT; 
                    break;
                case SpeedRestrictionType.ABOVE:
                    point.SpeedConstraintType = ConstraintType.MORE;
                    break;
                default:
                    point.SpeedConstraintType = ConstraintType.FREE;
                    break;
            }

            return point;
        }

    }
}
