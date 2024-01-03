using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes
{
    public class Runway : Fix
    {
        public string AirportIdentifier { get; }

        public double Gradient { get; }

        public double MagneticBearing { get; }

        public double TrueBearing { get; }

        public int ThresholdElevation { get; }

        public int DisplacedThresholdLength { get; }

        public int ThresholdCrossingHeight { get; }

        public int Length { get; }

        public int Width { get; }

        // Localizer Stuff

        // Surface Code

        public Runway(
            string identifier, 
            GeoPoint location, 
            string airportIdentifier, 
            double gradient, 
            double magneticBearing, 
            double trueBearing, 
            int thresholdElevation, 
            int displacedThresholdLength, 
            int thresholdCrossingHeight,
            int length, 
            int width) : base(identifier, identifier, location)
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
