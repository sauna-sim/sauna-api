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

        private SidEnumerator _enumerator;

        public Sid(List<Transition> rwyTransitions, List<Leg> commonLegs, List<Transition> transitions) 
        { 
            _rwyTransitions = rwyTransitions;
            _commonLegs = commonLegs;
            _transitions = transitions;
        }

        public Sid(List<Transition> rwyTransitions, List<Leg> commonLegs, List<Transition> transitions, string runway, string transition) : this(rwyTransitions, commonLegs, transitions)
        {
            selectRunwayTransition(runway);
            selectTransition(transition);
        }

        public override IEnumerator<Leg> GetEnumerator()
        {
            return _enumerator;
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

                    if (_selectedTransition != null)
                    {
                        if (_enumerator == null)
                        {
                            _enumerator = new SidEnumerator(this);
                        }
                        else
                        {
                            _enumerator.Reset();
                        }
                    }
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
                    if (_selectedRwyTransition != null)
                    {
                        if (_enumerator == null)
                        {
                            _enumerator = new SidEnumerator(this);
                        }
                        else
                        {
                            _enumerator.Reset();
                        }
                    }
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
                        _cursor = 0;
                        goto case 0;
                    case 0:
                        _cursor++;
                        if (_cursor >= _parent._selectedRwyTransition.legs.Count)
                        {
                            _state++;
                            _cursor = 0;
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
                            _cursor = 0;
                            goto case 2;
                        }
                        else
                        {
                            return true;
                        }
                    case 2:
                        _cursor++;
                        if (_cursor >= _parent._selectedTransition.legs.Count)
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
    }
}
