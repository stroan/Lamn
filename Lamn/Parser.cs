using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class Parser
	{
		public class ParserException : Exception { }

		private class LexemeStream
		{

			private Lexer.Lexeme[] Source { get; set; }
			private int Position { get; set; }
			private Stack<int> StateStack { get; set; }

			public bool EOF { get { return Position == Source.Length; } }
			public Lexer.Lexeme Head { get { return Source[Position]; } }

			public LexemeStream(Lexer.Lexeme[] source)
			{
				Source = source;
				Position = 0;
				StateStack = new Stack<int>();
			}

			public void PushState()
			{
				StateStack.Push(Position);
			}

			public void PopState()
			{
				Position = StateStack.Pop();
			}

			public Lexer.Lexeme TryPopKeyword(String id)
			{
				PopWhitespaceAndComments();

				if (!EOF && Head.LexemeType == Lexer.Lexeme.Type.KEYWORD &&
					Head.Value.Equals(id))
				{
					return PopLexeme(); 
				}

				return null;
			}

			private Lexer.Lexeme PopLexeme()
			{
				Lexer.Lexeme lexeme = Head;
				Position++;
				return lexeme;
			}

			public void PopWhitespaceAndComments()
			{
				while (!EOF && (Head.LexemeType == Lexer.Lexeme.Type.COMMENT ||
								Head.LexemeType == Lexer.Lexeme.Type.WHITESPACE))
				{
					PopLexeme();
				}
			}
		}

		public AST Parse(List<Lexer.Lexeme> tokens)
		{
			return new AST(ParseChunk(new LexemeStream(tokens.ToArray())));
		}

		private AST.Chunk ParseChunk(LexemeStream tokens)
		{
			tokens.PushState();

			List<AST.Statement> statements = new List<AST.Statement>();

			AST.Statement statement;
			while ((statement = ParseStatement(tokens)) != null)
			{
				statements.Add(statement);
				tokens.TryPopKeyword(";");
			}

			tokens.PopState();

			return new AST.Chunk(statements);
		}

		private AST.Statement ParseStatement(LexemeStream tokens)
		{
			Lexer.Lexeme lexeme = tokens.TryPopKeyword("not");

			if (lexeme == null)
			{
				return null;
			}

			return new AST.Statement(lexeme);
		}
	}
}
