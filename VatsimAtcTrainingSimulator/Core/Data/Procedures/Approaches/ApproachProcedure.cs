using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Data.Procedures.Approaches
{
    public enum ApproachType
    {
        ILS,
        LOC,
        RNAV,
        RNP,
        GPS,
        VOR,
        NDB,
        LDA
    }

    public class ApproachProcedure
    {
        private ApproachType _type;
        private char _letter;
        private string _rwy;
        private ApproachMinimums _mins;
        private ApproachMinimums _circleMins;
        private List<ProcedureSegment> _initSegments;
        private ProcedureSegment _finalSegment;
        private List<ProcedureSegment> _maSegments;

        private ApproachProcedure(ApproachType type, char letter, string rwy, ApproachMinimums mins, ApproachMinimums circleMins, List<ProcedureSegment> initSegments, ProcedureSegment finalSegment, ProcedureSegment defaultMaSegment, List<ProcedureSegment> alternateMaSegments)
        {
            _type = type;
            _letter = letter;
            _rwy = rwy;
            _mins = mins;
            _circleMins = circleMins;
            _initSegments = initSegments;
            _finalSegment = finalSegment;
            _maSegments = new List<ProcedureSegment>
            {
                defaultMaSegment
            };
            _maSegments.AddRange(alternateMaSegments);
        }

        public ApproachType Type => _type;

        public char Letter => _letter;

        public string Runway => _rwy;

        public ApproachMinimums Minimums => _mins;

        public ApproachMinimums CirclingMinimums => _circleMins;

        public List<ProcedureSegment> InitialSegments => _initSegments;

        public ProcedureSegment FinalSegment => _finalSegment;

        public List<ProcedureSegment> MissedApproachSegments => _maSegments;

        public ProcedureSegment DefaultMissedSegment => _maSegments.Count >= 1 ? _maSegments[0] : null;
    }
}
