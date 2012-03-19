using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	public class CompilerState
	{
		public List<UInt32> bytecodes = new List<UInt32>();
		public List<Object> constants = new List<Object>();
		public List<VirtualMachine.Function> childFunctions = new List<VirtualMachine.Function>();

		public int stackPosition = 0;
		public Dictionary<String, int> stackVars = new Dictionary<String, int>();

		public int initialClosureStackPosition = 0;
		public int closureStackPosition = 0;
		public Dictionary<String, int> closedVars = new Dictionary<String, int>();
		public Dictionary<String, int> newClosedVars = new Dictionary<String, int>();

		public Dictionary<String, int> labels = new Dictionary<String, int>();
		public List<KeyValuePair<String, int>> jumps = new List<KeyValuePair<String, int>>();

		public String currentBreakLabel;
		public SavedState currentBreakState;

		public void ResolveJumps()
		{
			foreach (KeyValuePair<String, int> jump in jumps)
			{
				bytecodes[jump.Value] |= (VirtualMachine.OpCodes.OP1_MASK & ((UInt32)(labels[jump.Key]) << VirtualMachine.OpCodes.OP1_SHIFT));
			}

			jumps.Clear();
		}

		public String getNewLabel()
		{
			return Guid.NewGuid().ToString();
		}

		public int AddConstant(Object o)
		{
			if (!constants.Contains(o))
			{
				constants.Add(o);
			}

			return constants.IndexOf(o);
		}

		public SavedState SaveState()
		{
			int initialStackPosition = stackPosition;
			int blockInitialClosureStackPosition = closureStackPosition;

			Dictionary<String, int> oldClosedVars = newClosedVars.ToDictionary(entry => entry.Key,
															 entry => entry.Value);
			Dictionary<String, int> oldStackVars = stackVars.ToDictionary(entry => entry.Key,
														entry => entry.Value);

			return new SavedState
			{
				initialStackPosition = initialStackPosition,
				blockInitialClosureStackPosition = blockInitialClosureStackPosition,
				oldClosedVars = oldClosedVars,
				oldStackVars = oldStackVars
			};
		}

		public void RestoreState(SavedState s)
		{
			Cleanup(s);

			if (closureStackPosition > s.blockInitialClosureStackPosition)
			{
				closureStackPosition = s.blockInitialClosureStackPosition;
				newClosedVars = s.oldClosedVars;
			}

			if (stackPosition != s.initialStackPosition)
			{
				stackPosition = s.initialStackPosition;
				stackVars = s.oldStackVars;
			}
		}

		public void Cleanup(SavedState s)
		{
			if (closureStackPosition > s.blockInitialClosureStackPosition)
			{
				bytecodes.Add(VirtualMachine.OpCodes.MakePOPCLOSED(closureStackPosition - s.blockInitialClosureStackPosition));
			}

			if (stackPosition != s.initialStackPosition)
			{
				bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(stackPosition - s.initialStackPosition));
			}
		}

		public class SavedState
		{
			public int initialStackPosition;
			public int blockInitialClosureStackPosition;

			public Dictionary<String, int> oldClosedVars;
			public Dictionary<String, int> oldStackVars;
		}
	}
}
