namespace AselAtcTrainingSim.AselApi.ApiObjects.Commands
{
    public class AltitudeCommandRequest
    {
        public string Callsign { get; set; }
        public int Altitude { get; set; }
        public bool PressureAlt { get; set; }
        public double Pressure { get; set; }
        public bool PressureInInHg { get; set; }
    }
}