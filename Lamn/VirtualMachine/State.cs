using System;
using System.Collections.Generic;
using System.Linq;

namespace Lamn.VirtualMachine
{
	public class State
	{
		public delegate VarArgs NativeFuncDelegate(VarArgs input, LamnEngine engine);
		public delegate void NativeCoreFuncDelegate(VarArgs input, LamnEngine engine);

		public LamnEngine Engine { get; private set; }

		public ThreadState CurrentThread { get { return ThreadStack.Peek(); } }
		public InstructionPointer CurrentIP { 
			get { 
				return CurrentThread.CurrentInstruction; 
			} 
			set { 
				CurrentThread.CurrentInstruction = value; 
			} 
		}

		public Stack<ThreadState> ThreadStack { get; private set; }

		private Dictionary<Object, Object> GlobalTable { get; set; }

		public System.IO.TextWriter OutStream { get; set; }

		private const int stackSize = 512;


		public State(LamnEngine engine)
		{
			Engine = engine;
			GlobalTable = new Dictionary<Object, Object>();
			ThreadStack = new Stack<ThreadState>();
			ThreadStack.Push(new ThreadState(stackSize));
			OutStream = new System.IO.StringWriter();
		}

		public void Run()
		{
			while (CurrentIP != null)
			{
				UInt32 currentInstruction = CurrentIP.CurrentInstruction;
				UInt32 opCode = currentInstruction & OpCodes.OPCODE_MASK;

				try
				{
					switch (opCode)
					{
						case OpCodes.LOADK:
							DoLOADK(currentInstruction);
							break;
						case OpCodes.ADD:
							DoADD(currentInstruction);
							break;
						case OpCodes.NEG:
							DoNEG(currentInstruction);
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
						case OpCodes.CONCAT:
							DoCONCAT(currentInstruction);
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

					if (CurrentThread.ExceptionHandlers.Count > 0 && CurrentThread.ExceptionHandlers.Peek().InstructionNumber == CurrentIP.InstructionIndex)
					{
						CurrentThread.ExceptionHandlers.Pop();
					}
				}
				catch (Exception e)
				{
					ThrowException(e);
				}
			}
		}

		public void Call()
		{
			DoCALL(0);
		}

		public void PutGlobal(String name, Object func) 
		{
			GlobalTable[name] = func;
		}

		#region Thread management functions
		public void ResumeThread(Thread t, VarArgs args)
		{
			if (t.State.StackPosition == 0) // Thread has terminated
			{
				VarArgs retArgs = new VarArgs();
				retArgs.PushArg(false);
				CurrentThread.PushStack(retArgs);
			}
			else // Resume thread
			{
				t.State.PushStack(args);
				ThreadStack.Push(t.State);
			}
			CurrentIP.InstructionIndex++;
		}

		public void YieldThread(VarArgs args)
		{
			ThreadStack.Pop();

			args.PushArg(true);
			CurrentThread.PushStack(args);

			CurrentIP.InstructionIndex++;
		}
		#endregion

		#region Exception handlers
		public void MarkExceptionHandler()
		{
			CurrentThread.PushExceptionHandler();
		}

		public void ThrowException(Exception e)
		{
			while (CurrentThread.ExceptionHandlers.Count == 0)
			{
				ThreadStack.Pop();
			}

			if (ThreadStack.Count == 0)
			{
				throw e;
			}

			CurrentThread.HandleException(e);
		}
		#endregion

		#region Execute instructions
		private void DoLOADK(UInt32 instruction)
		{
			UInt32 constantIndex = (instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT;
			CurrentThread.PushStack(CurrentIP.CurrentFunction.Constants[constantIndex]);
			CurrentIP.InstructionIndex++;
		}

		private void DoADD(UInt32 instruction)
		{
			Object op1 = CurrentThread.PopStack();
			Object op2 = CurrentThread.PopStack();

			if (op1 is Double && op2 is Double)
			{
				CurrentThread.PushStack((Double)op1 + (Double)op2);
				CurrentIP.InstructionIndex++;
			}
			else
			{
				Object o = GetBinHandler("__add", op1, op2);
				if (o == null)
				{
					throw new VMException();
				}

				CurrentThread.PushStack(o);
				CurrentThread.PushStack(op1);
				CurrentThread.PushStack(op2);
				DoCALL(OpCodes.MakeCALL(2, 1));
			}
		}

		private void DoNEG(UInt32 instruction)
		{
			Double op1 = (double)CurrentThread.PopStack();
			CurrentThread.PushStack(- op1);
			CurrentIP.InstructionIndex++;
		}

		private void DoRET(UInt32 instruction)
		{
			int oldBaseIndex = (int)CurrentThread.GetStackAtIndex(CurrentThread.BasePosition);

			VarArgs args = new VarArgs();
			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			for (int i = 0; i < numArgs; i++)
			{
				if (i == 0)
				{
					args.PushLastArg(CurrentThread.PopStack());
				}
				else
				{
					args.PushArg(CurrentThread.PopStack());
				}
			}

			while (CurrentThread.StackPosition > CurrentThread.BasePosition)
			{
				CurrentThread.PopStack();
			}

			CurrentThread.BasePosition = oldBaseIndex;

			Object retObj = CurrentThread.PopStack();

			if (retObj is ReturnPoint) // Returning inside a thread
			{
				ReturnPoint retPoint = (ReturnPoint)retObj;
				CurrentIP = retPoint.instructionPointer;
				if (CurrentIP != null)
				{
					CurrentIP.InstructionIndex++;
				}

				if (retPoint.popArgs > 0)
				{
					for (int i = 0; i < retPoint.popArgs; i++)
					{
						CurrentThread.PushStack(args.PopArg());
					}
				}
				else
				{
					CurrentThread.PushStack(args);
				}
			}
			else if (retObj is YieldPoint) // Returning across a thread boundary
			{
				YieldThread(args);
			}
			else
			{
				throw new VMException();
			}
		}

		public void DoCALL(UInt32 instruction)
		{
			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			int numPop = (int)((instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT);

			VarArgs args = new VarArgs();
			for (int i = 0; i < numArgs; i++)
			{
				if (i == 0) {
					args.PushLastArg(CurrentThread.PopStack());
				}
				else {
					args.PushArg(CurrentThread.PopStack());
				}
			}

			Object o = CurrentThread.PopStack();
			
			if (o is Closure)
			{
				Closure closure = (Closure)o;
				Function f = closure.Func;

				InstructionPointer newIP = null;
				newIP = new InstructionPointer(f, closure.ClosedVars, 0);

				ReturnPoint retPoint = new ReturnPoint(CurrentIP, numPop);

				CurrentThread.PushStack(retPoint);
				CurrentThread.PushStack(CurrentThread.BasePosition);
				CurrentThread.BasePosition = CurrentThread.StackPosition - 1;
				CurrentThread.PushStack(args);

				CurrentIP = newIP;
			}
			else if (o is NativeFuncDelegate)
			{
				NativeFuncDelegate nativeFunc = (NativeFuncDelegate)o;
				VarArgs returnArgs = nativeFunc(args, Engine);
				CurrentThread.PushStack(returnArgs);

				CurrentIP.InstructionIndex++;
			}
			else if (o is NativeCoreFuncDelegate)
			{
				NativeCoreFuncDelegate nativeFunc = (NativeCoreFuncDelegate)o;
				nativeFunc(args, Engine);
			}
			else
			{
				throw new VMException();
			}
		}

		private void DoPOPVARGS(UInt32 instruction)
		{
			VarArgs vargs = (VarArgs)CurrentThread.PeekStack();

			int numArgs = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);
			bool remVarArg = ((instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT) != 0;

			if (remVarArg)
			{
				CurrentThread.PopStack();
			}

			for (int i = 0; i < numArgs; i++)
			{
				CurrentThread.PushStack(vargs.PopArg());
			}

			CurrentIP.InstructionIndex++;
		}

		private void DoCLOSEVARS(UInt32 instruction)
		{
			int numVars = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < numVars; i++)
			{
				CurrentThread.ClosureStack.AddFirst(CurrentThread.GetUnboxedAtIndex((CurrentThread.StackPosition - 1) - i));
			}
				
			CurrentIP.InstructionIndex++;
		}

		private void DoPOPCLOSED(UInt32 instruction)
		{
			int numVars = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			for (int i = 0; i < numVars; i++)
			{
				CurrentThread.ClosureStack.RemoveFirst();
			}

			CurrentIP.InstructionIndex++;
		}

		public void DoCLOSURE(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			String functionId = (String)CurrentIP.CurrentFunction.Constants[index];
			StackCell[] closure = CurrentThread.ClosureStack.ToArray();

			CurrentThread.PushStack(new Closure(CurrentIP.CurrentFunction.ChildFunctions[functionId], closure));

			CurrentIP.InstructionIndex++;
		}

		public void DoGETUPVAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			CurrentThread.PushStack(CurrentIP.ClosedVars[index - 1].contents);

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTUPVAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object o = CurrentThread.PopStack();

			CurrentIP.ClosedVars[index - 1].contents = o;

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTGLOBAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object constant = CurrentIP.CurrentFunction.Constants[index];

			Object o = CurrentThread.PopStack();

			GlobalTable[constant] = o;

			CurrentIP.InstructionIndex++;
		}

		public void DoGETGLOBAL(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object constant = CurrentIP.CurrentFunction.Constants[index];

			Object obj = null;
			if (GlobalTable.ContainsKey(constant))
			{
				obj = GlobalTable[constant];
			}
			CurrentThread.PushStack(obj);

			CurrentIP.InstructionIndex++;
		}

		public void DoPUTSTACK(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			StackCell s = CurrentThread.GetUnboxedAtIndex(CurrentThread.StackPosition - index);

			Object o = CurrentThread.PopStack();

			s.contents = o;

			CurrentIP.InstructionIndex++;
		}

		public void DoGETSTACK(UInt32 instruction)
		{
			int index = (int)((instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT);

			Object o = CurrentThread.GetStackAtIndex(CurrentThread.StackPosition - index);

			CurrentThread.PushStack(o);

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
				o = CurrentThread.PeekStack();
			}
			else
			{
				o = CurrentThread.PopStack();
			}

			if (IsValueTrue(o))
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
				CurrentThread.PopStack();
			}

			CurrentIP.InstructionIndex++;
		}

