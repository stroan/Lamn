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
			String input2 = "function foo(a,b) return a + b end return foo(1,2)";

			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(input2);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			VirtualMachine vm = new VirtualMachine();

			String fooId = Guid.NewGuid().ToString();
			String mainId = Guid.NewGuid().ToString();

			Compiler compiler = new Compiler();
			List<VirtualMachine.Function> compiledFunctions = compiler.CompileAST(outpu2);

			// Function foo
			UInt32[] bytecodesFoo = { VirtualMachine.OpCodes.MakePOPVARGS(1),
									  VirtualMachine.OpCodes.MakeGETUPVAL(0),
									  VirtualMachine.OpCodes.ADD,
									  VirtualMachine.OpCodes.MakeRET(1) };
			Object[] constantsFoo = { };

			vm.RegisterFunction(new VirtualMachine.Function(bytecodesFoo, constantsFoo, fooId));

			// Chunk body
			UInt32[] bytecodes = { VirtualMachine.OpCodes.MakeLOADK(0),
								   VirtualMachine.OpCodes.MakeCLOSEVAR(1),
								   VirtualMachine.OpCodes.MakeLOADK(1),
								   VirtualMachine.OpCodes.MakeCLOSURE(2),
								   VirtualMachine.OpCodes.MakePOPCLOSED(1),
								   VirtualMachine.OpCodes.MakePUTGLOBAL(3),
								   VirtualMachine.OpCodes.MakeGETGLOBAL(3),
								   VirtualMachine.OpCodes.MakeCALL(1),
								   VirtualMachine.OpCodes.MakePOPVARGS(1),
								   VirtualMachine.OpCodes.MakeRET(1) };
			Object[] constants = { 3.0, 2.0, fooId, "foo" };

			vm.RegisterFunction(new VirtualMachine.Function(bytecodes, constants, mainId));

			VirtualMachine.Closure closure = new VirtualMachine.Closure(mainId, new VirtualMachine.StackCell[0]);
			vm.PushStack(closure);
			vm.Call();
			vm.Run();

			return;
		}
	}
}
