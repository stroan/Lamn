using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lamn.Compiler
{
	public class Lexer
	{
		public class Position
		{
			public int LineNumber { get; private set; }
			public int ColumnNumber { get; private set; }

			public Position(int line, int col)
			{
				LineNumber = line;
				ColumnNumber = col;
			}
		}
		public class Lexeme
		{
			public enum Type
			{
				NUMBER,
				WHITESPACE,
				KEYWORD,
				STRING,
				NAME,
				COMMENT,
				EOS
			}

			public Type LexemeType { get; private set; }
			public String Value { get; private set; }
			public double? NumberValue { get; private set; }

			public Position FilePosition { get; private set; }

			public Lexeme(String value, Type type, Position pos)
			{
				LexemeType = type;
				Value = value;
				FilePosition = pos;
			}

			public Lexeme(String value, double numberValue, Position pos)
				: this(value, Type.NUMBER, pos)
			{
				NumberValue = numberValue;
			}
		}

		public class LexException : Exception
		{
		}

		public class Rule
		{
			public delegate Lexeme Producer(String value, Position pos);
	
			private Regex regex;
			private Producer producer;

			public Rule(String regex, Producer producer)
			{
				this.producer = producer;
				this.regex = new Regex("\\G" + regex);
			}

			public Rule(String regex, Lexeme.Type type) : this(regex, DefaultProducer(type)) { }

			public Lexeme Match(StringSource source)
			{
				Position pos = source.SourcePos;
				Match match = source.TryExtract(regex);
				if (!match.Success) { return null; }

				return producer(match.Value, pos);
			}

			private static Producer DefaultProducer(Lexeme.Type type) 
			{
				return (value, pos) => new Lexeme(value, type, pos);
			}

			#region String producer
			public static Rule.Producer QuoteStringProducer { get { return new Rule.Producer(AcceptQuoteStringLexeme); } }
			public static Rule.Producer LongStringProducer { get { return new Rule.Producer(AcceptLongStringLexeme); } }

			private static Lexeme AcceptQuoteStringLexeme(String value, Position pos)
			{
				return new Lexeme(Unescaper.unescapeString(value.Substring(1,value.Length - 2)), Lexeme.Type.STRING, pos);
			}

			private static Lexeme AcceptLongStringLexeme(String value, Position pos)
			{
				int startIndex = value.IndexOf('[', 1) + 1;

				value = value.Replace("\r\n", "\n");
				value = value.Replace("\n\r", "\n");
				value = value.Replace("\r", "\n");

				int length = value.Length - (startIndex * 2);

				// Discard leading new lines.
				if (value[startIndex] == '\n')
				{
					startIndex++;
					length--;
				}

				return new Lexeme(value.Substring(startIndex, length), Lexeme.Type.STRING, pos);
			}
			#endregion

			#region Number producer
			public static Rule.Producer HexNumberProducer { get { return new Rule.Producer(AcceptHexNumberLexeme); } }
			public static Rule.Producer DecimalNumberProducer { get { return new Rule.Producer(AcceptDecimalNumberLexeme); } }

			private static Lexeme AcceptHexNumberLexeme(String value, Position pos)
			{
				return new Lexeme(value, (double)long.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber), pos);
			}

			private static Lexeme AcceptDecimalNumberLexeme(String value, Position pos)
			{
				return new Lexeme(value, Double.Parse(value, System.Globalization.NumberStyles.Float), pos);
			}
			#endregion

			#region Identifier producer

			public static Rule.Producer IdentifierProducer { get { return new Rule.Producer(AcceptIdentifier); } }

			private static Lexeme AcceptIdentifier(String value, Position pos)
			{
				Regex keywordRegex = new Regex("^(and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while)$");
				Match keywordMatch = keywordRegex.Match(value);
				return new Lexeme(value, keywordMatch.Success ? Lexeme.Type.KEYWORD : Lexeme.Type.NAME, pos);
			}

			#endregion
		}

		public class StringSource
		{
			private String RawString { get; set; }
			private int StartPos { get; set; }

			private int LineNumber { get; set; }
			private int ColumnNumber { get; set; }

			public Position SourcePos { get {
				return new Position(LineNumber, ColumnNumber);
			} }

			public bool EOF { get { return StartPos >= RawString.Length; } }

			public StringSource(String source)
			{
				RawString = source;
				StartPos = 0;
				LineNumber = 0;
				ColumnNumber = 0;
			}

			public Match TryExtract(Regex regex)
			{
				Match match = regex.Match(RawString, StartPos);

				if (match.Success)
				{
					StartPos += match.Value.Length;

					String value = match.Value;
					foreach (Char c in value)
					{
						if (c == '\n')
						{
							LineNumber++;
							ColumnNumber = 0;
						}
						else
						{
							ColumnNumber++;
						}
					}
				}

				return match;
			}
		}

		Rule[] rules = { new Rule("\\s+",                                          Lexeme.Type.WHITESPACE),
		                 new Rule("--\\[(?<depth>=*)\\[(.|\\n)*?\\]\\k<depth>\\]",  Lexeme.Type.COMMENT),    // Multiline comment
		                 new Rule("--.*",                                          Lexeme.Type.COMMENT),    // Short comment
		                 new Rule("\\[(?<depth>=*)\\[(.|\\n)*?\\]\\k<depth>\\]",    Rule.LongStringProducer),    // Multline string
		                 new Rule("\"([^\\n\"\\\\]|\\\\(.|\\n))*\"",               Rule.QuoteStringProducer),    // String with "s
		                 new Rule("'([^\\n'\\\\]|\\\\(.|\\n)|\\n)*'",                  Rule.QuoteStringProducer),    // String with 's
		                 new Rule("(\\+|-|\\*|\\/|%|\\^|\\#|==|~=|<=|>=|<|>|=|\\(|\\)|\\{|\\}|\\[|\\]|;|:|,)", Lexeme.Type.KEYWORD),
		                 new Rule("(\\.\\.\\.)",                                   Lexeme.Type.KEYWORD),
		                 new Rule("(\\.\\.)",                                      Lexeme.Type.KEYWORD),
		                 new Rule("(\\.)",                                         Lexeme.Type.KEYWORD),
		                 new Rule("0x[0-9a-fA-F]+",                                Rule.HexNumberProducer),     // Hex number
		                 new Rule("\\d+(\\.\\d+)?([eE](-)?\\d+(\\.\\d+)?)?",       Rule.DecimalNumberProducer), // Decimal number
		                 new Rule("[_A-Za-z][_A-Za-z0-9]*",                        Rule.IdentifierProducer) };

		public List<Lexeme> lex(String input)
		{
			List<Lexeme> output = new List<Lexeme>();
			StringSource source = new StringSource(input);

			while (!source.EOF)
			{
				bool validLex = false;
				foreach (Rule rule in rules)
				{
					Lexeme lexeme = rule.Match(source);
					if (lexeme != null)
					{
						output.Add(lexeme);
						validLex = true;
						break;
					}
				}

				if (!validLex)
				{
					throw new LexException();
				}
			}

			return output;
		}
	}
}
