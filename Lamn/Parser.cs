using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Grammar as defined in lparser.c
 * 
 * chunk -> { stat [';'] }
 * block -> chunk
 * 
 * stat -> ifstat |
 *         whilestat |
 *         DO block END |
 *         forstat |
 *         repeatstat |
 *         funcstat |
 *         localstat | localfunc |   <- lookahead for NAME | function
 *         exprstat
 * 
 * ifstat -> IF cond THEN block {ELSEIF cond THEN block} [ELSE block] END
 * test_then_block -> [IF | ELSEIF] cond THEN block
 * 
 * whilestat -> WHILE cond DO block END
 * 
 * forstat -> FOR (fornum | forlist) END       <- lookahead for '='|','
 * forbody -> DO block
 * fornum -> NAME = exp1,exp1[,exp1] forbody
 * forlist -> NAME {,NAME} IN explist1 forbody
 * 
 * repeatstat -> REPEAT block UNTIL cond
 * 
 * funcstat -> FUNCTION funcname body
 * funcname -> NAME {field} [`:' NAME]
 * field -> ['.' | ':'] NAME
 * 
 * localstat -> LOCAL NAME {`,' NAME} [`=' explist1]
 * 
 * retstat -> RETURN explist
 * 
 * localfunc -> LOCAL function NAME body
 * body ->  `(' parlist `)' chunk END
 * parlist -> [ param { `,' param } ]
 * 
 * exprstat -> func | assignment    <- e = primaryexp if is_function_call(e) { e assignment } else { e }
 * assignment -> `,' primaryexp assignment |
 *               `=' explist1
 * explist1 -> expr { `,' expr }
 * 
 * cond -> exp                      <- if exp = nil { false } else { exp }
 * 
 * subexpr -> (simpleexp | unop subexpr) { binop subexpr }  <- Expanding binop until priority is less or equal to the priovious op>
 * simpleexp -> NUMBER | STRING | NIL | true | false | ... |
 *                 constructor | FUNCTION body | primaryexp
 * primaryexp -> prefixexp { `.' NAME | index | `:' NAME funcargs | funcargs }
 * prefixexp -> NAME | '(' expr ')'
 * funcargs -> `(' [ explist1 ] `)' |
 *             constructor |
 *             STRING
 * 
 * constructor -> '{' { confield [fieldsep] } [fieldsep] '}'
 * confield -> recfield | expr    <- lookahead for '='
 * recfield -> (NAME | index) = exp1
 * fieldsep = ';' | ','
 * 
 * index -> '[' expr ']'
*/

namespace Lamn
{
	class Parser
	{
		public class ParserException : Exception { }
		private class NoMatchException : Exception { }

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

			#region State stack
			public void PushState()
			{
				StateStack.Push(Position);
			}

			public void PopState()
			{
				Position = StateStack.Pop();
			}

			private Lexer.Lexeme PopLexeme()
			{
				Lexer.Lexeme lexeme = Head;
				Position++;
				return lexeme;
			}
			#endregion

			#region Lexeme extraction
			public Lexer.Lexeme PopKeyword(String id)
			{
				PopWhitespaceAndComments();

				if (!EOF && Head.LexemeType == Lexer.Lexeme.Type.KEYWORD &&
					Head.Value.Equals(id))
				{
					return PopLexeme(); 
				}

				throw new NoMatchException();
			}

			public Lexer.Lexeme PopName() 
			{
				PopWhitespaceAndComments();

				if (!EOF && Head.LexemeType == Lexer.Lexeme.Type.NAME)
				{
					return PopLexeme();
				}
				
				throw new NoMatchException();
			}

			public Lexer.Lexeme PopEOF()
			{
				PopWhitespaceAndComments();

				if (EOF)
				{
					return null;
				}

				throw new NoMatchException();
			}

			public void PopWhitespaceAndComments()
			{
				while (!EOF && (Head.LexemeType == Lexer.Lexeme.Type.COMMENT ||
								Head.LexemeType == Lexer.Lexeme.Type.WHITESPACE))
				{
					PopLexeme();
				}
			}
			#endregion
		}

		private delegate A ParseOp<A>(LexemeStream stream);

		public AST Parse(List<Lexer.Lexeme> tokens)
		{
			return new AST(ParseChunk(new LexemeStream(tokens.ToArray())));
		}

		#region Parse Chunk
		private AST.Chunk ParseChunk(LexemeStream tokens)
		{
			List<AST.Statement> statements = Many(new ParseOp<AST.Statement>(ParseChunkPart))(tokens);
			tokens.PopEOF();
			return new AST.Chunk(statements);
		}

		private AST.Statement ParseChunkPart(LexemeStream tokens)
		{
			AST.Statement statement = ParseStatement(tokens);
			Optional(t => t.PopKeyword(";"))(tokens);
			return statement;
		}
		#endregion

		private AST.Statement ParseStatement(LexemeStream tokens)
		{
			return ParseAssignmentStatement(tokens);
		}

		private AST.AssignmentStatement ParseAssignmentStatement(LexemeStream tokens)
		{
			List<Lexer.Lexeme> vars = SeperatedList1(new ParseOp<Lexer.Lexeme>(ParseVar), new ParseOp<Lexer.Lexeme>(ParseComma))(tokens);
			tokens.PopKeyword("=");
			List<Lexer.Lexeme> exps = SeperatedList1(new ParseOp<Lexer.Lexeme>(ParseVar), new ParseOp<Lexer.Lexeme>(ParseComma))(tokens);

			return new AST.AssignmentStatement(vars, exps);
		}

		private Lexer.Lexeme ParseVar(LexemeStream tokens) 
		{
			return tokens.PopName();
		}

		private Lexer.Lexeme ParseComma(LexemeStream tokens)
		{
			return tokens.PopKeyword(",");
		}

		#region Parser Combinators
		private ParseOp<A> Optional<A>(ParseOp<A> op)
		{
			return tokens =>
			{
				try
				{
					return op(tokens);
				}
				catch (NoMatchException)
				{
					return default(A);
				}
			};
		}

		private ParseOp<List<A>> Many<A>(ParseOp<A> op)
		{
			return tokens =>
			{
				List<A> outputList = new List<A>();

				while (true)
				{
					try
					{
						outputList.Add(op(tokens));
					}
					catch (NoMatchException)
					{
						break;
					}
				}

				return outputList;
			};
		}

		private ParseOp<List<A>> SeperatedList1<A, B>(ParseOp<A> valueOp, ParseOp<B> sepOp)
		{
			return tokens =>
			{
				List<A> values = new List<A>();
				values.Add(valueOp(tokens));
				values.AddRange(Many(t => {sepOp(t); return valueOp(t);})(tokens));
				return values;
			};
		}
		#endregion
	}
}
