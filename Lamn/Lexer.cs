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
				NAME
			}

			public Type LexemeType { get; private set; }
			public String Value { get; private set; }

			public Lexeme(String value, Type type)
			{
				LexemeType = type;
				Value = value;
			}
		}

		public class LexException : Exception
		{
		}

		private class Rule
		{
			private Regex regex;
			private Lexeme.Type type;

			public Rule(Regex regex, Lexeme.Type type)
			{
				this.regex = regex;
				this.type = type;
			}

			public Lexeme Match(StringSource source)
			{
				Match match = source.TryExtract(regex);
				if (!match.Success) { return null; }

				return new Lexeme(match.Value, type);
			}
		}

		private class StringSource
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

		Rule[] rules = { new Rule(new Regex("\\G\\s+"),                   Lexeme.Type.WHITESPACE),
		                 new Rule(new Regex("\\G(and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while)"), Lexeme.Type.KEYWORD),
		                 new Rule(new Regex("\\G(\\+|-|\\*|\\/|%|\\^|\\#|==|~=|<=|>=|<|>|=|\\(|\\)|\\{|\\}|\\[|\\]|;|:|,)"), Lexeme.Type.KEYWORD),
		                 new Rule(new Regex("\\G(\\.\\.\\.)"),            Lexeme.Type.KEYWORD),
		                 new Rule(new Regex("\\G(\\.\\.)"),               Lexeme.Type.KEYWORD),
		                 new Rule(new Regex("\\G(\\.)"),                  Lexeme.Type.KEYWORD),
		                 new Rule(new Regex("\\G\\d+(\\.\\d+)?"),         Lexeme.Type.NUMBER), 
		                 new Rule(new Regex("\\G\"([^\"\\\\]|\\.)*\""),   Lexeme.Type.STRING),
		                 new Rule(new Regex("\\G[_A-Za-z][_A-Za-z0-9]*"), Lexeme.Type.NAME) };

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
