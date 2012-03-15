using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
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
					if (EOF) { return new Lexer.Lexeme(null, Lexer.Lexeme.Type.EOS, null); }
					return Source[Position]; 
				} 
			}

			public Lexer.Lexeme Lookahead
			{
				get
				{
					int index = Position + 1;
					while (index < Source.Length &&
						        (Source[index].LexemeType == Lexer.Lexeme.Type.COMMENT ||
								Source[index].LexemeType == Lexer.Lexeme.Type.WHITESPACE))
					{
						index++;
					}

					if (index < Source.Length)
					{
						return Source[index];
					}

					return new Lexer.Lexeme(null, Lexer.Lexeme.Type.EOS, null);
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

			public bool IsKeywordAndMove(String keyword)
			{
				if (IsKeyword(keyword))
				{
					MoveNext();
					return true;
				}

				return false;
			}

			public Lexer.Lexeme GetKeywordAndMove(String keyword)
			{
				if (!IsKeyword(keyword)) { throw new ParserException(); }

				Lexer.Lexeme lexeme = Head;
				MoveNext();

				return lexeme;
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

			public bool IsString()
			{
				return Head.LexemeType == Lexer.Lexeme.Type.STRING;
			}

			public Lexer.Lexeme GetString()
			{
				if (!IsString()) { throw new ParserException(); }
				return Head;
			}
		}

		#region Operator Precedences
		private static Dictionary<String, int> opLeftPriorities = new Dictionary<String, int>() {
			{"+", 6},{"-", 6},{"*", 7},{"/", 7},{"%", 7},
			{"^", 10},{"..", 5},
			{"==", 3},{"~=", 3},
			{">", 3},{"<", 3},{">=", 3},{"<=", 3},
			{"and", 2},
			{"or", 1}
		};

		private static Dictionary<String, int> opRightPriorities = new Dictionary<String, int>() {
			{"+", 6},{"-", 6},{"*", 7},{"/", 7},{"%", 7},
			{"^", 9},{"..", 4},
			{"==", 3},{"~=", 3},
			{">", 3},{"<", 3},{">=", 3},{"<=", 3},
			{"and", 2},
			{"or", 1}
		};
		#endregion

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
				if (Stream.IsKeyword(";"))
				{
					Stream.MoveNext();
				}
				else
				{
					statements.Add(ParseStatement());
				}
			}

			return new AST.Chunk(statements);
		}

		private bool IsChunkFollow()
		{
			return Stream.EOF || Stream.IsKeyword("end") || Stream.IsKeyword("elseif") || Stream.IsKeyword("else") || Stream.IsKeyword("until");
		}

		private AST.Statement ParseStatement()
		{
			if (Stream.IsKeyword("if"))
			{
				return ParseIfStatement();
			}
			else if (Stream.IsKeyword("while"))
			{
				return ParseWhileStatement();
			}
			else if (Stream.IsKeyword("local"))
			{
				return ParseLocalStatement();
			}
			else if (Stream.IsKeyword("do"))
			{
				return ParseDoStatement();
			}
			else if (Stream.IsKeyword("for"))
			{
				return ParseForStatement();
			}
			else if (Stream.IsKeyword("repeat"))
			{
				return ParseRepeatStatement();
			}
			else if (Stream.IsKeyword("function"))
			{
				return ParseFunctionStatement();
			}
			else if (Stream.IsKeyword("return"))
			{
				return ParseReturnStatement();
			}
			else if (Stream.IsKeyword("break"))
			{
				Stream.MoveNext();
				return new AST.BreakStatement();
			}
			else
			{
				return ParseExpressionStatement();
			}
		}

		/* ifstat -> IF cond THEN block {ELSEIF cond THEN block} [ELSE block] END */
		private AST.Statement ParseIfStatement()
		{
			List<AST.TestBlock> conditions = new List<AST.TestBlock>();

			conditions.Add(ParseTestBlock());

			while (Stream.IsKeyword("elseif"))
			{
				conditions.Add(ParseTestBlock());
			}

			if (Stream.IsKeyword("else"))
			{
				Stream.MoveNext();
				conditions.Add(new AST.TestBlock(new AST.BoolExpression(true), ParseChunk()));
			}

			Stream.GetKeywordAndMove("end");

			return new AST.IfStatement(conditions);
		}

		/* test_then_block -> [IF | ELSEIF] cond THEN block */
		private AST.TestBlock ParseTestBlock()
		{
			if (Stream.IsKeyword("if") || Stream.IsKeyword("elseif"))
			{
				Stream.MoveNext();
				AST.Expression cond = ParseExpression();
				Stream.GetKeywordAndMove("then");
				AST.Chunk block = ParseChunk();

				return new AST.TestBlock(cond, block);
			}
			else
			{
				throw new ParserException();
			}
		}

		/* whilestat -> WHILE cond DO block END */
		private AST.Statement ParseWhileStatement()
		{
			Stream.GetKeywordAndMove("while");

			AST.Expression cond = ParseCond();

			Stream.GetKeywordAndMove("do");

			AST.Chunk block = ParseChunk();

			Stream.GetKeywordAndMove("end");

			return new AST.WhileStatement(cond, block);
		}

		/* DO block END */
		private AST.Statement ParseDoStatement()
		{
			Stream.GetKeywordAndMove("do");

			AST.Chunk block = ParseChunk();

			Stream.GetKeywordAndMove("end");

			return new AST.DoStatement(block);
		}

		/* forstat -> FOR (fornum | forlist) END       <- lookahead for '='|',' */
		private AST.Statement ParseForStatement()
		{
			Stream.GetKeywordAndMove("for");

			AST.ForClause clause;

			Lexer.Lexeme lookahead = Stream.Lookahead;
			/* fornum -> NAME = exp1,exp1[,exp1] forbody */
			if (lookahead.LexemeType == Lexer.Lexeme.Type.KEYWORD && lookahead.Value.Equals("="))
			{
				String name = Stream.GetName().Value;
				Stream.MoveNext();

				Stream.GetKeywordAndMove("=");

				AST.Expression expr1 = ParseExpression();

				Stream.GetKeywordAndMove(",");

				AST.Expression expr2 = ParseExpression();

				AST.Expression expr3 = null;

				if (Stream.IsKeyword(","))
				{
					Stream.MoveNext();
					expr3 = ParseExpression();
				}

				clause = new AST.NumForClause(name, expr1, expr2, expr3);
			}
			else /* forlist -> NAME {,NAME} IN explist1 forbody */
			{
				List<String> names = new List<String>();
				names.Add(Stream.GetName().Value);
				Stream.MoveNext();

				while (Stream.IsKeyword(","))
				{
					Stream.MoveNext();

					names.Add(Stream.GetName().Value);
					Stream.MoveNext();
				}

				Stream.GetKeywordAndMove("in");

				List<AST.Expression> expressions = ParseExpressionList1();

				clause = new AST.ListForClause(names, expressions);
			}

			/* forbody -> DO block */
			Stream.GetKeywordAndMove("do");

			AST.Chunk block = ParseChunk();

			Stream.GetKeywordAndMove("end");

			return new AST.ForStatement(clause, block);
		}

		/* repeatstat -> REPEAT block UNTIL cond */
		private AST.Statement ParseRepeatStatement()
		{
			Stream.GetKeywordAndMove("repeat");

			AST.Chunk block = ParseChunk();

			Stream.GetKeywordAndMove("until");

			AST.Expression cond = ParseCond();

			return new AST.RepeatStatement(block, cond);
		}

		/* funcstat -> FUNCTION funcname body
		   funcname -> NAME {field} [`:' NAME]
		   field -> '.' NAME */
		private AST.Statement ParseFunctionStatement()
		{
			Stream.GetKeywordAndMove("function");

			String mainName = Stream.GetName().Value;
			Stream.MoveNext();

			List<String> fields = new List<string>();
			while (Stream.IsKeyword("."))
			{
				Stream.MoveNext();
				fields.Add(Stream.GetName().Value);
				Stream.MoveNext();
			}

			String selfName = null;
			if (Stream.IsKeyword(":"))
			{
				Stream.MoveNext();
				selfName = Stream.GetName().Value;
				Stream.MoveNext();
			}

			AST.Body body = ParseBody();

			return new AST.FunctionStatement(mainName, fields, selfName, body);
		}

		/* retstat -> RETURN explist */
		private AST.Statement ParseReturnStatement()
		{
			Stream.GetKeywordAndMove("return");

			List<AST.Expression> expressions = null;

			if (!Stream.IsKeyword(";") && !IsChunkFollow())
			{
				expressions = ParseExpressionList1();
			}

			return new AST.ReturnStatement(expressions);
		}

		/* exprstat -> func | assignment    <- e = primaryexp if is_function_call(e) { e assignment } else { e }
           assignment -> `,' primaryexp assignment |
                         `=' explist1 */
		private AST.Statement ParseExpressionStatement()
		{
			AST.Expression primaryExp = ParsePrimaryExpr();

			if (primaryExp is AST.FunctionApplicationExpression)
			{
				return new AST.FunctionCallStatement(primaryExp);
			}
			else if (Stream.IsKeyword(",") || Stream.IsKeyword("="))
			{
				List<AST.Expression> vars = new List<AST.Expression>() { primaryExp };
				while (Stream.IsKeyword(","))
				{
					Stream.MoveNext();
					vars.Add(ParseExpression());
				}

				Stream.GetKeywordAndMove("=");

				List<AST.Expression> exprs = ParseExpressionList1();

				return new AST.AssignmentStatement(vars, exprs);
			}
			else
			{
				throw new ParserException();
			}
		}

		/* localstat -> LOCAL function NAME body |
		 *              LOCAL NAME {`,' NAME} [`=' explist1]
		 */
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

		/* localstat -> LOCAL function NAME body 
		   NOTES: local has already been removed from the stream
		 */
		private AST.LocalFunctionStatement ParseLocalFunc()
		{
			Stream.MoveNext(); // Drop function

			Lexer.Lexeme firstToken = Stream.GetName();
			Lexer.Position pos = firstToken.FilePosition;
			String functionName = firstToken.Value;
			Stream.MoveNext();

			AST.Body body = ParseBody();

			return new AST.LocalFunctionStatement(functionName, body, pos);
		}

		/* body ->  `(' parlist `)' chunk END */
		private AST.Body ParseBody()
		{
			Stream.GetKeywordAndMove("(");

			AST.FunctionParamList paramList = ParseParamList();

			Stream.GetKeywordAndMove(")");

			AST.Chunk chunk = ParseChunk();

			Stream.GetKeywordAndMove("end");

			return new AST.Body(paramList, chunk);
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
				} while (!hasVarArgs && Stream.IsKeywordAndMove(","));
			}

			return new AST.FunctionParamList(paramList, hasVarArgs);
		}

		/* localstat -> LOCAL NAME {`,' NAME} [`=' explist1] 
		   NOTES: local has already been removed from the stream
		 */
		private AST.LocalAssignmentStatement ParseLocalDecl()
		{
			Lexer.Lexeme firstToken = Stream.GetName();
			Lexer.Position pos = firstToken.FilePosition;

			List<String> variableNames = new List<String>();
			variableNames.Add(firstToken.Value);
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

			return new AST.LocalAssignmentStatement(variableNames, expressions, pos);
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

		/* cond -> exp        <- if exp = nil { false } else { exp } */
		private AST.Expression ParseCond()
		{
			AST.Expression expr = ParseExpression();

			if (expr is AST.NilExpression)
			{
				return new AST.BoolExpression(false);
			}

			return expr;
		}

		/* expr -> subexpr */
		private AST.Expression ParseExpression()
		{
			return ParseSubExpression(0);
		}

		/* subexpr -> (simpleexp | unop subexpr) { binop subexpr }  <- Expanding binop until priority is less or equal to the priovious op> */
		private AST.Expression ParseSubExpression(int limit)
		{
			AST.Expression expr;
			if (Stream.IsKeyword("-") || Stream.IsKeyword("not") || Stream.IsKeyword("")) /* unop subexpr */
			{
				String unaryOp = Stream.Head.Value;
				Stream.MoveNext();

				expr = new AST.UnOpExpression(unaryOp, ParseSubExpression(8));
			}
			else /* simpleexp */
			{
				expr = ParseSimpleExpr();
			}

			/* { binop subexpr } <- Expanding binop until priority is less or equal to the priovious op> */
			/* TODO: THIS */

			while (IsBinOp() && opLeftPriorities[Stream.Head.Value] > limit)
			{
				String opName = Stream.Head.Value;
				Stream.MoveNext();

				AST.Expression rightExpr = ParseSubExpression(opRightPriorities[opName]);

				expr = new AST.BinOpExpression(opName, expr, rightExpr);
			}

			return expr;
		}

		/* binop ::= `+´ | `-´ | `*´ | `/´ | `^´ | `%´ | `..´ | 
		 `<´ | `<=´ | `>´ | `>=´ | `==´ | `~=´ | 
		 and | or */
		private bool IsBinOp()
		{
			return Stream.IsKeyword("+") || Stream.IsKeyword("-") || Stream.IsKeyword("*") ||
				   Stream.IsKeyword("/") || Stream.IsKeyword("^") || Stream.IsKeyword("%") ||
				   Stream.IsKeyword("..") || Stream.IsKeyword("<") || Stream.IsKeyword("<=") ||
				   Stream.IsKeyword(">") || Stream.IsKeyword(">=") || Stream.IsKeyword("==") ||
				   Stream.IsKeyword("~=") || Stream.IsKeyword("and") || Stream.IsKeyword("or");
		}

		/* simpleexp -> NUMBER | STRING | NIL | true | false | ... |
                        constructor | FUNCTION body | primaryexp */
		private AST.Expression ParseSimpleExpr()
		{
			AST.Expression expr = null;

			if (Stream.IsNumber())
			{
				expr = new AST.NumberExpression(Stream.GetNumber().NumberValue.Value);
				Stream.MoveNext();
			}
			else if (Stream.IsString())
			{
				expr = new AST.StringExpression(Stream.GetString().Value);
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("nil"))
			{
				expr = new AST.NilExpression();
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("true"))
			{
				expr = new AST.BoolExpression(true);
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("false"))
			{
				expr = new AST.BoolExpression(false);
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("..."))
			{
				expr = new AST.VarArgsExpression();
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("{"))
			{
				expr = ParseConstructor();
			}
			else if (Stream.IsKeyword("function"))
			{
				Stream.MoveNext();

				return new AST.FunctionExpression(ParseBody());
			}
			else
			{
				expr = ParsePrimaryExpr();
			}

			return expr;
		}

		/* primaryexp -> prefixexp { `.' NAME | index | `:' NAME funcargs | funcargs } */
		private AST.Expression ParsePrimaryExpr()
		{
			AST.Expression prefixExp;

			/* prefixexp -> NAME | '(' expr ')' */
			if (Stream.IsName())
			{
				prefixExp = new AST.NameExpression(Stream.GetName().Value);
				Stream.MoveNext();
			}
			else if (Stream.IsKeyword("("))
			{
				Stream.MoveNext();

				prefixExp = new AST.ParenExpression(ParseExpression());

				Stream.GetKeywordAndMove(")");
			}
			else
			{
				throw new ParserException();
			}

			AST.Expression primaryExp = prefixExp;

			while (true)
			{
				if (Stream.IsKeyword("."))
				{
					Stream.MoveNext();
					String lookupName = Stream.GetName().Value;
					Stream.MoveNext();
					primaryExp = new AST.LookupExpression(primaryExp, lookupName);
				}
				else if (Stream.IsKeyword("[")) /* index -> '[' expr ']' */
				{
					Stream.MoveNext();

					primaryExp = new AST.IndexExpression(primaryExp, ParseExpression());

					Stream.GetKeywordAndMove("]");
				}
				else if (Stream.IsKeyword(":"))
				{
					Stream.MoveNext();

					String functionName = Stream.GetName().Value;
					Stream.MoveNext();

					primaryExp = new AST.SelfLookupExpression(primaryExp, functionName);

					List<AST.Expression> args = ParseFuncArgs();

					primaryExp = new AST.FunctionApplicationExpression(primaryExp, args);
				}
				else if (Stream.IsKeyword("(") || Stream.IsKeyword("{") || Stream.IsString())
				{
					List<AST.Expression> args = ParseFuncArgs();
					primaryExp = new AST.FunctionApplicationExpression(primaryExp, args);
				}
				else
				{
					break;
				}
			}

			return primaryExp;
		}

		/* funcargs -> `(' [ explist1 ] `)' |
		               constructor |
		               STRING */
		private List<AST.Expression> ParseFuncArgs()
		{
			if (Stream.IsKeyword("("))
			{
				Stream.MoveNext();

				if (Stream.IsKeywordAndMove(")")) { return new List<AST.Expression>(); }

				List<AST.Expression> args = ParseExpressionList1();

				Stream.GetKeywordAndMove(")");

				return args;
			}
			else if (Stream.IsKeyword("{"))
			{
				List<AST.Expression> retList = new List<AST.Expression>();
				retList.Add(ParseConstructor());
				return retList;
			}
			else if (Stream.IsString())
			{
				List<AST.Expression> retList = new List<AST.Expression>();
				retList.Add(new AST.StringExpression(Stream.GetString().Value));
				Stream.MoveNext();
				return retList;
			}
			else
			{
				throw new ParserException();
			}
		}

		/* constructor -> '{' { confield [fieldsep] } [fieldsep] '}' */
		private AST.Expression ParseConstructor()
		{
			Stream.GetKeywordAndMove("{");

			List<AST.ConField> fields = new List<AST.ConField>();

			do
			{
				if (Stream.IsKeyword("}")) break;

				Lexer.Lexeme lookahead = Stream.Lookahead;
				if (Stream.IsKeyword("[") ||
					(Stream.IsName() && lookahead.LexemeType == Lexer.Lexeme.Type.KEYWORD && lookahead.Value == "="))
				{
					fields.Add(ParseRecField());
				}
				else
				{
					fields.Add(new AST.ListField(ParseExpression()));
				}
			} while (Stream.IsKeywordAndMove(",") || Stream.IsKeywordAndMove(";")); /* fieldsep = ';' | ',' */

			Stream.MoveNext(); // "}"

			return new AST.Constructor(fields);
		}

		/* (NAME | index) = exp1 */
		private AST.ConField ParseRecField()
		{
			if (Stream.IsName())
			{
				String name = Stream.GetName().Value;
				Stream.MoveNext();

				Stream.GetKeywordAndMove("=");

				return new AST.NameRecField(name, ParseExpression());
			}
			else if (Stream.IsKeyword("["))
			{
				Stream.MoveNext();

				AST.Expression expr = ParseExpression();

				Stream.GetKeywordAndMove("]");
				Stream.GetKeywordAndMove("=");

				return new AST.ExprRecField(expr, ParseExpression());
			}
			else
			{
				throw new ParserException();
			}
		}
	}
}
