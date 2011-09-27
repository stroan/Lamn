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

		public class AssignmentStatement : Statement
		{
			public List<Lexer.Lexeme> Variables { get; private set; }
			public List<Lexer.Lexeme> Expressions { get; private set; }

			public AssignmentStatement(List<Lexer.Lexeme> variables, List<Lexer.Lexeme> expressions)
			{
				Variables = variables;
				Expressions = expressions;
			}
		}

		public Chunk Body { get; private set; }

		public AST(Chunk chunk)
		{
			Body = chunk;
		}
	}
}
