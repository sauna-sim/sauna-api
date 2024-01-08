using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.LegCollections.Legs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Procedures
{
    public class Transition : LegCollection
    {
        internal List<Leg> legs;

        public string TransitionIdentifier { get; }

        public Length TransitionAltitude { get; }

        public override IEnumerator<Leg> GetEnumerator()
        {
            return legs.GetEnumerator();
        }

        public Transition(List<Leg> legs, string transitionIdentifier, Length transitionAltitude)
        {
            this.legs = legs;
            TransitionIdentifier = transitionIdentifier;
            TransitionAltitude = transitionAltitude;
        }
    }
}
