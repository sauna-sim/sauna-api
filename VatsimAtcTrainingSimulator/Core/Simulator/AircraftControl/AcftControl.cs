using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class AcftControl
    {
        public ILateralControlInstruction CurrentLateralInstruction { get; set; }
        public ILateralControlInstruction ArmedLateralInstruction { get; set; }
        public IVerticalControlInstruction CurrentVerticalInstruction { get; set; }
        private List<IVerticalControlInstruction> ArmedVerticalInstructions { get; set; }
        private object ArmedVerticalLock = new object();

        public AcftControl(ILateralControlInstruction lateralInstruction, IVerticalControlInstruction verticalInstruction)
        {
            CurrentLateralInstruction = lateralInstruction;
            CurrentVerticalInstruction = verticalInstruction;

            lock (ArmedVerticalLock)
            {
                ArmedVerticalInstructions = new List<IVerticalControlInstruction>();
            }
        }

        public AcftControl() : this(new HeadingHoldInstruction(0), new AltitudeHoldInstruction(10000)) { }

        public bool AddArmedVerticalInstruction(IVerticalControlInstruction instr)
        {
            lock (ArmedVerticalLock)
            {
                foreach (IVerticalControlInstruction elem in ArmedVerticalInstructions)
                {
                    if (elem.Type == instr.Type)
                    {
                        return false;
                    }
                }

                ArmedVerticalInstructions.Add(instr);
            }
            return true;
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            // Check if we should activate armed instructions
            if (ArmedLateralInstruction != null && ArmedLateralInstruction.ShouldActivateInstruction(position, posCalcInterval))
            {
                CurrentLateralInstruction = ArmedLateralInstruction;
                ArmedLateralInstruction = null;
            }

            lock (ArmedVerticalLock)
            {
                foreach (IVerticalControlInstruction armedInstr in ArmedVerticalInstructions)
                {
                    if (armedInstr.ShouldActivateInstruction(position, posCalcInterval))
                    {
                        CurrentVerticalInstruction = armedInstr;
                        ArmedVerticalInstructions.Remove(armedInstr);
                        break;
                    }
                }
            }

            // Update position
            CurrentLateralInstruction.UpdatePosition(ref position, posCalcInterval);
            CurrentVerticalInstruction.UpdatePosition(ref position, posCalcInterval);

            // Recalculate values
            position.UpdatePosition();
        }
    }
}
