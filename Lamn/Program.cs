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
			String input2 = "local z, z1, z2 = 100, 200, 300 ; function foo(a, b) local x,y = a,a ; return a + b, y, z end ; return foo(1,2)";

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
