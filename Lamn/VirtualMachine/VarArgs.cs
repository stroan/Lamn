using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	class VarArgs
	{
		public LinkedList<Object> Args { get; private set; }

		public VarArgs()
		{
			Args = new LinkedList<Object>();
		}

		public Object PopArg()
		{
			if (Args.Count == 0) { return null; }

			Object first = Args.First.Value;
			Args.RemoveFirst();
			return first;
		}

		public void PushArg(Object o)
		{
			if (o is VarArgs)
			{
				VarArgs v = (VarArgs)o;
				Args.AddFirst(v.Args.First);
			}
			else
			{
				Args.AddFirst(o);
			}
		}

		public void PushLastArg(Object o)
		{
			if (o is VarArgs)
			{
				VarArgs v = (VarArgs)o;
				foreach (Object va in v.Args.Reverse())
				{
					PushArg(va);
				}
			}
			else
			{
				PushArg(o);
			}
		}
	}
}
