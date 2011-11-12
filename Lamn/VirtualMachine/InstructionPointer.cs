using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	class InstructionPointer
	{
		public Function CurrentFunction { get; private set; }
		public int InstructionIndex { get; set; }
		public StackCell[] ClosedVars { get; private set; }

		public UInt32 CurrentInstruction { get { return CurrentFunction.Bytecodes[InstructionIndex]; } }

		public InstructionPointer(Function currentFunction, StackCell[] closedVars, int instructionIndex)
		{
			CurrentFunction = currentFunction;
			ClosedVars = closedVars;
			InstructionIndex = instructionIndex;
		}
	}
}
