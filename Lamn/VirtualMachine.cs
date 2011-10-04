﻿using System;
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
			public const UInt32 ADD   = 0x02000000;
			
			public const UInt32 RET   = 0x03000000;
			public const UInt32 CALL  = 0x04000000;

			public const UInt32 POPVARGS = 0x05000000;

			public const UInt32 OPCODE_MASK = 0xFF000000;
			public const UInt32 OP1_MASK    = 0x00FFF000;
			public const UInt32 OP2_MASK    = 0x00000FFF;
			public const int OPCODE_SHIFT   = 24;
			public const int OP1_SHIFT      = 12;
			public const int OP2_SHIFT      = 0;

			public static UInt32 MakeLOADK(int index)
			{
				return LOADK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeCALL(int numArgs)
			{
				return CALL | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakePOPVARGS(int numArgs)
			{
				return POPVARGS | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeRET(int numArgs)
			{
				return RET | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
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

		public class VarArgs
		{
			public LinkedList<Object> Args { get; private set; }

			public VarArgs()
			{
				Args = new LinkedList<Object>();	
			}

			public Object PopArg()
			{
				if (Args.Count == 0) { return null; }

				Object first = Args.First.Value;
				Args.RemoveFirst();
				return first;
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

		public class StackCell
		{
			public Object contents;
		}

		public class VMException : Exception { }

		private List<Function> FunctionMap { get; set; }

		private StackCell[] Stack { get; set; }
		private InstructionPointer CurrentIP { get; set; }

		private const int stackSize = 512;

		private int baseIndex = 0;
		private int stackIndex = 0;

		public VirtualMachine()
		{
			FunctionMap = new List<Function>();
			Stack = new StackCell[stackSize];
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
						DoRET(currentInstruction);
						break;
					case OpCodes.CALL:
						DoCALL(currentInstruction);
						break;
					case OpCodes.POPVARGS:
						DoPOPVARGS(currentInstruction);
						break;
					default:
						throw new VMException();
				}
			}
		}

		public void Call()
		{
			DoCALL(0);
		}

		#region Manipulate Stacks
		public void PushStack(Object o) {
			Stack[stackIndex] = new StackCell() { contents = o };
			stackIndex++;
		}

		public Object PopStack()
		{
			stackIndex--;
			Object retValue = Stack[stackIndex].contents;
			Stack[stackIndex] = null;
			return retValue;
		}

		public Object PeekStack()
		{
			return Stack[stackIndex - 1].contents;
		}

		public Object GetStackAtIndex(int index)
		{
			return Stack[index].contents;
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

		private void DoRET(UInt32 instruction)
		{
			int oldBaseIndex = (int)GetStackAtIndex(baseIndex);

			VarArgs args = new VarArgs();
			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			for (int i = 0; i < numArgs; i++)
			{
				args.Args.AddLast(PopStack());
			}

			while (stackIndex > baseIndex)
			{
				PopStack();
			}

			baseIndex = oldBaseIndex;

			CurrentIP = (InstructionPointer)PopStack();
			if (CurrentIP != null)
			{
				CurrentIP.InstructionIndex++;
			}

			PushStack(args);
		}

		private void DoCALL(UInt32 instruction)
		{
			Object o = PopStack();

			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			VarArgs args = new VarArgs();
			for (int i = 0; i < numArgs; i++)
			{
				args.Args.AddLast(PopStack());
			}
			
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

			PushStack(CurrentIP);
			PushStack(baseIndex);
			baseIndex = stackIndex - 1;
			PushStack(args);

			CurrentIP = newIP;
		}

		private void DoPOPVARGS(UInt32 instruction)
		{
			VarArgs vargs = (VarArgs)PeekStack();

			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < numArgs; i++)
			{
				PushStack(vargs.PopArg());
			}

			CurrentIP.InstructionIndex++;
		}
		#endregion
	}
}