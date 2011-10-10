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
			String input2 = "function foo(a, b) x = a ; return a end";

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input2);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			VirtualMachine vm = new VirtualMachine();

			String fooId = Guid.NewGuid().ToString();
			String mainId = Guid.NewGuid().ToString();

			Compiler compiler = new Compiler();
			VirtualMachine.Function compiledFunctions = compiler.CompileAST(outpu2);

			compiledFunctions.Print();

			VirtualMachine.Closure closure = new VirtualMachine.Closure(compiledFunctions, new VirtualMachine.StackCell[0]);
			vm.PushStack(closure);
			vm.Call();
			vm.Run();

			return;
		}
	}
}
