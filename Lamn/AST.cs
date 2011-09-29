using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class AST
	{
		public class Chunk
		{
			public List<Statement> Statements { get; private set; }

			public Chunk(List<Statement> statements)
			{
				Statements = statements;
			}
		}

		public class Statement { }

		public class LocalAssignmentStatement : Statement
		{
			public List<String> Variables { get; private set; }
			public List<Expression> Expressions { get; private set; }

			public LocalAssignmentStatement(List<String> variables, List<Expression> expressions)
			{
				Variables = variables;
				Expressions = expressions;
			}
		}

		public class LocalFunctionStatement : Statement
		{
			public String Name { get; private set; }
			public Body Body { get; private set; }

			public LocalFunctionStatement(String name,Body body)
			{
				Name = name;
				Body = body;
			}
		}

		public class FunctionParamList
		{
			public List<String> NamedParams { get; private set; }
			public bool HasVarArgs { get; private set; }

			public FunctionParamList(List<String> parameters, bool hasVarArgs)
			{
				NamedParams = parameters;
				HasVarArgs = hasVarArgs;
			}
		}

		public class Body
		{
			public FunctionParamList ParamList { get; private set; }
			public Chunk Chunk { get; private set; }

			public Body(FunctionParamList p, Chunk c)
			{
				ParamList = p;
				Chunk = c;
			}
		}

		public class Expression { }

		public class NameExpression : Expression
		{
			public String Value { get; private set; }

			public NameExpression(String value)
			{
				Value = value;
			}
		}

		public class NumberExpression : Expression
		{
			public double Value { get; private set; }

			public NumberExpression(double value)
			{
				Value = value;
			}
		}

		public class StringExpression : Expression
		{
			public String Value { get; private set; }

			public StringExpression(String value)
			{
				Value = value;
			}
		}

		public class BoolExpression : Expression
		{
			public bool Value { get; private set; }

			public BoolExpression(bool value)
			{
				Value = value;
			}
		}

		public class NilExpression : Expression { }

		public class VarArgsExpression : Expression { }

		public class UnOpExpression : Expression
		{
			public String Op { get; private set; }
			public Expression Expr { get; private set; }

			public UnOpExpression(String op, Expression expr)
			{
				Op = op;
				Expr = expr;
			}
		}

		public class FunctionExpression : Expression
		{
			public Body Body { get; private set; }

			public FunctionExpression(Body body)
			{
				Body = body;
			}
		}

		public class Constructor : Expression
		{
			public List<ConField> Fields { get; private set; }

			public Constructor(List<ConField> fields)
			{
				Fields = fields;
			}
		}

		public class ConField { }

		public class ListField : ConField { 
			public Expression Expr { get; private set; }

			public ListField(Expression expr)
			{
				Expr = expr;
			}
		}

		public class NameRecField : ConField {
			public String Name { get; private set; }
			public Expression Value { get; private set; }

			public NameRecField(String name, Expression value)
			{
				Name = name;
				Value = value;
			}
		}

		public class ExprRecField : ConField { 
			public Expression IndexExpr { get; private set; }
			public Expression Value { get; private set; }

			public ExprRecField(Expression indexExpr, Expression value)
			{
				IndexExpr = indexExpr;
				Value = value;
			}
		}

		public Chunk Contents { get; private set; }

		public AST(Chunk chunk)
		{
			Contents = chunk;
		}
	}
}
