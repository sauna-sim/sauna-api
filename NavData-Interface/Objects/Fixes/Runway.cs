using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes
{
    public class Runway : Fix
    {
        public string AirportIdentifier { get; }

        public Angle Gradient { get; }

        public Bearing MagneticBearing { get; }

        public Bearing TrueBearing { get; }

        public Length ThresholdElevation { get; }

        public Length DisplacedThresholdLength { get; }

        public Length ThresholdCrossingHeight { get; }

        public Length Length { get; }

        public Length Width { get; }

        // Localizer Stuff

        public Runway(
            string identifier, 
            GeoPoint location, 
            string airportIdentifier, 
            Angle gradient, 
            Bearing magneticBearing, 
            Bearing trueBearing, 
            Length thresholdElevation, 
            Length displacedThresholdLength, 
            Length thresholdCrossingHeight,
            Length length, 
            Length width) : base(identifier, identifier, location)
        {
            AirportIdentifier = airportIdentifier;
            Gradient = gradient;
            MagneticBearing = magneticBearing;
            TrueBearing = trueBearing;
            ThresholdElevation = thresholdElevation;
            DisplacedThresholdLength = displacedThresholdLength;
            ThresholdCrossingHeight = thresholdCrossingHeight;
            Length = length;
            Width = width;
        }
    }
}
