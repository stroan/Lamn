using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class Program
	{
		static void Main(string[] args)
		{
			String input = "a = 1\nb = 2\nc = a + b";

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input);

			return;
		}
	}
}
