using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class LeftExpressionCompiler : AST.ExpressionVisitor
	{
		public CompilerState State { get; private set; }
		public int NumResults { get; private set; }

		public LeftExpressionCompiler(CompilerState state)
		{
			State = state;
			NumResults = 1;
		}

		#region ExpressionVisitor Members

		public void Visit(AST.NameExpression expression)
		{
			return;
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
			expression.Obj.Visit(new ExpressionCompiler(State, 1));
		}

		public void Visit(AST.SelfLookupExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.IndexExpression expression)
		{
			expression.Obj.Visit(new ExpressionCompiler(State, 1));
			expression.Index.Visit(new ExpressionCompiler(State, 1));
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
