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
			String input2 = "local function foo () local a, b end ; local a, b, c = 0x1, 2.5, 3e5; local c, d";

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input2);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			return;
		}
	}
}
