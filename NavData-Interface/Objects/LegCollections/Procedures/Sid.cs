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

        private List<Leg> _commonLegs;

        private List<Transition> _transitions;

        private Transition _selectedRwyTransition;
        
        private Transition _selectedTransition;

        private Length _transitionAltitude;

        /// <summary>
        /// Returns the applicable transition altitude to use when flying this SID.
        /// <br></br>
        /// Returns null if the departure has multiple runway transitions and none are selected.
        /// </summary>
        public Length TransitionAltitude
        {
            get
            {
                if (_selectedRwyTransition != null)
                {
                    return _selectedRwyTransition.TransitionAltitude;
                }
                else
                {
                    return _transitionAltitude;
                }
            }
        }

        public Sid(string airportIdentifier, string routeIdentifier, List<Transition> rwyTransitions, List<Leg> commonLegs, List<Transition> transitions, Length transitionAltitude) : base(airportIdentifier, routeIdentifier)
        { 
            _rwyTransitions = rwyTransitions;
            _commonLegs = commonLegs;
            _transitions = transitions;
            _transitionAltitude = transitionAltitude;
        }

        public Sid(string airportIdentifier, string routeIdentifier, List<Transition> rwyTransitions, List<Leg> commonLegs, List<Transition> transitions, Length transitionAltitude, string runway, string transition) : this(airportIdentifier, routeIdentifier, rwyTransitions, commonLegs, transitions, transitionAltitude)
        {
            selectRunwayTransition(runway);
            selectTransition(transition);
        }

        public override IEnumerator<Leg> GetEnumerator()
        {
            return new SidEnumerator(this);
        }

        public void selectRunwayTransition(string runwayIdentifier)
        {
            runwayIdentifier = ("RW" + runwayIdentifier.ToUpper()).PadRight(5);

            foreach (var transition in _rwyTransitions)
            {
                if (transition.TransitionIdentifier == runwayIdentifier ||
                    transition.TransitionIdentifier.EndsWith("B") && runwayIdentifier.Substring(0, 4) == transition.TransitionIdentifier.Substring(0, 4))
                {
                    _selectedRwyTransition = transition;
                    return;
                }
            }

            throw new ArgumentException("Runway transition not found");
        }

        public void selectTransition(string transitionIdentifier)
        {
            foreach (var transition in _transitions)
            {
                if (transition.TransitionIdentifier == transitionIdentifier)
                {
                    _selectedTransition = transition;
                    return;
                }
            }

            throw new ArgumentException("Transition not found");
        }

        private class SidEnumerator : IEnumerator<Leg>
        {
            private Sid _parent;

            private int _cursor = 0;

            private int _state = -1;

            public Leg Current
            {
                get
                {
                    switch (_state)
                    {
                        case -1:
                            return null;
                        case 0:
                            return _parent._selectedRwyTransition.legs[_cursor];
                        case 1:
                            return _parent._commonLegs[_cursor];
                        case 2:
                            return _parent._selectedTransition.legs[_cursor];
                        case 3:
                            return null;
                        default:
                            throw new IndexOutOfRangeException("Internal error in SID iterator");
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                switch (_state)
                {
                    case -1:
                        _state++;
                        _cursor = -1;
                        goto case 0;
                    case 0:
                        _cursor++;
                        if (_parent._selectedRwyTransition == null || _cursor >= _parent._selectedRwyTransition.legs.Count)
                        {
                            _state++;
                            _cursor = -1;
                            goto case 1;
                        } else
                        {
                            return true;
                        }
                    case 1:
                        _cursor++;
                        if (_cursor >= _parent._commonLegs.Count)
                        {
                            _state++;
                            _cursor = -1;
                            goto case 2;
                        }
                        else
                        {
                            return true;
                        }
                    case 2:
                        _cursor++;
                        if (_parent._selectedTransition == null || _cursor >= _parent._selectedTransition.legs.Count)
                        {
                            _state++;
                            _cursor = 0;
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    case 3:
                        return false;
                    default:
                        throw new IndexOutOfRangeException("Internal error in SID iterator");
                }
            }

            public void Reset()
            {
                _state = -1;
                _cursor = 0;
            }
            public SidEnumerator(Sid parent)
            {
                _parent = parent;
                _state = -1;
                _cursor = 0;
            }
        }

        public override string ToString()
        {
            var initialString = 
                $"SID: {AirportIdentifier} - {RouteIdentifier}\n" +
                $"Runway: {_selectedRwyTransition?.TransitionIdentifier} Transition: {_selectedTransition?.TransitionIdentifier}\n" +
                $"Transition Altitude: {TransitionAltitude}\n" +
                $"Runway transitions: \n\n";

            foreach (var transition in _rwyTransitions)
            {
                initialString = initialString + transition.ToString() + "\n---------------------------------------\n";
            }

            initialString += "\nCommon legs: \n";

            foreach (var leg in _commonLegs)
            {
                initialString = initialString + "\t" + leg.ToString() + "\n";
            }

            initialString += "\nTransitions: \n\n";

            foreach (var transition in _transitions)
            {
                initialString = initialString + transition.ToString() + "\n---------------------------------------\n";
            }

            return initialString;
        }
    }
}
