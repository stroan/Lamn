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
 *         retstat |
 *         BREAK |
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
 * expr -> subexpr
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

		private class LexemeStream
		{

			private Lexer.Lexeme[] Source { get; set; }
			private int Position { get; set; }

			public bool EOF { get { return Position == Source.Length; } }

			public Lexer.Lexeme Head { 
				get {
					if (EOF) { return new Lexer.Lexeme(null, Lexer.Lexeme.Type.EOS); }
					return Source[Position]; 
				} 
			}

			public Lexer.Lexeme Lookahead
			{
				get
				{
					if (Position >= Source.Length - 2) { return new Lexer.Lexeme(null, Lexer.Lexeme.Type.EOS); }
					return Source[Position];
				}
			}

			public LexemeStream(Lexer.Lexeme[] source)
			{
				Source = source;
				Position = 0;

				PopWhitespaceAndComments();
			}

			private void PopWhitespaceAndComments()
			{
				while (!EOF && (Head.LexemeType == Lexer.Lexeme.Type.COMMENT ||
								Head.LexemeType == Lexer.Lexeme.Type.WHITESPACE))
				{
					Position++;
				}
			}

			public void MoveNext()
			{
				Position++;
				PopWhitespaceAndComments();
			}

			public bool IsKeyword(String keyword)
			{
				return Head.LexemeType == Lexer.Lexeme.Type.KEYWORD
					&& Head.Value.Equals(keyword);
			}

			public bool IsName()
			{
				return Head.LexemeType == Lexer.Lexeme.Type.NAME;
			}

			public Lexer.Lexeme GetName()
			{
				if (!IsName()) { throw new ParserException(); }
				return Head;
			}

			public bool IsNumber()
			{
				return Head.LexemeType == Lexer.Lexeme.Type.NUMBER;
			}

			public Lexer.Lexeme GetNumber()
			{
				if (!IsNumber()) { throw new ParserException(); }
				return Head;
			}
		}

		private class Body
		{
			public AST.FunctionParamList paramList;
			public AST.Chunk chunk;

			public Body(AST.FunctionParamList p, AST.Chunk c)
			{
				paramList = p;
				chunk = c;
			}
		}

		private LexemeStream Stream { get; set; }

		public Parser(List<Lexer.Lexeme> input)
		{
			Stream = new LexemeStream(input.ToArray());
		}

		public AST Parse()
		{
			return new AST(ParseChunk());
		}

		private AST.Chunk ParseChunk()
		{
			List<AST.Statement> statements = new List<AST.Statement>();

			while (!IsChunkFollow())
			{
				statements.Add(ParseStatement());

				if (Stream.IsKeyword(";")) Stream.MoveNext();
			}

			return new AST.Chunk(statements);
		}

		private bool IsChunkFollow()
		{
			return Stream.EOF || Stream.IsKeyword("end");
		}

		private AST.Statement ParseStatement()
		{
			if (Stream.IsKeyword("local")) {
				return ParseLocalStatement();
			}
			return null;
		}

		private AST.Statement ParseLocalStatement()
		{
			Stream.MoveNext(); // Remove 'local'

			if (Stream.IsKeyword("function"))
			{
				return ParseLocalFunc();
			}
			else
			{
				return ParseLocalDecl();
			}
		}

		/* LOCAL function NAME body 
		   NOTES: local has already been removed from the stream
		 */
		private AST.LocalFunctionStatement ParseLocalFunc()
		{
			Stream.MoveNext(); // Drop function

			String functionName = Stream.GetName().Value;
			Stream.MoveNext();

			Body body = ParseBody();

			return new AST.LocalFunctionStatement(functionName);
		}

		/* body ->  `(' parlist `)' chunk END */
		private Body ParseBody()
		{
			if (!Stream.IsKeyword("(")) { throw new ParserException(); }
			Stream.MoveNext();

			AST.FunctionParamList paramList = ParseParamList();

			if (!Stream.IsKeyword(")")) { throw new ParserException(); }
			Stream.MoveNext();

			AST.Chunk chunk = ParseChunk();

			if (!Stream.IsKeyword("end")) { throw new ParserException(); }
			Stream.MoveNext();

			return new Body(paramList, chunk);
		}

		/* parlist -> [ param { `,' param } ] */
		private AST.FunctionParamList ParseParamList()
		{
			List<String> paramList = new List<String>();
			bool hasVarArgs = false;

			if (!Stream.IsKeyword(")")) // The only follow of parlist
			{
				do
				{
					Stream.MoveNext(); // Drop '(' or ','
					if (Stream.IsName())
					{
						paramList.Add(Stream.GetName().Value);
					}
					else if (Stream.IsKeyword("..."))
					{
						hasVarArgs = true;
					}
					else
					{
						throw new ParserException();
					}
					Stream.MoveNext();
				} while (!hasVarArgs && Stream.IsKeyword(","));
			}

			return new AST.FunctionParamList(paramList, hasVarArgs);
		}

		/* localstat -> LOCAL NAME {`,' NAME} [`=' explist1] 
		   NOTES: local has already been removed from the stream
		 */
		private AST.LocalAssignmentStatement ParseLocalDecl()
		{
			List<String> variableNames = new List<String>();
			variableNames.Add(Stream.GetName().Value);
			Stream.MoveNext();

			while (Stream.IsKeyword(","))
			{
				Stream.MoveNext();
				variableNames.Add(Stream.GetName().Value);
				Stream.MoveNext();
			}

			List<AST.Expression> expressions = null;

			if (Stream.IsKeyword("="))
			{
				Stream.MoveNext();

				expressions = ParseExpressionList1();
			}

			return new AST.LocalAssignmentStatement(variableNames, expressions);
		}

		/* explist1 -> expr { `,' expr } */
		private List<AST.Expression> ParseExpressionList1()
		{
			List<AST.Expression> expressions = new List<AST.Expression>();
			expressions.Add(ParseExpression());

			while (Stream.IsKeyword(","))
			{
				Stream.MoveNext();
				expressions.Add(ParseExpression());
			}

			return expressions;
		}

		private AST.Expression ParseExpression()
		{
			AST.Expression expression = new AST.NumberExpression(Stream.GetNumber().NumberValue.Value);
			Stream.MoveNext();
			return expression;
		}
	}
}
