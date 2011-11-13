using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lamn;
using Lamn.VirtualMachine;

namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			LamnEngine l = new LamnEngine();
			l.SetGlobal("print", new State.NativeFuncDelegate(Print));

			l.Run(System.IO.File.ReadAllText("../../../TestFiles/Test7.lua"));
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

		static VarArgs tonumber(VarArgs input)
		{
			return input;
		}
	}
}
