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

			s.PutGlobal("loadstring", new State.NativeFuncDelegate(LoadString));

			s.PutGlobal("coroutine", GetCoroutineTable());
		}

		#region Basic core functions
		public static VarArgs GetMetaTable(VarArgs args, State s)
		{
			Object o = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (o != null && o is Table)
			{
				returnArgs.PushArg(((Table)o).MetaTable);
			}

			return returnArgs;
		}

		public static VarArgs SetMetaTable(VarArgs args, State s)
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

		public static VarArgs ToNumber(VarArgs args, State s)
		{
			Object arg = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (arg is Double)
			{
				returnArgs.PushArg(arg);
			}

			return returnArgs;
		}

		static VarArgs Error(VarArgs args, State s)
		{
			throw new VMException();
		}

		static VarArgs Assert(VarArgs args, State s)
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

		static void PCall(VarArgs args, State s)
		{
			Object func = args.PopArg();

			s.MarkExceptionHandler();

			s.CurrentThread.PushStack(func);
			s.CurrentThread.PushStack(args);
			s.DoCALL(OpCodes.MakeCALL(1));
		}

		static VarArgs Print(VarArgs input, State s)
		{
			foreach (Object o in input.Args)
			{
				if (o == null)
				{
					s.OutStream.Write("nil");
				}
				else if (o is Double)
				{
					s.OutStream.Write((Double)o);
				}
				else if (o is String)
				{
					s.OutStream.Write((String)o);
				}
				else if (o is Boolean)
				{
					s.OutStream.Write((Boolean)o);
				}
				else if (o is Table)
				{
					s.OutStream.Write(((Table)o).ToString());
				}
				else
				{
					s.OutStream.Write("[Unknown]");
				}
				s.OutStream.Write("\t");
			}

			s.OutStream.Write("\n");

			return new VarArgs();
		}

		static VarArgs LoadString(VarArgs input, State s)
		{
			return new VarArgs();
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

		private static VarArgs CoroutineCreate(VarArgs args, State s)
		{
			Thread t = new Thread((Closure)args.PopArg());
			t.State.PushStack(new YieldPoint());
			t.State.PushStack(0);
			t.State.BasePosition = 1;

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg(t);
			return returnArgs;
		}

		private static void CoroutineResume(VarArgs args, State s)
		{
			Thread t = (Thread)args.PopArg();
			s.ResumeThread(t, args);
		}

		private static void CoroutineYield(VarArgs args, State s)
		{
			s.YieldThread(args);
		}
		#endregion
	}
}
