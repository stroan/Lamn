using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lamn;

namespace REPL
{
	class Program
	{
		static bool finished = false;

		static void Main(string[] args)
		{
			LamnEngine l = new LamnEngine();
			l.SetGlobal("quit", new Lamn.VirtualMachine.State.NativeFuncDelegate(Quit));

			l.OutputStream = System.Console.Out;

			while (!finished)
			{
				System.Console.Write("> ");
				String line = System.Console.ReadLine();

				try
				{
					l.Run(line);
				}
				catch (Exception e)
				{
					System.Console.WriteLine(e.ToString());
				}
			}
		}

		private static Lamn.VirtualMachine.VarArgs Quit(Lamn.VirtualMachine.VarArgs args, Lamn.VirtualMachine.State s)
		{
			finished = true;
			return new Lamn.VirtualMachine.VarArgs();
		}
	}
}
