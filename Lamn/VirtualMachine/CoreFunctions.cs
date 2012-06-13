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
			
			s.PutGlobal("require", new State.NativeFuncDelegate(Require));

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
		
		static VarArgs Require(VarArgs input, LamnEngine s)
		{
			VarArgs retArgs = new VarArgs();
			String arg = (String)input.PopArg();
			if (arg.Equals("debug")) 
			{
				retArgs.PushArg(GetDebugTable ());
			}
			return retArgs;
		}
		#endregion

		#region String functions
		private static Table GetStringTable()
		{
			Table stringTable = new Table();
			stringTable.RawPut("len", new State.NativeFuncDelegate(StrLen));
			stringTable.RawPut("gsub", new State.NativeFuncDelegate(StrGSub));
			stringTable.RawPut("format", new State.NativeFuncDelegate(StrFmt));

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
		
		private static VarArgs StrFmt(VarArgs args, LamnEngine s) 
		{
			String currentString = (String)args.PopArg();
			while(true) 
			{
				int index = currentString.IndexOf("%s");
				if (index < 0) {
					break;
				}
				
				String replace = (String)args.PopArg();
				currentString = currentString.Substring(0,index) + replace + currentString.Substring(index + 2);
			}
			VarArgs retArgs = new VarArgs();
			retArgs.PushArg(currentString);
			return retArgs;
		}
		#endregion
		
		#region debug functions
		private static Table GetDebugTable()
		{
			Table debugTable = new Table();
			debugTable.RawPut ("getinfo", new State.NativeFuncDelegate(DebugGetInfo));
			return debugTable;
		}
		
		private static VarArgs DebugGetInfo(VarArgs args, LamnEngine s)
		{
			VarArgs retArgs = new VarArgs();
			List<ReturnPoint> stack = s.LamnState.GetStackTrace();
			int func = (int)((double)args.PopArg() - 1);
			String spec = (String)args.PopArg();
			if (spec.Equals("n") && func < stack.Count) {
				Table t = new Table();
				ReturnPoint retPoint = stack[func];
				t.RawPut("name", retPoint.instructionPointer.CurrentFunction.Name);
				retArgs.PushArg(t);
			}
			return retArgs;	
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
			Thread t = new Thread((Closure)args.PopArg(), 512);
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
			mathTable.RawPut("fmod", new State.NativeFuncDelegate(FMod));
			mathTable.RawPut("floor", new State.NativeFuncDelegate(Floor));

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
		
		private static VarArgs FMod(VarArgs args, LamnEngine s)
		{
			VarArgs args2 = ToNumber(args, s);
			Double d1 = (Double)args2.PopArg();
			args2 = ToNumber(args, s);
			Double d2 = (Double)args2.PopArg();
			
			int div = (int)(d1 / d2);
			Double rem = d1 - (d2 * div);
			
			VarArgs retArgs = new VarArgs();
			retArgs.PushArg(rem);
			return retArgs;
		}
		
		private static VarArgs Floor(VarArgs args, LamnEngine s)
		{
			args = ToNumber(args, s);
			Double d = (Double)args.PopArg();
			
			VarArgs retArgs = new VarArgs();
			retArgs.PushArg(Math.Floor(d));
			return retArgs;
		}
		#endregion
	}
}
