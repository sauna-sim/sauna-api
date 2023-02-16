namespace AselAtcTrainingSim.AselApi.ApiObjects.Aircraft
{
    public class AircraftPositionRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double IndicatedAltitude { get; set; }
        public bool IsPressureAltitude { get; set; } = false;
        public bool IsMachNumber { get; set; } = false;
        public double IndicatedSpeed { get; set; }
        public double MagneticHeading { get; set; }
    }
}
