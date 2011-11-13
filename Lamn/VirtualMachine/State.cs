using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class State
	{
		public delegate VarArgs NativeFuncDelegate(VarArgs input);

		private Dictionary<String, Function> FunctionMap { get; set; }

		private StackCell[] Stack { get; set; }
		private InstructionPointer CurrentIP { get; set; }
		private LinkedList<StackCell> ClosureStack { get; set; }

		private Dictionary<Object, Object> GlobalTable { get; set; }

		private const int stackSize = 512;

		private int baseIndex = 0;
		private int stackIndex = 0;

		public State()
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
					case OpCodes.PUTUPVAL:
						DoPUTUPVAL(currentInstruction);
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
					case OpCodes.JMP:
						DoJMP(currentInstruction);
						break;
					case OpCodes.JMPTRUE:
						DoJMPTRUE(currentInstruction);
						break;
					case OpCodes.POPSTACK:
						DoPOPSTACK(currentInstruction);
						break;
					case OpCodes.EQ:
						DoEQ(currentInstruction);
						break;
					case OpCodes.NOT:
						DoNOT(currentInstruction);
						break;
					case OpCodes.AND:
						DoAND(currentInstruction);
						break;
					case OpCodes.OR:
						DoOR(currentInstruction);
						break;
					case OpCodes.LESSEQ:
						DoLESSEQ(currentInstruction);
						break;
					case OpCodes.LESS:
						DoLESS(currentInstruction);
						break;
					case OpCodes.NEWTABLE:
						DoNEWTABLE(currentInstruction);
						break;
					case OpCodes.PUTTABLE:
						DoPUTTABLE(currentInstruction);
						break;
					case OpCodes.GETTABLE:
						DoGETTABLE(currentInstruction);
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

		public void PutGlobal(String name, NativeFuncDelegate func) 
		{
			GlobalTable[name] = func;
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

			ReturnPoint retPoint = (ReturnPoint)PopStack();
			CurrentIP = retPoint.instructionPointer;
			if (CurrentIP != null)
			{
				CurrentIP.InstructionIndex++;
			}

			if (retPoint.popArgs > 0)
			{
				for (int i = 0; i < retPoint.popArgs; i++)
				{
					PushStack(args.PopArg());
				}
			}
			else
			{
				PushStack(args);
			}
		}

		private void DoCALL(UInt32 instruction)
		{
			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			int numPop = (int)((instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT);

			VarArgs args = new VarArgs();
			for (int i = 0; i < numArgs; i++)
			{
				if (i == 0) {
					args.PushLastArg(PopStack());
				}
				else {
					args.PushArg(PopStack());
				}
			}

			Object o = PopStack();
			
			if (o is Closure)
			{
				Closure closure = (Closure)o;
				Function f = closure.Func;

				InstructionPointer newIP = null;
				newIP = new InstructionPointer(f, closure.ClosedVars, 0);

				ReturnPoint retPoint = new ReturnPoint(CurrentIP, numPop);

				PushStack(retPoint);
				PushStack(baseIndex);
				baseIndex = stackIndex - 1;
				PushStack(args);

				CurrentIP = newIP;
			}
			else if (o is NativeFuncDelegate)
			{
				NativeFuncDelegate nativeFunc = (NativeFuncDelegate)o;
				VarArgs returnArgs = nativeFunc(args);
				PushStack(returnArgs);

				CurrentIP.InstructionIndex++;
			}
			else
			{
				throw new VMException();
			}
		}

		private void DoPOPVARGS(UInt32 instruction)
		{
			VarArgs vargs = (VarArgs)PeekStack();

			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			bool remVarArg = ((instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT) != 0;

			if (remVarArg)
			{
				PopStack();
			}

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

			PushStack(CurrentIP.ClosedVars[index - 1].contents);

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTUPVAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object o = PopStack();

			CurrentIP.ClosedVars[index - 1].contents = o;

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

			s.contents = o;

			CurrentIP.InstructionIndex++;
		}

		public void DoGETSTACK(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object o = GetStackAtIndex(stackIndex - index);

			PushStack(o);

			CurrentIP.InstructionIndex++;
		}

		public void DoJMP(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			CurrentIP.InstructionIndex = index;
		}
		public void DoJMPTRUE(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			bool preserve = (int)((instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT) == 1;

			Object o;
			if (preserve)
			{
				o = PeekStack();
			}
			else
			{
				o = PopStack();
			}

			if (isValueTrue(o))
			{
				CurrentIP.InstructionIndex = index;
			}
			else
			{
				CurrentIP.InstructionIndex++;
			}
		}

		public void DoPOPSTACK(UInt32 instruction)
		{
			int count = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < count; i++)
			{
				PopStack();
			}

			CurrentIP.InstructionIndex++;
		}

		public void DoEQ(UInt32 instruction)
		{
			Object op1 = (Object)PopStack();
			Object op2 = (Object)PopStack();
			PushStack(op1.Equals(op2));
			CurrentIP.InstructionIndex++;
		}

		public void DoNOT(UInt32 instruction)
		{
			Object op1 = PopStack();

			PushStack(!isValueTrue(op1));

			CurrentIP.InstructionIndex++;
		}

		private void DoAND(UInt32 instruction)
		{
			Object op1 = PopStack();
			Object op2 = PopStack();
			if (isValueTrue(op1) && isValueTrue(op2))
			{
				PushStack(op1);
			}
			else
			{
				PushStack(false);
			}
			CurrentIP.InstructionIndex++;
		}

		private void DoOR(UInt32 instruction)
		{
			Object op1 = PopStack();
			Object op2 = PopStack();
			if (isValueTrue(op1) || isValueTrue(op2))
			{
				PushStack(op2);
			}
			else
			{
				PushStack(false);
			}
			CurrentIP.InstructionIndex++;
		}

		private void DoLESSEQ(UInt32 instruction)
		{
			Double op1 = (Double)PopStack();
			Double op2 = (Double)PopStack();
			PushStack(op2 <= op1);
			CurrentIP.InstructionIndex++;
		}

		private void DoLESS(UInt32 instruction)
		{
			Double op1 = (Double)PopStack();
			Double op2 = (Double)PopStack();
			PushStack(op2 < op1);
			CurrentIP.InstructionIndex++;
		}

		private void DoNEWTABLE(UInt32 instruction)
		{
			PushStack(new Table());
			CurrentIP.InstructionIndex++;
		}

		private void DoPUTTABLE(UInt32 instruction)
		{
			Object value = PopStack();
			Object key = PopStack();
			Table table = (Table)PopStack();

			if (key is String)
			{
				table.RawPut((String)key, value);
			}
			else if (key is Double && value is VarArgs)
			{
				VarArgs vargs = (VarArgs)value;
				Double index = (Double)key;
				foreach (Object o in vargs.Args)
				{
					table.RawPut(index++, o);
				}
			}
			else if (key is Double)
			{
				table.RawPut((Double)key, value);
			}
			else
			{
				throw new NotImplementedException();
			}

			CurrentIP.InstructionIndex++;
		}

		private void DoGETTABLE(UInt32 instruction)
		{
			Object key = PopStack();
			Table table = (Table)PopStack();

			Object value = null;
			Table metatable = null;
			do
			{
				value = table.RawGet(key);
				metatable = table.MetaTable;

				if (value == null)
				{
					table = metatable;
				}
			} while (value == null && metatable != null);

			PushStack(value);

			CurrentIP.InstructionIndex++;
		}
		#endregion

		private bool isValueTrue(Object o)
		{
			if (o == null)
			{
				return false;
			}
			else if (o is bool)
			{
				return (bool)o;
			}
			else
			{
				return true;
			}
		}
	}
}
