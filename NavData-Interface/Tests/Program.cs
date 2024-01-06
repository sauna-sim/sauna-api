using System;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface;
using NavData_Interface.DataSources;

namespace NavData_Interface_Tests
{
    public class Tests
    {
        public static void Main()
        {
            TestGetClosestWaypoint();
            Console.WriteLine();
            TestGetNavaidByIdentifier("LAM");
            Console.WriteLine();
            TestGetNavaidByIdentifier("LBA");
            Console.WriteLine();
            TestGetAirportByIdentifier("EGKK");
            Console.WriteLine();
            TestGetAirportByIdentifier("KLAX");
            Console.WriteLine();
            TestGetLocalizer("LEMD", "32L");
            Console.WriteLine();
            TestGetLocalizer("EGKK", "26L");
            Console.WriteLine();
        }

        public static void TestGetWaypointLocation()
        {
            var dataSource = new DFDSource("invalid_airac.s3db");
            var waypoints = dataSource.GetWaypointsByIdentifier("WILLO");


            foreach (var waypoint in waypoints)
            {
                Console.WriteLine($"Waypoint {waypoint.Identifier} is located at ({waypoint.Location.Lat}, {waypoint.Location.Lon})");
            }
        }

        public static void TestGetClosestWaypoint()
        {
            var navDataInterface = new NavDataInterface(new DFDSource("e_dfd_2301.s3db"));
            GeoPoint point = new(51.5074, -0.1278); // Example location (London)
            string identifier = "WILLO";
            var closestWaypoint = navDataInterface.GetClosestFixByIdentifier(point, identifier);
                Console.WriteLine($"The closest waypoint to ({point.Lat}, {point.Lon}) with identifier {identifier} is located at ({closestWaypoint.Location.Lat}, {closestWaypoint.Location.Lon})");
        }
        public static void TestGetNavaidByIdentifier(string navaidIdentifier)
        {
            // Assuming you have a NavaidDataSource class that provides access to Navaid data
            var navaidDataSource = new DFDSource("e_dfd_2301.s3db");

            // Replace "ICAO_CODE" with the ICAO code of the Navaid you want to test

            var navaids = navaidDataSource.GetNavaidsByIdentifier(navaidIdentifier);

            if (navaids.Count > 0)
            {
                foreach (var navaid in navaids)
                {
                    Console.WriteLine($"Navaid {navaid.Identifier}, named {navaid.Name} is located at ({navaid.Location.Lat}, {navaid.Location.Lon})");
                }
             }
            else
            {
                Console.WriteLine($"Navaid with ICAO code {navaidIdentifier} not found.");
            }
        }

        public static void TestGetAirportByIdentifier(string identifier)
        {
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");

            var airport = navDataInterface.GetAirportByIdentifier(identifier);

            if (airport == null)
            {
                throw new Exception();
            } else
            {
                Console.WriteLine($"Airport {airport.Identifier}, named {airport.Name}, is located ({airport.Location.Lat}, {airport.Location.Lat}).");
            }
        }

        public static void TestGetLocalizer(string airport, string runway)
        {
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");

            var localizer = navDataInterface.GetLocalizerFromAirportRunway(airport, runway);

            if (localizer == null)
            {
                throw new Exception();
            } else
            {
                Console.WriteLine(localizer);
            }
        }
    }
}