using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* LL(1) Lua Grammar (pending)
  
	DONE) chunk ::= {stat [`;´]} [laststat [`;´]]

	DONE) block ::= chunk                                                       

        =============================================================================================
	XXXX) stat ::=  varlist `=´ explist |                                         (????)
		      functioncall |                                                  (????)
		      do block end |                                                  (do)
		      while exp do block end |                                        (while)
		      repeat block until exp |                                        (repeat)
		      if exp then block {elseif exp then block} [else block] end |    (if)
		      for forclause do block end |                                    (for)
		      function funcname funcbody |                                    (function)
		      local localdecl |                                               (local)
		      
        =============================================================================================
        NEW) localdecl ::= function Name funcbody |                                   (function)
                           namelist [`=` explist]                                     (Name)
                                                                                      --------
                                                                                      (function, Name)
                                                                                      
        =============================================================================================
        NEW) forclause ::= Name forrest                                               (Name)
                                                                                      --------
                                                                                      (Name)
        
        =============================================================================================
        NEW) forrest ::= `=´ exp `,´ exp [`,´ exp] |                                  (=)
                         `,´ nameList in explist |                                    (,)
                         in explist                                                   (in)
                                                                                      --------
                                                                                      (=, `,´, in)

        =============================================================================================
	DONE) laststat ::= return [explist] |                                         (return)
	                   break                                                      (break)
	                                                                              --------
	                                                                              (return, break)

        =============================================================================================
	DONE) funcname ::= Name {`.´ Name} [`:´ Name]                                 (Name)
                                                                                      --------
                                                                                      (Name)
                                                                                      
        =============================================================================================
	DONE) varlist ::= var {`,´ var}

        =============================================================================================
	XXXX) var ::=  Name | prefixexp `[´ exp `]´ | prefixexp `.´ Name 

        =============================================================================================
	DONE) namelist ::= Name {`,´ Name}                                            (Name)
	                                                                              --------
	                                                                              (Name)

        =============================================================================================
	DONE) explist ::= {exp `,´} exp

        =============================================================================================
	XXXX) exp ::=  nil | false | true | Number | String | `...´ | function | 
		 prefixexp | tableconstructor | exp binop exp | unop exp 

        =============================================================================================
	XXXX) prefixexp ::= var | functioncall | `(´ exp `)´

        =============================================================================================
	XXXX) functioncall ::=  prefixexp args | prefixexp `:´ Name args 

        =============================================================================================
	DONE) args ::=  `(´ [explist] `)´ | tableconstructor | String 

        =============================================================================================
	DONE) function ::= function funcbody

        =============================================================================================
	DONE) funcbody ::= `(´ [parlist] `)´ block end

        =============================================================================================
	DONE) parlist ::= namelist [`,´ `...´] | `...´

        =============================================================================================
	DONE) tableconstructor ::= `{´ [fieldlist] `}´

        =============================================================================================
	DONE) fieldlist ::= field {fieldsep field} [fieldsep]

        =============================================================================================
	????) field ::= `[´ exp `]´ `=´ exp | Name `=´ exp | exp

        =============================================================================================
	DONE) fieldsep ::= `,´ | `;´

        =============================================================================================
	DONE) binop ::= `+´ | `-´ | `*´ | `/´ | `^´ | `%´ | `..´ | 
		      `<´ | `<=´ | `>´ | `>=´ | `==´ | `~=´ | 
		      and | or

        =============================================================================================
	DONE) unop ::= `-´ | not | `#´
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
