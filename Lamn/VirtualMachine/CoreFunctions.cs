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
	}
}
