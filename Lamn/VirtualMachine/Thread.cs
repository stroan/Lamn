using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class Thread
	{
		public Closure ThreadClosure { get; private set; } 

		public ThreadState State { get; private set; }

		public Thread(Closure closure, int stackSize)
		{
			ThreadClosure = closure;
			State = new ThreadState(stackSize);
			State.CurrentInstruction = new InstructionPointer(closure.Func, closure.ClosedVars, 0);
			State.CurrentInstruction.InstructionIndex = -1;
		}
	}
}
