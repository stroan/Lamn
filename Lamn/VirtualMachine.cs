using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class VirtualMachine
	{
		public class OpCodes
		{
			public const UInt32 LOADK = 0x01000000;
			public const UInt32 ADD = 0x02000000;
			public const UInt32 RET = 0x03000000;

			public const UInt32 OPCODE_MASK = 0xFF000000;
			public const UInt32 OP1_MASK = 0x00FFF000;
			public const UInt32 OP2_MASK = 0x00000FFF;
			public const int OPCODE_SHIFT = 24;
			public const int OP1_SHIFT = 12;
			public const int OP2_SHIFT = 0;

			public static UInt32 MakeLoadConstant(int index)
			{
				return LOADK | ((UInt32)index << OP1_SHIFT) & OP1_MASK;
			}
		}

		public class Function
		{
			public UInt32[] Bytecodes { get; private set; }
			public Object[] Constants { get; private set; }
			public int Index { get; private set; }

			public Function(UInt32[] bytecodes, Object[] constants, int index)
			{
				Bytecodes = bytecodes;
				Constants = constants;
				Index = index;
			}
		}

		public class Closure
		{
			public int FunctionIndex { get; private set; }

			public Closure(int functionIndex)
			{
				FunctionIndex = functionIndex;
			}
		}

		public class InstructionPointer
		{
			public Function CurrentFunction { get; private set; }
			public int InstructionIndex { get; set; }
			public Stack<Object> ExpressionStack { get; set; }

			public UInt32 CurrentInstruction { get { return CurrentFunction.Bytecodes[InstructionIndex]; } }

			public InstructionPointer(Function currentFunction, int instructionIndex)
			{
				CurrentFunction = currentFunction;
				InstructionIndex = instructionIndex;
				ExpressionStack = new Stack<object>();
			}
		}

		public class VMException : Exception { }

		private List<Function> FunctionMap { get; set; }

		private List<Object> Stack { get; set; }
		private Stack<InstructionPointer> CallStack { get; set; }
		private InstructionPointer CurrentIP { get; set; }

		public VirtualMachine()
		{
			FunctionMap = new List<Function>();
			Stack = new List<Object>();
			CallStack = new Stack<InstructionPointer>();
		}

		public int RegisterFunction(UInt32[] bytecodes, Object[] constants)
		{
			int index = FunctionMap.Count;
			FunctionMap.Add(new Function(bytecodes, constants, index));
			return index;
		}

		public void Run()
		{
			while (CurrentIP != null)
			{
				UInt32 currentInstruction = CurrentIP.CurrentInstruction;
				UInt32 opCode = currentInstruction & OpCodes.OPCODE_MASK;
				switch (opCode)
				{
					case OpCodes.LOADK:
						DoLOADK(currentInstruction);
						break;
					case OpCodes.ADD:
						DoADD(currentInstruction);
						break;
					case OpCodes.RET:
						DoRET();
						break;
					default:
						throw new VMException();
				}
			}
		}

		public void Call()
		{
			Object o = PopStack();

			InstructionPointer newIP = null;

			if (o is Closure)
			{
				Function f = FunctionMap[((Closure)o).FunctionIndex];
				newIP = new InstructionPointer(f, 0);
			}
			else
			{
				throw new VMException();
			}

			CallStack.Push(CurrentIP);
			CurrentIP = newIP;
		}

		#region Manipulate Stacks
		public void PushStack(Object o) {
			Stack.Add(o);
		}

		public Object PopStack()
		{
			Object retValue = GetStack(0);
			Stack.RemoveAt(Stack.Count - 1);
			return retValue;
		}

		public Object GetStack(int index)
		{
			return Stack[Stack.Count - (index + 1)];
		}
		#endregion

		#region Execute instructions
		private void DoLOADK(UInt32 instruction)
		{
			UInt32 constantIndex = (instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT;
			PushStack(CurrentIP.CurrentFunction.Constants[constantIndex]);
			CurrentIP.InstructionIndex++;
		}

		private void DoADD(UInt32 instruction)
		{
			Double op1 = (Double)PopStack();
			Double op2 = (Double)PopStack();
			PushStack(op1 + op2);
			CurrentIP.InstructionIndex++;
		}

		private void DoRET()
		{
			CurrentIP = CallStack.Pop();
			if (CurrentIP != null)
			{
				CurrentIP.InstructionIndex++;
			}
		}
		#endregion
	}
}
