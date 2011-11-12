using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	class Closure
	{
		public Function Func { get; private set; }
		public StackCell[] ClosedVars { get; private set; }

		public Closure(Function func, StackCell[] closedVars)
		{
			Func = func;
			ClosedVars = closedVars;
		}
	}
}
