using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class ReturnPoint
	{
		public InstructionPointer instructionPointer;
		public int popArgs;

		public ReturnPoint(InstructionPointer ip, int a)
		{
			instructionPointer = ip;
			popArgs = a;
		}
	}
}
