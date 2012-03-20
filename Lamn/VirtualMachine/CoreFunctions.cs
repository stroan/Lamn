using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lamn.VirtualMachine
{
	class CoreFunctions
	{
		public static void RegisterCoreFunctions(State s)
		{
			s.PutGlobal("getmetatable", new State.NativeFuncDelegate(GetMetaTable));
			s.PutGlobal("setmetatable", new State.NativeFuncDelegate(SetMetaTable));

			s.PutGlobal("type", new State.NativeFuncDelegate(TypeString));
			s.PutGlobal("tonumber", new State.NativeFuncDelegate(ToNumber));
			
			s.PutGlobal("print", new State.NativeFuncDelegate(Print));
			
			s.PutGlobal("error", new State.NativeFuncDelegate(Error));
			s.PutGlobal("assert", new State.NativeFuncDelegate(Assert));
			s.PutGlobal("pcall", new State.NativeCoreFuncDelegate(PCall));

			s.PutGlobal("load", new State.NativeFuncDelegate(LoadString));

			s.PutGlobal("coroutine", GetCoroutineTable());
			s.PutGlobal("string", GetStringTable());
			s.PutGlobal("math", GetMathTable());
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

		public static VarArgs TypeString(VarArgs args, LamnEngine s)
		{
			Object arg = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (arg is Double)
			{
				returnArgs.PushArg("number");
			}
			else if (arg is String)
			{
				returnArgs.PushArg("string");
			}
			else if (arg is Table)
			{
				returnArgs.PushArg("table");
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
		#endregion

		#region String functions
		private static Table GetStringTable()
		{
			Table stringTable = new Table();
			stringTable.RawPut("len", new State.NativeFuncDelegate(StrLen));
			stringTable.RawPut("gsub", new State.NativeFuncDelegate(StrGSub));

			return stringTable;
		}

		private static class RegexCompiler
		{
			public static Regex compile(String luaRegex)
			{
				return new Regex(luaRegex.Replace("%s", "\\s"));
			}
		}

		private static VarArgs StrLen(VarArgs args, LamnEngine s)
		{
			String str = (String)args.PopArg();

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg((double)str.Length);
			return returnArgs;
		}

		private static VarArgs StrGSub(VarArgs args, LamnEngine s)
		{
			String str = (String)args.PopArg();
			String pattern = (String)args.PopArg();
			String repl = (String)args.PopArg();
			Object n = args.PopArg();

			int count = -1;
			if (n != null)
			{
				count = (int)(Double)n;
			}

			Regex patternRegex = RegexCompiler.compile(pattern);
			String retStr = patternRegex.Replace(str, repl, count);

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg(retStr);
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

		#region Math functions
		private static Table GetMathTable()
		{
			Table mathTable = new Table();
			mathTable.RawPut("sin", new State.NativeFuncDelegate(Sin));

			return mathTable;
		}

		private static VarArgs Sin(VarArgs args, LamnEngine s)
		{
			args = ToNumber(args, s);
			Double d = (Double)args.PopArg();

			VarArgs returnArgs = new VarArgs();
			returnArgs.PushArg(Math.Sin(d));
			return returnArgs;
		}
		#endregion
	}
}
