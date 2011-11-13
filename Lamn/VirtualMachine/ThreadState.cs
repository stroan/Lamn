using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class ThreadState
	{
		public StackCell[] Stack { get; private set; }
		public LinkedList<StackCell> ClosureStack { get; set; }
		public int StackPosition { get; private set; }
		public int BasePosition { get; set; }
		public InstructionPointer CurrentInstruction { get; set; }

		public ThreadState(int size)
		{
			Stack = new StackCell[size];
			StackPosition = 0;
			BasePosition = 0;
			ClosureStack = new LinkedList<StackCell>();
		}

		public void PushStack(Object o)
		{
			Stack[StackPosition] = new StackCell() { contents = o };
			StackPosition++;
		}

		public void PushStackUnboxed(StackCell s)
		{
			Stack[StackPosition] = s;
			StackPosition++;
		}

		public Object PopStack()
		{
			StackPosition--;
			Object retValue = Stack[StackPosition].contents;
			Stack[StackPosition] = null;
			return retValue;
		}

		public Object PeekStack()
		{
			return Stack[StackPosition - 1].contents;
		}

		public Object GetStackAtIndex(int index)
		{
			return Stack[index].contents;
		}

		public StackCell GetUnboxedAtIndex(int index)
		{
			return Stack[index];
		}
	}
}
