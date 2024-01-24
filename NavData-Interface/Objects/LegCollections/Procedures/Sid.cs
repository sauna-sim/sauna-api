using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.LegCollections.Legs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Procedures
{
    public class Sid : TerminalProcedure
    {

        private List<Transition> _rwyTransitions;

        protected override List<Transition> FirstTransitions => _rwyTransitions;

        
    }
}
