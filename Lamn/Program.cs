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
			String input2 = "function foo(...) local a = 1 return end foo, bar = 1,2,3";

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input2);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			VirtualMachine vm = new VirtualMachine();

			// Function foo
			UInt32[] bytecodesFoo = { VirtualMachine.OpCodes.MakePOPVARGS(2),
									  VirtualMachine.OpCodes.ADD,
									  VirtualMachine.OpCodes.MakeRET(1) };
			Object[] constantsFoo = { };

			int fooIndex = vm.RegisterFunction(bytecodesFoo, constantsFoo);

			// Chunk body
			UInt32[] bytecodes = { VirtualMachine.OpCodes.MakeLOADK(0),
								   VirtualMachine.OpCodes.MakeLOADK(1),
								   VirtualMachine.OpCodes.MakeLOADK(2),
								   VirtualMachine.OpCodes.MakeCALL(2),
								   VirtualMachine.OpCodes.MakePOPVARGS(1),
								   VirtualMachine.OpCodes.MakeRET(1) };
			Object[] constants = { 3.0, 2.0, new VirtualMachine.Closure(fooIndex) };

			int functionIndex = vm.RegisterFunction(bytecodes, constants);

			VirtualMachine.Closure closure = new VirtualMachine.Closure(functionIndex);
			vm.PushStack(closure);
			vm.Call();
			vm.Run();

			return;
		}
	}
}
