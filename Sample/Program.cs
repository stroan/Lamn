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

			l.Run(System.IO.File.ReadAllText("../../../TestFiles/Test8.lua"));
		}
	}
}
