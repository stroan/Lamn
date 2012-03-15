using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	class CoreFunctions
	{
		public static void RegisterCoreFunctions(State s)
		{
			s.PutGlobal("getmetatable", new State.NativeFuncDelegate(GetMetaTable));
			s.PutGlobal("setmetatable", new State.NativeFuncDelegate(SetMetaTable));
			
			s.PutGlobal("tonumber", new State.NativeFuncDelegate(ToNumber));
			
			s.PutGlobal("print", new State.NativeFuncDelegate(Print));
			
			s.PutGlobal("error", new State.NativeFuncDelegate(Error));
			s.PutGlobal("assert", new State.NativeFuncDelegate(Assert));
			s.PutGlobal("pcall", new State.NativeCoreFuncDelegate(PCall));

			s.PutGlobal("load", new State.NativeFuncDelegate(LoadString));
			s.PutGlobal("dostring", new State.NativeCoreFuncDelegate(DoString));

			s.PutGlobal("coroutine", GetCoroutineTable());
			s.PutGlobal("string", GetStringTable());
		}

		#region Basic core functions
		public static VarArgs GetMetaTable(VarArgs args, LamnEngine s)
		{
			Object o = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (o != null && o is Table)
			{
				returnArgs.PushArg(((Table)o).MetaTable);
			}

			return returnArgs;
		}

		public static VarArgs SetMetaTable(VarArgs args, LamnEngine s)
		{
			Object obj = args.PopArg();
			Object metatable = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (obj != null && obj is Table)
			{
				((Table)obj).MetaTable = (Table)metatable;
			}

			return returnArgs;
		}

		public static VarArgs ToNumber(VarArgs args, LamnEngine s)
		{
			Object arg = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (arg is Double)
			{
				returnArgs.PushArg(arg);
			}
			else if (arg is String)
			{
				returnArgs.PushArg(Double.Parse((String)arg));
			}

			return returnArgs;
		}

		static VarArgs Error(VarArgs args, LamnEngine s)
		{
			throw new VMException();
		}

		static VarArgs Assert(VarArgs args, LamnEngine s)
		{
			Object o = args.PopArg();
			if (!State.IsValueTrue(o))
			{
				throw new VMException();
			}

			VarArgs newArgs = new VarArgs();
			newArgs.PushArg(o);
			return newArgs;
		}

		static void PCall(VarArgs args, LamnEngine s)
		{
			Object func = args.PopArg();

			s.LamnState.MarkExceptionHandler();

			s.LamnState.CurrentThread.PushStack(func);
			s.LamnState.CurrentThread.PushStack(args);
			s.LamnState.DoCALL(OpCodes.MakeCALL(1));
		}

		static VarArgs Print(VarArgs input, LamnEngine s)
		{
			foreach (Object o in input.Args)
			{
				if (o == null)
				{
					s.LamnState.OutStream.Write("nil");
				}
				else if (o is Double)
				{
					s.LamnState.OutStream.Write((Double)o);
				}
				else if (o is String)
				{
					s.LamnState.OutStream.Write((String)o);
				}
				else if (o is Boolean)
				{
					s.LamnState.OutStream.Write((Boolean)o);
				}
				else if (o is Table)
				{
					s.LamnState.OutStream.Write(((Table)o).ToString());
				}
				else
				{
					s.LamnState.OutStream.Write("[Unknown]");
				}
				s.LamnState.OutStream.Write("\t");
			}

			s.LamnState.OutStream.Write("\n");

			return new VarArgs();
		}

		static VarArgs LoadString(VarArgs input, LamnEngine s)
		{
			VarArgs retArgs = new VarArgs();
			retArgs.PushArg(s.CompileString((String)input.PopArg()));
			return retArgs;
		}

		static void DoString(VarArgs input, LamnEngine s)
		{
			VarArgs retArgs = new VarArgs();
			Closure closure = s.CompileString((String)input.PopArg());
			s.LamnState.CurrentThread.PushStack(closure);
			s.LamnState.DoCALL(OpCodes.MakeCALL(0));
		}
		#endregion

		#region String functions
		private static Table GetStringTable()
		{
			Table stringTable = new Table();
			stringTable.RawPut("len", new State.NativeFuncDelegate(StrLen));

			return stringTable;
		}

		private static VarArgs StrLen(VarArgs args, LamnEngine s)
		{
			String str = (String)args.PopArg();

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg((double)str.Length);
			return returnArgs;
		}
		#endregion

		#region coroutine functions
		private static Table GetCoroutineTable()
		{
			Table coroutineTable = new Table();
			coroutineTable.RawPut("create", new State.NativeFuncDelegate(CoroutineCreate));
			coroutineTable.RawPut("resume", new State.NativeCoreFuncDelegate(CoroutineResume));
			coroutineTable.RawPut("yield", new State.NativeCoreFuncDelegate(CoroutineYield));

			return coroutineTable;
		}

		private static VarArgs CoroutineCreate(VarArgs args, LamnEngine s)
		{
			Thread t = new Thread((Closure)args.PopArg());
			t.State.PushStack(new YieldPoint());
			t.State.PushStack(0);
			t.State.BasePosition = 1;

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg(t);
			return returnArgs;
		}

		private static void CoroutineResume(VarArgs args, LamnEngine s)
		{
			Thread t = (Thread)args.PopArg();
			s.LamnState.ResumeThread(t, args);
		}

		private static void CoroutineYield(VarArgs args, LamnEngine s)
		{
			s.LamnState.YieldThread(args);
		}
		#endregion
	}
}
