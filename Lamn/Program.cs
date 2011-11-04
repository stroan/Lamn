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
			String input2 = System.IO.File.ReadAllText("../../../TestFiles/Test5.lua");

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input2);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			VirtualMachine vm = new VirtualMachine();
			vm.PutGlobal("print", new VirtualMachine.NativeFuncDelegate(printFunc));
			vm.PutGlobal("tonumber", new VirtualMachine.NativeFuncDelegate(tonumber));

			Compiler compiler = new Compiler();
			VirtualMachine.Function compiledFunctions = compiler.CompileAST(outpu2);

			compiledFunctions.Print();

			VirtualMachine.Closure closure = new VirtualMachine.Closure(compiledFunctions, new VirtualMachine.StackCell[0]);
			vm.PushStack(closure);
			vm.Call();
			vm.Run();

			VirtualMachine.VarArgs returnValue = (VirtualMachine.VarArgs)vm.PopStack();

			return;
		}

		static VirtualMachine.VarArgs printFunc(VirtualMachine.VarArgs input)
		{
			foreach (Object o in input.Args)
			{
				if (o is Double)
				{
					System.Console.Write((Double)o);
				}
				else if (o is String)
				{
					System.Console.Write((String)o);
				}
				else
				{
					System.Console.Write("[Unknown]");
				}
				System.Console.Write("\t");
			}

			System.Console.Write("\n");

			return new VirtualMachine.VarArgs();
		}

		static VirtualMachine.VarArgs tonumber(VirtualMachine.VarArgs input)
		{
			return input;
		}
	}
}
