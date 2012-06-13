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
		public State LamnState { get; private set; }

		public System.IO.TextWriter OutputStream { get { return LamnState.OutStream; } set { LamnState.OutStream = value; } }

		public LamnEngine()
		{
			LamnState = new State(this);
			CoreFunctions.RegisterCoreFunctions(LamnState);
		}

		public void SetGlobal(String name, State.NativeFuncDelegate userFunc)
		{
			LamnState.PutGlobal(name, userFunc);
		}

		public VirtualMachine.Closure CompileString(String source)
		{
			Lexer lexer = new Lexer();
			List<Lexer.Lexeme> output = lexer.lex(source);

			Parser parser = new Parser(output);
			AST outpu2 = parser.Parse();

			Compiler.Compiler compiler = new Compiler.Compiler();
			VirtualMachine.Function compiledFunctions = compiler.CompileAST(outpu2);

			//Function.Print(compiledFunctions, LamnState.OutStream);

			return new VirtualMachine.Closure(compiledFunctions, new VirtualMachine.StackCell[0]);
		}

		public void Run(String source) 
		{
			VirtualMachine.Closure closure = CompileString(source);
			LamnState.CurrentThread.PushStack(closure);
			LamnState.Call();
			LamnState.Run();

			VirtualMachine.VarArgs returnValue = (VirtualMachine.VarArgs)LamnState.CurrentThread.PopStack();
		}
	}
}
