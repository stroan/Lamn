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

			l.OutputStream = System.Console.Out;

			l.Run(System.IO.File.ReadAllText("../../../TestFiles/custom_tests/Test8.lua"));
		}
	}
}
