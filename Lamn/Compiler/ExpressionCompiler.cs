using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	public class ExpressionCompiler : AST.ExpressionVisitor
	{
		public CompilerState State { get; private set; }
		public int NumResults { get; private set; }

		public ExpressionCompiler(CompilerState state)
		{
			State = state;
			NumResults = 1;
		}

		public ExpressionCompiler(CompilerState state, int numResults)
		{
			State = state;
			NumResults = numResults;
		}

		#region ExpressionVisitor Members

		public void Visit(AST.NameExpression expression)
		{
			if (State.stackVars.ContainsKey(expression.Value))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - State.stackVars[expression.Value]));
				State.stackPosition++;
			}
			else if (State.closedVars.ContainsKey(expression.Value))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETUPVAL(State.initialClosureStackPosition - State.closedVars[expression.Value]));
				State.stackPosition++;
			}
			else
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETGLOBAL(State.AddConstant(expression.Value)));
				State.stackPosition++;
			}
		}

		public void Visit(AST.NumberExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Value)));
			State.stackPosition++;
		}

		public void Visit(AST.StringExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Value)));
			State.stackPosition++;
		}

		public void Visit(AST.BoolExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Value)));
			State.stackPosition++;
		}

		public void Visit(AST.NilExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(null)));
			State.stackPosition++;
		}

		public void Visit(AST.VarArgsExpression expression)
		{
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - State.stackVars["..."]));
			State.stackPosition++;
		}

		public void Visit(AST.ParenExpression expression)
		{
			expression.Expr.Visit(new ExpressionCompiler(State, 1));
		}

		public void Visit(AST.UnOpExpression expression)
		{
			expression.Expr.Visit(this);

			if (expression.Op.Equals("not"))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.NOT);
			}
			else if (expression.Op.Equals("-"))
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.NEG);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void Visit(AST.BinOpExpression expression)
		{
			if (expression.Op.Equals("+"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.ADD);
			}
			else if (expression.Op.Equals("=="))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.EQ);
			}
			else if (expression.Op.Equals("or"))
			{
				String afterLabel = State.getNewLabel();
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));

				State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count));
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeJMPTRUEPreserve());
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(1)); State.stackPosition--;

				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.labels.Add(afterLabel, State.bytecodes.Count);
				State.stackPosition++;
			}
			else if (expression.Op.Equals("and"))
			{
				String afterLabel = State.getNewLabel();
				String nextLabel = State.getNewLabel();

				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));

				State.jumps.Add(new KeyValuePair<string, int>(nextLabel, State.bytecodes.Count));
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeJMPTRUEPreserve());
				State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count));
				State.bytecodes.Add(VirtualMachine.OpCodes.JMP);

				State.labels.Add(nextLabel, State.bytecodes.Count);
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(1)); State.stackPosition--;
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.labels.Add(afterLabel, State.bytecodes.Count);
				State.stackPosition++;
			}
			else if (expression.Op.Equals(".."))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.CONCAT);
			}
			else if (expression.Op.Equals("^"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.POW);
			}
			else if (expression.Op.Equals("*"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.MUL);
			}
			else if (expression.Op.Equals("/"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.DIV);
			}
			else if (expression.Op.Equals("-"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.MINUS);
			}
			else if (expression.Op.Equals("<"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.LESS);
			}
			else if (expression.Op.Equals("<="))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.LESSEQ);
			}
			else if (expression.Op.Equals(">="))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.LESS);
				State.bytecodes.Add(VirtualMachine.OpCodes.NOT);
			}
			else if (expression.Op.Equals(">"))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.LESSEQ);
				State.bytecodes.Add(VirtualMachine.OpCodes.NOT);
			}
			else if (expression.Op.Equals("~="))
			{
				expression.LeftExpr.Visit(new ExpressionCompiler(State, 1));
				expression.RightExpr.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.EQ);
				State.bytecodes.Add(VirtualMachine.OpCodes.NOT);
			}
			else
			{
				throw new NotImplementedException();
			}

			State.stackPosition--;
		}

		public void Visit(AST.FunctionExpression expression)
		{
			FunctionCompiler funcCompiler = new FunctionCompiler(State, expression.Body, false);

			VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.State.bytecodes.ToArray(), funcCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), funcCompiler.State.childFunctions);
			State.childFunctions.Add(function);

			State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(State.AddConstant(function.Id)));

			State.stackPosition++;
		}

		public void Visit(AST.LookupExpression expression)
		{
			expression.Obj.Visit(new ExpressionCompiler(State, 1));
			State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Name))); State.stackPosition++;
			State.bytecodes.Add(VirtualMachine.OpCodes.GETTABLE); State.stackPosition--;
		}

		public void Visit(AST.SelfLookupExpression expression)
		{
			throw new NotImplementedException();
		}

		public void Visit(AST.IndexExpression expression)
		{
			expression.Obj.Visit(new ExpressionCompiler(State, 1));
			expression.Index.Visit(new ExpressionCompiler(State, 1));
			State.bytecodes.Add(VirtualMachine.OpCodes.GETTABLE); State.stackPosition--;
		}

		public void Visit(AST.FunctionApplicationExpression expression)
		{
			int funcIndex = State.stackPosition;

			if (expression.Obj is AST.SelfLookupExpression)
			{
				AST.SelfLookupExpression lookup = (AST.SelfLookupExpression)expression.Obj;
				int selfPos = State.stackPosition;
				lookup.Obj.Visit(new ExpressionCompiler(State, 1));
				int funcPos = State.stackPosition;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - selfPos)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(lookup.Name))); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.GETTABLE); State.stackPosition--;

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - selfPos)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - funcPos)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - selfPos)); State.stackPosition--;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - funcPos)); State.stackPosition--;
			}
			else
			{
				expression.Obj.Visit(new ExpressionCompiler(State, 1));
			}

			foreach (AST.Expression expr in expression.Args)
			{
				ExpressionCompiler compiler = new ExpressionCompiler(State, 1);
				if (expr == expression.Args.Last())
				{
					compiler = new ExpressionCompiler(State, 0);
				}

				expr.Visit(compiler);
			}

			int currentIndex = State.stackPosition;

			int argCount = expression.Args.Count;
			if (expression.Obj is AST.SelfLookupExpression)
			{
				argCount++;
			}

			State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(argCount));

			if (NumResults > 0)
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(NumResults, true));
				State.stackPosition = funcIndex + NumResults;
			}
			else
			{
				State.stackPosition = funcIndex + 1;
			}
		}

		public void Visit(AST.Constructor expression)
		{
			int tablePosition = State.stackPosition;
			State.bytecodes.Add(VirtualMachine.OpCodes.NEWTABLE); State.stackPosition++;

			double listIndex = 1;

			foreach (AST.ConField field in expression.Fields)
			{
				if (field is AST.NameRecField)
				{
					AST.NameRecField nField = (AST.NameRecField)field;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - tablePosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(nField.Name))); State.stackPosition++;
					nField.Value.Visit(this); // Stack position ++;
					State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
				}
				else if (field is AST.ListField)
				{
					AST.ListField lField = (AST.ListField)field;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - tablePosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(listIndex++))); State.stackPosition++;
					lField.Expr.Visit(new ExpressionCompiler(State, field == expression.Fields.Last() ? 0 : 1)); // Stack position ++;
					State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
				}
				else if (field is AST.ExprRecField)
				{
					AST.ExprRecField eField = (AST.ExprRecField)field;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - tablePosition)); State.stackPosition++;
					eField.IndexExpr.Visit(this); // Stack position ++;
					eField.Value.Visit(this); // Stack position ++;
					State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
				}
			}
		}

		#endregion
	}
}
