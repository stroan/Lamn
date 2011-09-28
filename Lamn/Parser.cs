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
		private class LexemeStream
		{

			private Lexer.Lexeme[] Source { get; set; }
			private int Position { get; set; }

			public bool EOF { get { return Position == Source.Length; } }
			public Lexer.Lexeme Head { get { return Source[Position]; } }
			public Lexer.Lexeme Lookahead { get { return Source[Position + 1]; } }

			public LexemeStream(Lexer.Lexeme[] source)
			{
				Source = source;
				Position = 0;

				PopWhitespaceAndComments();
			}

			private Lexer.Lexeme PopLexeme()
			{
				Lexer.Lexeme lexeme = Head;
				Position++;

				PopWhitespaceAndComments();

				return lexeme;
			}

			public void PopWhitespaceAndComments()
			{
				while (!EOF && (Head.LexemeType == Lexer.Lexeme.Type.COMMENT ||
								Head.LexemeType == Lexer.Lexeme.Type.WHITESPACE))
				{
					Position++;
				}
			}
		}

		private LexemeStream Stream { get; set; }

		public Parser(List<Lexer.Lexeme> input)
		{
			Stream = new LexemeStream(input.ToArray());
		}

		public AST Parse()
		{
			return null;
		}
	}
}
