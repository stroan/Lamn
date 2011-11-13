﻿using System;
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
		}

		public static VarArgs GetMetaTable(VarArgs args)
		{
			Object o = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (o != null && o is Table)
			{
				returnArgs.PushArg(((Table)o).MetaTable);
			}

			return returnArgs;
		}

		public static VarArgs SetMetaTable(VarArgs args)
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

		public static VarArgs ToNumber(VarArgs args)
		{
			Object arg = args.PopArg();
			VarArgs returnArgs = new VarArgs();

			if (arg is Double)
			{
				returnArgs.PushArg(arg);
			}

			return args;
		}

		static VarArgs Print(VarArgs input)
		{
			foreach (Object o in input.Args)
			{
				if (o == null)
				{
					System.Console.Write("nil");
				}
				else if (o is Double)
				{
					System.Console.Write((Double)o);
				}
				else if (o is String)
				{
					System.Console.Write((String)o);
				}
				else if (o is Boolean)
				{
					System.Console.Write((Boolean)o);
				}
				else if (o is Table)
				{
					System.Console.Write(((Table)o).ToString());
				}
				else
				{
					System.Console.Write("[Unknown]");
				}
				System.Console.Write("\t");
			}

			System.Console.Write("\n");

			return new VarArgs();
		}
	}
}
