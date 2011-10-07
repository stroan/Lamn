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
		}

		public class ChunkCompiler : AST.StatementVisitor, AST.ExpressionVisitor
		{
			public CompilerState State { get; set; }

			public ChunkCompiler(AST.Chunk chunk, CompilerState state)
			{
				State = state;

				foreach (AST.Statement statement in chunk.Statements) {
					statement.Visit(this);
				}
			}

			#region StatementVisitor Members

			public void Visit(AST.LocalAssignmentStatement statement)
			{
				throw new NotImplementedException();
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
				throw new NotImplementedException();
			}

			public void Visit(AST.AssignmentStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.RepeatStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.ReturnStatement statement)
			{
				foreach (AST.Expression expr in statement.Expressions)
				{
					expr.Visit(this);
				}

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeRET(statement.Expressions.Count));
			}

			public void Visit(AST.BreakStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionStatement statement)
			{
				FunctionCompiler funcCompiler = new FunctionCompiler(statement.Body);

				VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.State.bytecodes.ToArray(), funcCompiler.State.constants.ToArray(), Guid.NewGuid().ToString());
				State.childFunctions.Add(function);

				AddConstant(function.Id);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(GetConstantIndex(function.Id)));

				AddConstant(statement.MainName);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(GetConstantIndex(statement.MainName)));
			}
			#endregion

			#region ExpressionVisitor Members

			public void Visit(AST.NameExpression expression)
			{
				throw new NotImplementedException();
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

		public class FunctionCompiler
		{
			public CompilerState State { get; set; }
			public String Id { get; set; }

			public FunctionCompiler(AST.Body body) {
				State = new CompilerState();

				new ChunkCompiler(body.Chunk, State);

				Id = Guid.NewGuid().ToString();
			}
		}

		public List<VirtualMachine.Function> CompileAST(AST ast)
		{
			ChunkCompiler chunkCompiler = new ChunkCompiler(ast.Contents, new CompilerState());
			List<VirtualMachine.Function> functions = new List<VirtualMachine.Function>();
			functions.Add(new VirtualMachine.Function(chunkCompiler.State.bytecodes.ToArray(), chunkCompiler.State.constants.ToArray(), Guid.NewGuid().ToString()));
			functions.AddRange(chunkCompiler.State.childFunctions);
			return functions;
		}
	}
}
