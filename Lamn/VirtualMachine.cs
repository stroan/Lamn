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
			public const UInt32 ADD   = 0x02000000;
			
			public const UInt32 RET   = 0x03000000;
			public const UInt32 CALL  = 0x04000000;

			public const UInt32 POPVARGS = 0x05000000;

			public const UInt32 CLOSEVARS = 0x06000000;
			public const UInt32 POPCLOSED = 0x07000000;
			public const UInt32 CLOSURE = 0x08000000;
			public const UInt32 GETUPVAL = 0x09000000;

			public const UInt32 GETGLOBAL = 0x0A000000;
			public const UInt32 PUTGLOBAL = 0x0B000000;

			public const UInt32 GETSTACK = 0x0C000000;
			public const UInt32 PUTSTACK = 0x0D000000;

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

			public static UInt32 MakePOPVARGS(int numArgs, bool remVarArgObj)
			{
				UInt32 retVal = POPVARGS | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
				if (remVarArgObj)
				{
					retVal = retVal | ((1 << OP2_SHIFT) & OP2_MASK);
				}
				return retVal;
			}

			public static UInt32 MakeRET(int numArgs)
			{
				return RET | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeCLOSEVAR(int numVars)
			{
				return CLOSEVARS | (((UInt32)numVars << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakePOPCLOSED(int numVars)
			{
				return POPCLOSED | (((UInt32)numVars << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeCLOSURE(int index)
			{
				return CLOSURE | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeGETUPVAL(int index)
			{
				return GETUPVAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeGETGLOBAL(int index)
			{
				return GETGLOBAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakePUTGLOBAL(int index)
			{
				return PUTGLOBAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakeGETSTACK(int index)
			{
				return GETSTACK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}

			public static UInt32 MakePUTSTACK(int index)
			{
				return PUTSTACK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
			}
		}

		public class Function
		{
			public UInt32[] Bytecodes { get; private set; }
			public Object[] Constants { get; private set; }
			public String Id { get; private set; }

			public Dictionary<String, Function> ChildFunctions { get; private set; }

			public Function(UInt32[] bytecodes, Object[] constants, String id, List<Function> children)
			{
				Bytecodes = bytecodes;
				Constants = constants;
				Id = id;

				ChildFunctions = new Dictionary<string, Function>();
				foreach (Function child in children)
				{
					ChildFunctions[child.Id] = child;
				}
			}

			public void Print()
			{
				System.Console.WriteLine("Function " + Id);
				for (int i = 0; i < Bytecodes.Length; i++)
				{
					UInt32 instruction = Bytecodes[i];

					String name;
					UInt32 opCode = instruction & OpCodes.OPCODE_MASK;
					switch (opCode)
					{
						case OpCodes.LOADK:
							name = "LOADK";
							break;
						case OpCodes.ADD:
							name = "ADD";
							break;
						case OpCodes.RET:
							name = "RET";
							break;
						case OpCodes.CALL:
							name = "CALL";
							break;
						case OpCodes.POPVARGS:
							name = "POPVARGS";
							break;
						case OpCodes.CLOSEVARS:
							name = "CLOSEVARS";
							break;
						case OpCodes.POPCLOSED:
							name = "POPCLOSED";
							break;
						case OpCodes.CLOSURE:
							name = "CLOSURE";
							break;
						case OpCodes.GETUPVAL:
							name = "GETUPVAL";
							break;
						case OpCodes.GETGLOBAL:
							name = "GETGLOBAL";
							break;
						case OpCodes.PUTGLOBAL:
							name = "PUTGLOBAL";
							break;
						case OpCodes.GETSTACK:
							name = "GETSTACK";
							break;
						case OpCodes.PUTSTACK:
							name = "PUTSTACK";
							break;
						default:
							throw new VMException();
					}

					UInt32 op1 = (instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT;
					UInt32 op2 = (instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT;

					System.Console.WriteLine(String.Format("0x{0:x4} {1,10} {2:d} {3:d}", new Object[] {i, name, op1, op2}));
				}
				System.Console.WriteLine("");

				System.Console.WriteLine(String.Format("Constants: {0:d}", Constants.Length));
				for (int i = 0; i < Constants.Length; i++)
				{
					String constantStr = "null";
					if (Constants[i] != null)
					{
						constantStr = Constants[i].ToString();
					}
					System.Console.WriteLine(String.Format("{0:d}: {1,-5}", i, constantStr));
				}

				System.Console.WriteLine("");
				System.Console.WriteLine("");

				foreach (Function child in ChildFunctions.Values)
				{
					child.Print();
				}
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

			public void PushArg(Object o)
			{
				if (o is VarArgs)
				{
					VarArgs v = (VarArgs)o;
					Args.AddFirst(v.Args.First);
				}
				else
				{
					Args.AddFirst(o);
				}
			}

			public void PushLastArg(Object o)
			{
				if (o is VarArgs)
				{
					VarArgs v = (VarArgs)o;
					foreach (Object va in v.Args.Reverse()) {
						PushArg(va);
					}
				}
				else
				{
					PushArg(o);
				}
			}
		}

		public class Closure
		{
			public Function Func { get; private set; }
			public StackCell[] ClosedVars { get; private set; }

			public Closure(Function func, StackCell[] closedVars)
			{
				Func = func;
				ClosedVars = closedVars;
			}
		}

		public class InstructionPointer
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

		public class StackCell
		{
			public Object contents;
		}

		public class VMException : Exception { }

		private Dictionary<String, Function> FunctionMap { get; set; }

		private StackCell[] Stack { get; set; }
		private InstructionPointer CurrentIP { get; set; }
		private LinkedList<StackCell> ClosureStack { get; set; }

		private Dictionary<Object, Object> GlobalTable { get; set; }

		private const int stackSize = 512;

		private int baseIndex = 0;
		private int stackIndex = 0;

		public VirtualMachine()
		{
			FunctionMap = new Dictionary<String, Function>();
			Stack = new StackCell[stackSize];
			ClosureStack = new LinkedList<StackCell>();
			GlobalTable = new Dictionary<Object, Object>();
		}

		public void RegisterFunction(Function f)
		{
			FunctionMap.Add(f.Id, f);
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
					case OpCodes.CLOSEVARS:
						DoCLOSEVARS(currentInstruction);
						break;
					case OpCodes.POPCLOSED:
						DoPOPCLOSED(currentInstruction);
						break;
					case OpCodes.CLOSURE:
						DoCLOSURE(currentInstruction);
						break;
					case OpCodes.GETUPVAL:
						DoGETUPVAL(currentInstruction);
						break;
					case OpCodes.GETGLOBAL:
						DoGETGLOBAL(currentInstruction);
						break;
					case OpCodes.PUTGLOBAL:
						DoPUTGLOBAL(currentInstruction);
						break;
					case OpCodes.GETSTACK:
						DoGETSTACK(currentInstruction);
						break;
					case OpCodes.PUTSTACK:
						DoPUTSTACK(currentInstruction);
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

		public void PushStackUnboxed(StackCell s)
		{
			Stack[stackIndex] = s;
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
				if (i == 0)
				{
					args.PushLastArg(PopStack());
				}
				else
				{
					args.PushArg(PopStack());
				}
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
				args.Args.AddFirst(PopStack());
			}
			
			InstructionPointer newIP = null;

			if (o is Closure)
			{
				Closure closure = (Closure)o;
				Function f = closure.Func;
				newIP = new InstructionPointer(f, closure.ClosedVars, 0);
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

		private void DoCLOSEVARS(UInt32 instruction)
		{
			int numVars = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < numVars; i++)
			{
				ClosureStack.AddFirst(Stack[(stackIndex - 1) - i]);
			}
				
			CurrentIP.InstructionIndex++;
		}

		private void DoPOPCLOSED(UInt32 instruction)
		{
			int numVars = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < numVars; i++)
			{
				ClosureStack.RemoveFirst();
			}

			CurrentIP.InstructionIndex++;
		}

		public void DoCLOSURE(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			String functionId = (String)CurrentIP.CurrentFunction.Constants[index];
			StackCell[] closure = ClosureStack.ToArray();

			PushStack(new Closure(CurrentIP.CurrentFunction.ChildFunctions[functionId], closure));

			CurrentIP.InstructionIndex++;
		}

		public void DoGETUPVAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			PushStackUnboxed(CurrentIP.ClosedVars[index]);

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTGLOBAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object constant = CurrentIP.CurrentFunction.Constants[index];

			Object o = PopStack();

			GlobalTable[constant] = o;

			CurrentIP.InstructionIndex++;
		}

		public void DoGETGLOBAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object constant = CurrentIP.CurrentFunction.Constants[index];

			PushStack(GlobalTable[constant]);

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTSTACK(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			StackCell s = Stack[stackIndex - index];

			Object o = PopStack();

			s.contents = s;

			CurrentIP.InstructionIndex++;
		}

		public void DoGETSTACK(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object o = GetStackAtIndex(stackIndex - index);

			PushStack(o);

			CurrentIP.InstructionIndex++;
		}
		#endregion
	}
}
