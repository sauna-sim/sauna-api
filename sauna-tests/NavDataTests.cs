using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NavData_Interface.DataSources;
using NavData_Interface.Objects.Fixes.Waypoints;
using NavData_Interface.Objects.LegCollections.Procedures;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace sauna_tests
{
    [Explicit]
    public class NavDataTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestReader() 
        {
            var filePath = "e_dfd_2301.s3db";
            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filePath,
                Version = 3,
                ReadOnly = true
            }.ToString();

            var connection = new SQLiteConnection(connectionString);
            connection.Open();

            SQLiteCommand command = new SQLiteCommand(connection);

            command.CommandText = $"SELECT * FROM tbl_sids WHERE airport_identifier == @airport AND procedure_identifier = @sid";

            command.Parameters.AddWithValue("@airport", "EGKK");
            command.Parameters.AddWithValue("@sid", "LAM6M");

            var reader = command.ExecuteReader();

            int i = 0;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (reader.Read()) { i++; }
            stopwatch.Stop();

            Console.WriteLine(stopwatch.Elapsed / i);
        }

        [Test]
        public void TestReader2()
        {
            var filePath = "e_dfd_2301.s3db";
            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filePath,
                Version = 3,
                ReadOnly = true
            }.ToString();

            var connection = new SQLiteConnection(connectionString);
            connection.Open();

            SQLiteCommand command = new SQLiteCommand("SELECT * FROM tbl_sids", connection);

            var reader = command.ExecuteReader();

            int i = 0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (reader.Read()) { i++; }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.Elapsed/i);
        }

        [Test]
        public static void TestLoadAllSids()
        {
            var filePath = "e_dfd_2301.s3db";
            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filePath,
                Version = 3,
                ReadOnly = true
            }.ToString();

            var connection = new SQLiteConnection(connectionString);

            connection.Open();

            var query = new SQLiteCommand("SELECT DISTINCT airport_identifier, procedure_identifier FROM tbl_sids", connection);
            var reader = query.ExecuteReader();
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");

            reader.Read();

            while (reader.HasRows)
            {
                var sid = navDataInterface.GetSidByAirportAndIdentifier(reader["airport_identifier"].ToString(), reader["procedure_identifier"].ToString());

                reader.Read();
            }
        }

        [Test]
        public static void TestLoadAllStars()
        {
            var filePath = "e_dfd_2301.s3db";
            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filePath,
                Version = 3,
                ReadOnly = true
            }.ToString();

            var connection = new SQLiteConnection(connectionString);

            connection.Open();

            var query = new SQLiteCommand("SELECT DISTINCT airport_identifier, procedure_identifier FROM tbl_stars", connection);
            var reader = query.ExecuteReader();
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");

            reader.Read();

            while (reader.HasRows) 
            {
                var sid = navDataInterface.GetStarByAirportAndIdentifier(reader["airport_identifier"].ToString(), reader["procedure_identifier"].ToString());

                reader.Read();
            }
        }

        [Test]
        public static void TestGetStarFromAirportIdentifier()
        {
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");
            var star = navDataInterface.GetStarByAirportAndIdentifier("EGKK", "KIDL1G");

            Console.WriteLine(star);

            star = navDataInterface.GetStarByAirportAndIdentifier("KIAD", "CAVLR4");
            star.selectTransition("DORRN");
            star.selectRunwayTransition("01R");

            Console.WriteLine(star);

            foreach (var leg in star)
            {
                Console.WriteLine(leg);
            }
        }

        [Test]
        public static void TestGetSidFromAirportIdentifier()
        {
            var navDataInterface = new DFDSource("e_dfd_2301.s3db");

            var sid = navDataInterface.GetSidByAirportAndIdentifier("EGKK", "LAM6M");

            Console.WriteLine(sid);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            sid = navDataInterface.GetSidByAirportAndIdentifier("KIAD", "JCOBY4");
            sid.selectRunwayTransition("01R");
            sid.selectTransition("AGARD");

            stopwatch.Stop();

            Console.WriteLine(sid);

            foreach (var leg in sid)
            {
                Console.WriteLine(leg);
            }

            Console.WriteLine($"JCOBY time {stopwatch.Elapsed}");
        }

        [Test]
        public static void TestGetClosestAirportWithinRadius1()
        {
            var navDataInterface = new DFDSource("e_dfd_2311.s3db");
            var westOfLoughNeagh = new GeoPoint(54.686784, -6.544965);
            var closestAirport = navDataInterface.GetClosestAirportWithinRadius(westOfLoughNeagh, Length.FromMeters(30_000));
            Assert.That(closestAirport.Identifier, Is.EqualTo("EGAL"));
        }

        [Test]
        public static void TestGetClosestAirportWithinRadius2()
        {
            var navDataInterface = new DFDSource("e_dfd_2311.s3db");
            var point = new GeoPoint(-17.953955, -179.99);
            var closestAirport = navDataInterface.GetClosestAirportWithinRadius(point, Length.FromMeters(100_000));
            Assert.That(closestAirport.Identifier, Is.EqualTo("NFMO"));
        }

        //[Test]
        //public static void TestGetClosestAirportWithinRadius3()
        //{
        //    var navDataInterface = new DFDSource("e_dfd_2311.s3db");
        //    var point = new GeoPoint(-89.75, -142.284902);
        //    var closestAirport = navDataInterface.GetClosestAirportWithinRadius(point, 100_000);
        //    Assert.That(closestAirport.Identifier, Is.EqualTo("NZSP"));
        //}

        [Test]
        public static void TestGetClosestAirportWithinRadius4()
        {
            var navDataInterface = new DFDSource("e_dfd_2311.s3db");
            var point = new GeoPoint(-27.058760, 83.227773);
            var closestAirport = navDataInterface.GetClosestAirportWithinRadius(point, Length.FromMeters(100_000));
            Assert.That(closestAirport, Is.Null);
        }

        [Test]
        public static void TestCombinedSourcePriorities()
        {
            var navigraphSource = new DFDSource("e_dfd_2301.s3db");
            var custom1 = new InMemorySource("custom1");
            var custom2 = new InMemorySource("custom2");

            var navdataInterface = new CombinedSource("test_sources", navigraphSource, custom1, custom2);

            var badWillo = new Waypoint("WILLO", new GeoPoint(50.985, -0.1912));
            custom1.AddFix(badWillo);

            var weirdPoint1 = new Waypoint("NOEXIST", new GeoPoint(0.001, 0.001));
            custom1.AddFix(weirdPoint1);

            var weirdPoint2 = new Waypoint("NOEXIST", new GeoPoint(0, 0));

            custom2.AddFix(weirdPoint2);

            var willoResults = navdataInterface.GetFixesByIdentifier("WILLO");
            Assert.That(willoResults[0] != badWillo && willoResults[0] != null);

            var weirdPointResults = navdataInterface.GetFixesByIdentifier("NOEXIST");
            Assert.That(weirdPointResults[0] == weirdPoint1 && weirdPointResults.Count == 1);

            navdataInterface.ChangePriority("custom1", 100);
            weirdPointResults = navdataInterface.GetFixesByIdentifier("NOEXIST");
            Assert.That(weirdPointResults[0] == weirdPoint2 && weirdPointResults.Count == 1);
        }
    }
}
