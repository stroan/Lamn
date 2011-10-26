using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class Compiler
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
		}

		public class ChunkCompiler : AST.StatementVisitor
		{
			private bool hasReturned = false;

			public CompilerState State { get; set; }

			public ChunkCompiler(AST.Chunk chunk, CompilerState state)
			{
				State = state;

				foreach (AST.Statement statement in chunk.Statements) {
					statement.Visit(this);
				}

				if (!hasReturned)
				{
					state.bytecodes.Add(VirtualMachine.OpCodes.MakeRET(0));
				}
			}

			#region StatementVisitor Members

			public void Visit(AST.LocalAssignmentStatement statement)
			{
				int numVars = statement.Variables.Count;

				int initialStackPosition = State.stackPosition;
				for (int i = 0; i < statement.Expressions.Count; i++)
				{
					int numResults = 1;
					if (i == statement.Expressions.Count - 1)
					{
						numResults = numVars - i;
					}
					statement.Expressions[i].Visit(new ExpressionCompiler(State, numResults));
				}

				int nowStackPosition = State.stackPosition;
				for (int i = 0; i < numVars - (nowStackPosition - initialStackPosition); i++)
				{
					State.constants.Add(null);
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.constants.IndexOf(null)));
					State.stackPosition++;
				}

				int initialClosureStackPosition = State.closureStackPosition;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(statement.Variables.Count));
				State.closureStackPosition += statement.Variables.Count;

				for (int i = 0; i < statement.Variables.Count; i++)
				{
					State.stackVars[statement.Variables[i]] = initialStackPosition + i;
					State.newClosedVars[statement.Variables[i]] = initialClosureStackPosition + i;
				}
			}

			public void Visit(AST.LocalFunctionStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.IfStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.WhileStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.DoStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.ForStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionCallStatement statement)
			{
				statement.Expr.Visit(new ExpressionCompiler(State, 0));
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(0, true));
				State.stackPosition--;
			}

			public void Visit(AST.AssignmentStatement statement)
			{
				int numVars = statement.Variables.Count;

				int initialStackPosition = State.stackPosition;
				for (int i = 0; i < statement.Expressions.Count; i++)
				{
					int numResults = 1;
					if (i == statement.Expressions.Count - 1)
					{
						numResults = numVars - i;
					}
					statement.Expressions[i].Visit(new ExpressionCompiler(State, numResults));
				}

				int nowStackPosition = State.stackPosition;
				for (int i = 0; i < statement.Expressions.Count - (nowStackPosition - initialStackPosition); i++)
				{
					State.constants.Add(null);
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.constants.IndexOf(null)));
				}

				for (int i = statement.Variables.Count - 1; i >= 0; i--)
				{
					statement.Variables[i].Visit(new AssignerExpressionCompiler(State));
				}
			}

			public void Visit(AST.RepeatStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.ReturnStatement statement)
			{
				foreach (AST.Expression expr in statement.Expressions)
				{
					ExpressionCompiler compiler = new ExpressionCompiler(State, 1);
					if (expr == statement.Expressions.Last())
					{
						compiler = new ExpressionCompiler(State, 0);
					}

					expr.Visit(compiler);
				}

				if (State.newClosedVars.Count > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPCLOSED(State.closureStackPosition - State.initialClosureStackPosition));
				}

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeRET(statement.Expressions.Count));

				hasReturned = true;
			}

			public void Visit(AST.BreakStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionStatement statement)
			{
				FunctionCompiler funcCompiler = new FunctionCompiler(State, statement.Body);

				VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.State.bytecodes.ToArray(), funcCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), funcCompiler.State.childFunctions);
				State.childFunctions.Add(function);

				AddConstant(function.Id);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(GetConstantIndex(function.Id)));

				AddConstant(statement.MainName);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(GetConstantIndex(statement.MainName)));

				State.stackPosition--;
			}
			#endregion

			#region Helper Members
			private void AddConstant(Object c)
			{
				if (State.constants.Contains(c)) { return; }
				State.constants.Add(c);
			}

			private int GetConstantIndex(Object c)
			{
				return State.constants.IndexOf(c);
			}
			#endregion
		}

		public class AssignerExpressionCompiler : AST.ExpressionVisitor
		{
			public CompilerState State { get; private set; }
			public int NumResults { get; private set; }

			public AssignerExpressionCompiler(CompilerState state)
			{
				State = state;
				NumResults = 1;
			}

			#region ExpressionVisitor Members

			public void Visit(AST.NameExpression expression)
			{
				if (State.stackVars.ContainsKey(expression.Value))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - State.stackVars[expression.Value]));
				}
				else
				{
					State.constants.Add(expression.Value);
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(State.constants.IndexOf(expression.Value)));
					State.stackPosition--;
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
				throw new NotImplementedException();
			}

			public void Visit(AST.IndexExpression expression)
			{
				throw new NotImplementedException();
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

		public class ExpressionCompiler : AST.ExpressionVisitor
		{
			public CompilerState State { get; private set; }
			public int NumResults { get; private set; }

			public ExpressionCompiler(CompilerState state)
			{
				State = state;
				NumResults = 1;
			}

			public ExpressionCompiler(CompilerState state, int numResults)
			{
				State = state;
				NumResults = numResults;
			}

			#region ExpressionVisitor Members

			public void Visit(AST.NameExpression expression)
			{
				if (State.stackVars.ContainsKey(expression.Value))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - State.stackVars[expression.Value]));
					State.stackPosition++;
				}
				else if (State.closedVars.ContainsKey(expression.Value))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETUPVAL(State.closedVars[expression.Value]));
					State.stackPosition++;
				}
				else
				{
					State.constants.Add(expression.Value);
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETGLOBAL(State.constants.IndexOf(expression.Value)));
					State.stackPosition++;
				}
			}

			public void Visit(AST.NumberExpression expression)
			{
				State.constants.Add(expression.Value);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.constants.IndexOf(expression.Value)));
				State.stackPosition++;
			}

			public void Visit(AST.StringExpression expression)
			{
				State.constants.Add(expression.Value);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.constants.IndexOf(expression.Value)));
				State.stackPosition++;
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

			public void Visit(AST.UnOpExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.BinOpExpression expression)
			{
				expression.LeftExpr.Visit(this);
				expression.RightExpr.Visit(this);

				if (expression.Op.Equals("+"))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.ADD);
				}
				else
				{
					throw new NotImplementedException();
				}

				State.stackPosition--;
			}

			public void Visit(AST.FunctionExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.LookupExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.IndexExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionApplicationExpression expression)
			{
				int funcIndex = State.stackPosition;
				expression.Obj.Visit(new ExpressionCompiler(State, 1));

				foreach (AST.Expression expr in expression.Args)
				{
					ExpressionCompiler compiler = new ExpressionCompiler(State, 1);
					if (expr == expression.Args.Last())
					{
						compiler = new ExpressionCompiler(State, 0);
					}

					expr.Visit(compiler);
				}

				int currentIndex = State.stackPosition;

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(expression.Args.Count));

				if (NumResults > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(NumResults, true));
					State.stackPosition = funcIndex + NumResults;
				}
				else {
					State.stackPosition = funcIndex + 1;
				}
			}

			public void Visit(AST.Constructor expression)
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		public class FunctionCompiler
		{
			public CompilerState State { get; set; }
			public String Id { get; set; }

			public FunctionCompiler(CompilerState oldState, AST.Body body) {
				State = new CompilerState();

				State.initialClosureStackPosition = oldState.closureStackPosition;
				State.closureStackPosition = oldState.closureStackPosition;
				State.closedVars = oldState.closedVars;
				foreach (KeyValuePair<String, int> i in oldState.newClosedVars)
				{
					State.closedVars[i.Key] = i.Value;
				}

				State.stackPosition = 1;

				if (body.ParamList.NamedParams.Count > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(body.ParamList.NamedParams.Count));
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(body.ParamList.NamedParams.Count));
					State.stackPosition += body.ParamList.NamedParams.Count;
				}

				int stackIndex = 1;
				foreach (String param in body.ParamList.NamedParams)
				{
					State.stackVars[param] = stackIndex;
					stackIndex++;
				}

				if (body.ParamList.HasVarArgs)
				{
					State.stackVars["..."] = 0;
				}

				new ChunkCompiler(body.Chunk, State);

				Id = Guid.NewGuid().ToString();
			}
		}

		public VirtualMachine.Function CompileAST(AST ast)
		{
			ChunkCompiler chunkCompiler = new ChunkCompiler(ast.Contents, new CompilerState());
			return new VirtualMachine.Function(chunkCompiler.State.bytecodes.ToArray(), chunkCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), chunkCompiler.State.childFunctions);
		}
	}
}
