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
		public Stack<ExceptionHandler> ExceptionHandlers { get; private set; }

		public ThreadState(int size)
		{
			Stack = new StackCell[size];
			StackPosition = 0;
			BasePosition = 0;
			ClosureStack = new LinkedList<StackCell>();
			ExceptionHandlers = new Stack<ExceptionHandler>();
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

		public void PushExceptionHandler()
		{
			ExceptionHandler e = new ExceptionHandler(StackPosition, ClosureStack.Count, CurrentInstruction.InstructionIndex, BasePosition);
			ExceptionHandlers.Push(e);
		}

		public void HandleException(Exception e)
		{
			ExceptionHandler handler = ExceptionHandlers.Pop();

			while (StackPosition > handler.StackPosition)
			{
				PopStack();
			}

			while (ClosureStack.Count > handler.ClosureStackPosition)
			{
				ClosureStack.RemoveFirst();
			}

			BasePosition = handler.BasePosition;
			CurrentInstruction.InstructionIndex = handler.InstructionNumber + 1;

			VarArgs retArgs = new VarArgs();
			retArgs.PushArg("foo bar");
			retArgs.PushArg(false);
			PushStack(retArgs);
		}
	}
}
