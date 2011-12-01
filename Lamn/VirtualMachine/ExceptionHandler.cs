using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class ExceptionHandler
	{
		public int StackPosition { get; private set; }
		public int ClosureStackPosition { get; private set; }
		public int InstructionNumber { get; private set; }
		public int BasePosition { get; private set; }

		public ExceptionHandler(int stackPos, int closurePos, int instruction, int basePos)
		{
			StackPosition = stackPos;
			BasePosition = basePos;
			ClosureStackPosition = closurePos;
			InstructionNumber = instruction;
		}
	}
}
