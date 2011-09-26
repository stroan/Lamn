using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lamn
{
	class Lexer
	{
		public class Lexeme
		{
			public enum Type
			{
				NUMBER,
				WHITESPACE,
				KEYWORD,
				STRING,
				NAME,
				COMMENT
			}

			public Type LexemeType { get; private set; }
			public String Value { get; private set; }
			public double? NumberValue { get; private set; }

			public Lexeme(String value, Type type)
			{
				LexemeType = type;
				Value = value;
			}

			public Lexeme(String value, double numberValue) : this(value, Type.NUMBER)
			{
				NumberValue = numberValue;
			}
		}

		public class LexException : Exception
		{
		}

		public class Rule
		{
			public delegate Lexeme Producer(String value);
	
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
				Match match = source.TryExtract(regex);
				if (!match.Success) { return null; }

				return producer(match.Value);
			}

			private static Producer DefaultProducer(Lexeme.Type type) 
			{
				return (value) => new Lexeme(value, type);
			}

			#region String producer
			public static Rule.Producer DoubleStringProducer { get { return new Rule.Producer(AcceptDoubleStringLexeme); } }
			public static Rule.Producer SingleStringProducer { get { return new Rule.Producer(AcceptSingleStringLexeme); } }
			public static Rule.Producer LongStringProducer { get { return new Rule.Producer(AcceptLongStringLexeme); } }

			private static Lexeme AcceptDoubleStringLexeme(String value)
			{
				Regex doubleQuoteString = new Regex("^\"(?<contents>([^\\n\"\\\\]|\\\\.)*)\"$");

				return new Lexeme(Regex.Unescape(doubleQuoteString.Match(value).Groups["contents"].Value), Lexeme.Type.STRING);
			}

			private static Lexeme AcceptSingleStringLexeme(String value)
			{
				Regex singleQuoteString = new Regex("^'(?<contents>([^\\n'\\\\]|\\\\.)*)'$");

				return new Lexeme(Regex.Unescape(singleQuoteString.Match(value).Groups["contents"].Value), Lexeme.Type.STRING);
			}

			private static Lexeme AcceptLongStringLexeme(String value)
			{
				Regex longString = new Regex("^\\[(?<depth>=*)\\[(?<contents>(.|\\n)*)\\]\\k<depth>\\]$");

				return new Lexeme(longString.Match(value).Groups["contents"].Value, Lexeme.Type.STRING);
			}
			#endregion

			#region Number producer
			public static Rule.Producer HexNumberProducer { get { return new Rule.Producer(AcceptHexNumberLexeme); } }
			public static Rule.Producer DecimalNumberProducer { get { return new Rule.Producer(AcceptDecimalNumberLexeme); } }

			private static Lexeme AcceptHexNumberLexeme(String value)
			{
				return new Lexeme(value, (double)long.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber));
			}

			private static Lexeme AcceptDecimalNumberLexeme(String value)
			{
				return new Lexeme(value, Double.Parse(value, System.Globalization.NumberStyles.Float));
			}
			#endregion
		}

		public class StringSource
		{
			private String RawString { get; set; }
			private int StartPos { get; set; }

			public bool EOF { get { return StartPos >= RawString.Length; } }

			public StringSource(String source)
			{
				RawString = source;
				StartPos = 0;
			}

			public Match TryExtract(Regex regex)
			{
				Match match = regex.Match(RawString, StartPos);

				if (match.Success)
				{
					StartPos += match.Value.Length;
				}

				return match;
			}
		}

		Rule[] rules = { new Rule("\\s+",                                          Lexeme.Type.WHITESPACE),
		                 new Rule("--\\[(?<depth>=*)\\[(.|\\n)*\\]\\k<depth>\\]",  Lexeme.Type.COMMENT),    // Multiline comment
		                 new Rule("--.*",                                          Lexeme.Type.COMMENT),    // Short comment
		                 new Rule("\\[(?<depth>=*)\\[(.|\\n)*\\]\\k<depth>\\]",    Rule.LongStringProducer),    // Multline string
		                 new Rule("\"([^\\n\"\\\\]|\\\\.)*\"",                     Rule.DoubleStringProducer),    // String with "s
		                 new Rule("'([^\\n'\\\\]|\\\\.)*'",                        Rule.SingleStringProducer),    // String with 's
		                 new Rule("(and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while)", Lexeme.Type.KEYWORD),
		                 new Rule("(\\+|-|\\*|\\/|%|\\^|\\#|==|~=|<=|>=|<|>|=|\\(|\\)|\\{|\\}|\\[|\\]|;|:|,)", Lexeme.Type.KEYWORD),
		                 new Rule("(\\.\\.\\.)",                                   Lexeme.Type.KEYWORD),
		                 new Rule("(\\.\\.)",                                      Lexeme.Type.KEYWORD),
		                 new Rule("(\\.)",                                         Lexeme.Type.KEYWORD),
		                 new Rule("0x[0-9a-fA-F]+",                                Rule.HexNumberProducer),     // Hex number
		                 new Rule("\\d+(\\.\\d+)?([eE](-)?\\d+(\\.\\d+)?)?",       Rule.DecimalNumberProducer), // Decimal number
		                 new Rule("[_A-Za-z][_A-Za-z0-9]*",                        Lexeme.Type.NAME) };

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