		public void DoEQ(UInt32 instruction)
		{
			Object op1 = (Object)CurrentThread.PopStack();
			Object op2 = (Object)CurrentThread.PopStack();

			if (op1 == null)
			{
				CurrentThread.PushStack(op2 == null);
			}
			else
			{
				CurrentThread.PushStack(op1.Equals(op2));
			}

			CurrentIP.InstructionIndex++;
		}

		public void DoNOT(UInt32 instruction)
		{
			Object op1 = CurrentThread.PopStack();

			CurrentThread.PushStack(!IsValueTrue(op1));

			CurrentIP.InstructionIndex++;
		}

		private void DoAND(UInt32 instruction)
		{
			Object op1 = CurrentThread.PopStack();
			Object op2 = CurrentThread.PopStack();
			if (IsValueTrue(op1) && IsValueTrue(op2))
			{
				CurrentThread.PushStack(op1);
			}
			else
			{
				CurrentThread.PushStack(false);
			}
			CurrentIP.InstructionIndex++;
		}

		private void DoOR(UInt32 instruction)
		{
			Object op1 = CurrentThread.PopStack();
			Object op2 = CurrentThread.PopStack();
			if (IsValueTrue(op1) || IsValueTrue(op2))
			{
				CurrentThread.PushStack(op2);
			}
			else
			{
				CurrentThread.PushStack(false);
			}
			CurrentIP.InstructionIndex++;
		}

