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

		public class Statement {
		    public Statement(Lexer.Lexeme lexeme) { }
		}

		public Chunk Body { get; private set; }

		public AST(Chunk chunk)
		{
			Body = chunk;
		}
	}
}
