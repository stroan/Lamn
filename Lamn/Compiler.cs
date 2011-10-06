using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class Compiler
	{
		public class ChunkCompiler : AST.StatementVisitor, AST.ExpressionVisitor
		{
			public List<UInt32> bytecodes = new List<UInt32>();
			public List<Object> constants = new List<Object>();
			public List<VirtualMachine.Function> childFunctions = new List<VirtualMachine.Function>();

			public ChunkCompiler(AST.Chunk chunk)
			{
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

				bytecodes.Add(VirtualMachine.OpCodes.MakeRET(statement.Expressions.Count));
			}

			public void Visit(AST.BreakStatement statement)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionStatement statement)
			{
				ChunkCompiler funcCompiler = new ChunkCompiler(statement.Body.Chunk);

				VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.bytecodes.ToArray(), funcCompiler.constants.ToArray(), Guid.NewGuid().ToString());
				childFunctions.Add(function);

				AddConstant(function.Id);
				bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(GetConstantIndex(function.Id)));

				AddConstant(statement.MainName);
				bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(GetConstantIndex(statement.MainName)));
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
				if (constants.Contains(c)) { return; }
				constants.Add(c);
			}

			private int GetConstantIndex(Object c)
			{
				return constants.IndexOf(c);
			}
			#endregion
		}
		public List<VirtualMachine.Function> CompileAST(AST ast)
		{
			ChunkCompiler chunkCompiler = new ChunkCompiler(ast.Contents);
			List<VirtualMachine.Function> functions = new List<VirtualMachine.Function>();
			functions.Add(new VirtualMachine.Function(chunkCompiler.bytecodes.ToArray(), chunkCompiler.constants.ToArray(), Guid.NewGuid().ToString()));
			functions.AddRange(chunkCompiler.childFunctions);
			return functions;
		}
	}
}