		private void DoCONCAT(UInt32 instruction)
		{
			Object op1 = CurrentThread.PopStack();
			Object op2 = CurrentThread.PopStack();

			if (op1 is double)
			{
				op1 = op1.ToString();
			}

			if (op2 is double)
			{
				op2 = op2.ToString();
			}

			if (op1 is String && op2 is String)
			{
				CurrentThread.PushStack((String)op2 + (String)op1);
			}
			else
			{
				throw new NotImplementedException();
			}

			CurrentIP.InstructionIndex++;
		}

		private void DoLESSEQ(UInt32 instruction)
		{
			Double op1 = (Double)CurrentThread.PopStack();
			Double op2 = (Double)CurrentThread.PopStack();
			CurrentThread.PushStack(op2 <= op1);
			CurrentIP.InstructionIndex++;
		}

		private void DoLESS(UInt32 instruction)
		{
			Double op1 = (Double)CurrentThread.PopStack();
			Double op2 = (Double)CurrentThread.PopStack();
			CurrentThread.PushStack(op2 < op1);
			CurrentIP.InstructionIndex++;
		}

		private void DoNEWTABLE(UInt32 instruction)
		{
			CurrentThread.PushStack(new Table());
			CurrentIP.InstructionIndex++;
		}

		private void DoPUTTABLE(UInt32 instruction)
		{
			Object value = CurrentThread.PopStack();
			Object key = CurrentThread.PopStack();
			Table table = (Table)CurrentThread.PopStack();

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
			Object key = CurrentThread.PopStack();
			Table table = (Table)CurrentThread.PopStack();
			Object value = table.MetaGet(key);

			CurrentThread.PushStack(value);

			CurrentIP.InstructionIndex++;
		}
		#endregion

		public static bool IsValueTrue(Object o)
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

		private Object GetBinHandler(String key, Object op1, Object op2)
		{
			if (op1 is Table && ((Table)op1).MetaTable != null && ((Table)op1).MetaTable.MetaGet(key) != null)
			{
				return ((Table)op1).MetaTable.MetaGet(key);
			}

			if (op2 is Table && ((Table)op2).MetaTable != null && ((Table)op2).MetaTable.MetaGet(key) != null)
			{
				return ((Table)op2).MetaTable.MetaGet(key);
			}

			return null;
		}
	}
}
