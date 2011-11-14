using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class AssignerExpressionCompiler : AST.ExpressionVisitor
	{
		public CompilerState State { get; private set; }
		public int NumResults { get; private set; }

		int leftPosition;
		int rightPosition;

		public AssignerExpressionCompiler(CompilerState state, int left, int right)
		{
			State = state;
			NumResults = 1;
			leftPosition = left;
			rightPosition = right;
		}

		#region ExpressionVisitor Members

		public void Visit(AST.NameExpression expression)
		{
			if (State.stackVars.ContainsKey(expression.Value))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - State.stackVars[expression.Value])); State.stackPosition--;
			}
			else if (State.closedVars.ContainsKey(expression.Value))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTUPVAL(State.initialClosureStackPosition - State.closedVars[expression.Value])); State.stackPosition--;
			}
			else
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(State.AddConstant(expression.Value))); State.stackPosition--;
			}
		}

		public void Visit(AST.NumberExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.StringExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.BoolExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.NilExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.VarArgsExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.ParenExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.UnOpExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.BinOpExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.FunctionExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.LookupExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - leftPosition)); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Name))); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
		}

		public void Visit(AST.SelfLookupExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.IndexExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - leftPosition)); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - (leftPosition + 1))); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
		}

		public void Visit(AST.FunctionApplicationExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.Constructor expression)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
