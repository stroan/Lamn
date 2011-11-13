using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lamn.Compiler;
using Lamn.VirtualMachine;

namespace Lamn
{
	public class LamnEngine
	{
		private State LamnState { get; set; }

		public LamnEngine()
		{
			LamnState = new State();
			CoreFunctions.RegisterCoreFunctions(LamnState);
		}

		public void SetGlobal(String name, State.NativeFuncDelegate userFunc)
		{
			LamnState.PutGlobal(name, userFunc);
		}

		public void Run(String source) 
		{
			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(source);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			Compiler.Compiler compiler = new Compiler.Compiler();
			VirtualMachine.Function compiledFunctions = compiler.CompileAST(outpu2);

			compiledFunctions.Print();

			VirtualMachine.Closure closure = new VirtualMachine.Closure(compiledFunctions, new VirtualMachine.StackCell[0]);
			LamnState.PushStack(closure);
			LamnState.Call();
			LamnState.Run();

			VirtualMachine.VarArgs returnValue = (VirtualMachine.VarArgs)LamnState.PopStack();
		}
	}
}
