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

			public LocalFunctionStatement(String name)
			{
				Name = name;
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

		public class Expression { }

		public class NumberExpression : Expression
		{
			public double Value { get; private set; }

			public NumberExpression(double value)
			{
				Value = value;
			}
		}

		public Chunk Body { get; private set; }

		public AST(Chunk chunk)
		{
			Body = chunk;
		}
	}
}
