using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VatsimAtcTrainingSimulator.Core.Data.Loaders
{
    public static class LevelD767Loader
    {
        public static void LoadDataForAirport(string airportIcao)
        {
            string navdataPath = "%APPDATA%\\PShivaraman\\VatsimAtcTrainingSimulator\\NavData\\navdata";
            string fileName = $"{navdataPath}\\{airportIcao}.xml";

            XDocument navdataXml = XDocument.Load(fileName);
            XElement airport = navdataXml.Root.Element("Airport");

            if (airport == null || airport.Attribute("ICAOcode") == null || airport.Attribute("ICAOcode").Value != airportIcao)
            {
                throw new InvalidOperationException("The navdata file was invalid!");
            }

            // Loop through elements
            foreach (XElement procElem in airport.Elements())
            {
                if (procElem.Name == "Sid")
                {

                } else if (procElem.Name == "Star")
                {

                } else if (procElem.Name == "Approach")
                {

                }
            }
        }
    }
}
