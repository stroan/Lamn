using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class FunctionCompiler
	{
		public CompilerState State { get; set; }
		public String Id { get; set; }

		public FunctionCompiler(CompilerState oldState, AST.Body body, bool selfFunction)
		{
			State = new CompilerState();

			State.initialClosureStackPosition = oldState.closureStackPosition;
			State.closureStackPosition = oldState.closureStackPosition;
			State.closedVars = oldState.closedVars;
			foreach (KeyValuePair<String, int> i in oldState.newClosedVars)
			{
				State.closedVars[i.Key] = i.Value;
			}

			State.stackPosition = 1;

			List<String> paramList = body.ParamList.NamedParams;
			if (selfFunction)
			{
				List<String> pList = new List<String>();
				pList.Add("self");
				pList.AddRange(paramList);
				paramList = pList;
			}

			if (paramList.Count > 0)
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(paramList.Count));
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(paramList.Count));
				State.stackPosition += paramList.Count;
			}

			int stackIndex = 1;
			foreach (String param in paramList)
			{
				State.stackVars[param] = stackIndex;
				stackIndex++;

				State.newClosedVars[param] = State.closureStackPosition;
				State.closureStackPosition++;
			}

			if (body.ParamList.HasVarArgs)
			{
				State.stackVars["..."] = 0;
			}

			new ChunkCompiler(body.Chunk, State, null, true);
			State.ResolveJumps();

			Id = Guid.NewGuid().ToString();
		}
	}
}
