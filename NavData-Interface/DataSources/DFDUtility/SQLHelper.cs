using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility
{
    internal static class SQLHelper
    {
        internal static GeoPoint locationFromColumns(SQLiteDataReader reader, string latColumn, string lonColumn)
        {
            return new GeoPoint(
                Double.Parse(reader[latColumn].ToString()),
                Double.Parse(reader[lonColumn].ToString())
                );
        }

        internal static WaypointType waypointTypeFromTypeString(string typeString, bool isTerminal)
        {
            Kind wpClass;

            if (typeString.Length != 3)
            {
                throw new FormatException("The waypoint type is invalid because the type field is too long");
            }

            switch (typeString[0])
            {
                case 'A':
                    wpClass = Kind.RFCenter;
                    break;
                case 'C':
                    wpClass = Kind.Rnav;
                    break;
                case 'I':
                    wpClass = Kind.Other;
                    break;
                case 'M':
                    wpClass = Kind.Mm;
                    break;
                case 'N':
                    wpClass = Kind.Ndb;
                    break;
                case 'O':
                    wpClass = Kind.Om;
                    break;
                case 'R':
                    wpClass = Kind.Other;
                    break;
                case 'U':
                    wpClass = Kind.Other;
                    break;
                case 'V':
                    wpClass = Kind.Vfr;
                    break;
                case 'W':
                    wpClass = Kind.Rnav;
                    break;

                default:
                    throw new FormatException("The first column of the waypoint type is invalid");
            }

            if (wpClass == Kind.Vfr)
            {
                // For VRPs, the remaining columns are not relevant and will be blank.
                return new WaypointType(wpClass);
            }

            bool isIaf = false, isFaf = false, isIf = false, isFac = false, isMaf = false, isOceanicEntryExit = false, isStepdownFix = false;

            switch (typeString[1])
            {
                case 'A':
                    isFaf = true;
                    break;
                case 'B':
                    isFaf = true;
                    isIaf = true;
                    break;
                case 'C':
                    isFac = true;
                    break;
                case 'D':
                    isIf = true;
                    break;
                case 'E':
                    break;
                case 'F':
                    break;
                case 'I':
                    isIaf = true;
                    break;
                case 'K':
                    isFac = true;
                    isIaf = true;
                    break;
                case 'L':
                    isFac = true;
                    isIf = true;
                    break;
                case 'M':
                    isMaf = true;
                    break;
                case 'N':
                    isIaf = true;
                    isMaf = true;
                    break;
                case 'O':
                    isOceanicEntryExit = true;
                    break;
                case 'P':
                    if (isTerminal)
                    {
                        isStepdownFix = true;
                    }

                    break;
                case 'S':
                    if (isTerminal)
                    {
                        isStepdownFix = true;
                    }
                    break;
                case 'U':
                    break;
                case 'V':
                    break;
                case 'W':
                    break;
                case ' ':
                    break;
                default:
                    throw new FormatException("The second column of the waypoint typeString is invalid");
            }

            return new WaypointType(wpClass, isIaf, isFaf, isIf, isMaf, isFac, isStepdownFix, isOceanicEntryExit);
        }
    }
}