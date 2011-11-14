using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class Compiler
	{
		public VirtualMachine.Function CompileAST(AST ast)
		{
			ChunkCompiler chunkCompiler = new ChunkCompiler(ast.Contents, new CompilerState(), null, true);
			chunkCompiler.State.ResolveJumps();
			return new VirtualMachine.Function(chunkCompiler.State.bytecodes.ToArray(), chunkCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), chunkCompiler.State.childFunctions);
		}
	}
}
