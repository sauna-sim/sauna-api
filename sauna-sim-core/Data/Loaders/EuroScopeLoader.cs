using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Data.Loaders
{
    public static class EuroScopeLoader
    {
        public static void ReadVatsimPosFlag(int posdata, out double hdg, out double bank, out double pitch, out bool onGround)
        {
            // Read position data
            posdata >>= 1;
            onGround = (posdata & 0x1) != 0;
            posdata >>= 1;
            hdg = posdata & 0x3FF;
            hdg = (hdg * 360.0) / 1024.0;
            posdata >>= 10;
            bank = posdata & 0x3FF;
            bank = (bank * 180.0) / 512;
            posdata >>= 10;
            pitch = posdata & 0x3FF;
            pitch = (pitch * 90.0) / 256.0;
        }
    }
}
