using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn
{
	class Compiler
	{
		public class CompilerState
		{
			public List<UInt32> bytecodes = new List<UInt32>();
			public List<Object> constants = new List<Object>();
			public List<VirtualMachine.Function> childFunctions = new List<VirtualMachine.Function>();

			public int stackPosition = 0;			
			public Dictionary<String, int> stackVars = new Dictionary<String, int>();

			public int initialClosureStackPosition = 0;
			public int closureStackPosition = 0;
			public Dictionary<String, int> closedVars = new Dictionary<String, int>();
			public Dictionary<String, int> newClosedVars = new Dictionary<String, int>();

			public Dictionary<String, int> labels = new Dictionary<String, int>();
			public List<KeyValuePair<String, int>> jumps = new List<KeyValuePair<String, int>>();

			public String currentBreakLabel;

			public void ResolveJumps()
			{
				foreach (KeyValuePair<String, int> jump in jumps)
				{
					bytecodes[jump.Value] |= (VirtualMachine.OpCodes.OP1_MASK & ((UInt32)(labels[jump.Key]) << VirtualMachine.OpCodes.OP1_SHIFT));
				}

				jumps.Clear();
			}

			public String getNewLabel()
			{
				return Guid.NewGuid().ToString();
			}

			public int AddConstant(Object o)
			{
				if (!constants.Contains(o))
				{
					constants.Add(o);
				}

				return constants.IndexOf(o);
			}

			public SavedState SaveState()
			{
				int initialStackPosition = stackPosition;
				int blockInitialClosureStackPosition = closureStackPosition;

				Dictionary<String, int> oldClosedVars = newClosedVars.ToDictionary(entry => entry.Key,
																 entry => entry.Value);
				Dictionary<String, int> oldStackVars = stackVars.ToDictionary(entry => entry.Key,
															entry => entry.Value);

				return new SavedState
				{
					initialStackPosition = initialStackPosition,
					blockInitialClosureStackPosition = blockInitialClosureStackPosition,
					oldClosedVars = oldClosedVars,
					oldStackVars = oldStackVars
				};
			}

			public void RestoreState(SavedState s) {
				Cleanup(s);

				if (closureStackPosition > s.blockInitialClosureStackPosition)
				{
					closureStackPosition = s.blockInitialClosureStackPosition;
					newClosedVars = s.oldClosedVars;
				}

				if (stackPosition != s.initialStackPosition)
				{
					stackPosition = s.initialStackPosition;
					stackVars = s.oldStackVars;
				}
			}

			public void Cleanup(SavedState s)
			{
				if (closureStackPosition > s.blockInitialClosureStackPosition)
				{
					bytecodes.Add(VirtualMachine.OpCodes.MakePOPCLOSED(closureStackPosition - s.blockInitialClosureStackPosition));
				}

				if (stackPosition != s.initialStackPosition)
				{
					bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(stackPosition - s.initialStackPosition));
				}
			}
		}

		public class SavedState
		{
			public int initialStackPosition;
			public int blockInitialClosureStackPosition;

			public Dictionary<String, int> oldClosedVars;
			public Dictionary<String, int> oldStackVars;
		}

		public class ChunkCompiler : AST.StatementVisitor
		{
			private bool hasReturned = false;

			public CompilerState State { get; set; }

			SavedState savedState;

			public ChunkCompiler(AST.Chunk chunk, CompilerState state, SavedState save, bool returns)
			{
				State = state;

				savedState = save;
				if (save == null)
				{
					savedState = State.SaveState();
				}


				foreach (AST.Statement statement in chunk.Statements) {
					statement.Visit(this);
				}

				CleanupChunk();

				if (!hasReturned && returns)
				{
					state.bytecodes.Add(VirtualMachine.OpCodes.MakeRET(0));
				}
			}

			private void CleanupChunk()
			{
				State.RestoreState(savedState);
			}

			#region StatementVisitor Members

			public void Visit(AST.LocalAssignmentStatement statement)
			{
				int numVars = statement.Variables.Count;

				int initialStackPosition = State.stackPosition;
				CompileExpressionList(statement.Expressions, numVars);

				int initialClosureStackPosition = State.closureStackPosition;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(statement.Variables.Count));
				State.closureStackPosition += statement.Variables.Count;

				for (int i = 0; i < statement.Variables.Count; i++)
				{
					State.stackVars[statement.Variables[i]] = initialStackPosition + i;
					State.newClosedVars[statement.Variables[i]] = initialClosureStackPosition + i;
				}
			}

			public void Visit(AST.LocalFunctionStatement statement)
			{
				FunctionCompiler funcCompiler = new FunctionCompiler(State, statement.Body);

				VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.State.bytecodes.ToArray(), funcCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), funcCompiler.State.childFunctions);
				State.childFunctions.Add(function);

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(State.AddConstant(function.Id)));

				State.stackVars[statement.Name] = State.stackPosition;

				State.stackPosition++;
			}

			public void Visit(AST.IfStatement statement)
			{
				String afterLabel = State.getNewLabel();

				KeyValuePair<String, AST.TestBlock>[] branchLabels = new KeyValuePair<String, AST.TestBlock>[statement.Conditions.Count];
				for (int i = 0; i < branchLabels.Length; i++)
				{
					branchLabels[i] = new KeyValuePair<string, AST.TestBlock>(State.getNewLabel(), statement.Conditions[i]);
				}

				for (int i = 0; i < branchLabels.Length; i++)
				{
					AST.TestBlock branch = branchLabels[i].Value;
					String branchLabel = branchLabels[i].Key;
					String startLabel = State.getNewLabel();

					State.labels[branchLabel] = State.bytecodes.Count;
					branch.Cond.Visit(new ExpressionCompiler(State, 1));

					State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE);
					State.stackPosition--;
					State.jumps.Add(new KeyValuePair<string, int>(startLabel, State.bytecodes.Count - 1));

					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
					if (i < branchLabels.Length - 1)
					{
						State.jumps.Add(new KeyValuePair<string, int>(branchLabels[i + 1].Key, State.bytecodes.Count - 1));
					}
					else
					{
						State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count - 1));
					}

					State.labels[startLabel] = State.bytecodes.Count;
					ChunkCompiler chunk = new ChunkCompiler(branch.Block, State, null, false);

					if (i < branchLabels.Length - 1)
					{
						State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
						State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count - 1));
					}
				}

				State.labels[afterLabel] = State.bytecodes.Count;
			}

			public void Visit(AST.WhileStatement statement)
			{
				String afterLabel = State.getNewLabel();
				String condLabel = State.getNewLabel();
				String startLabel = State.getNewLabel();

				State.labels[condLabel] = State.bytecodes.Count;
				statement.Condition.Visit(new ExpressionCompiler(State, 1));

				State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE);
				State.stackPosition--;
				State.jumps.Add(new KeyValuePair<string, int>(startLabel, State.bytecodes.Count - 1));

				State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
				State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count - 1));

				String oldBreakLabel = State.currentBreakLabel;
				State.currentBreakLabel = afterLabel;

				State.labels[startLabel] = State.bytecodes.Count;
				ChunkCompiler chunk = new ChunkCompiler(statement.Block, State, null, false);

				State.currentBreakLabel = oldBreakLabel;

				State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
				State.jumps.Add(new KeyValuePair<string, int>(condLabel, State.bytecodes.Count - 1));

				State.labels[afterLabel] = State.bytecodes.Count;
			}

			public void Visit(AST.DoStatement statement)
			{
				new ChunkCompiler(statement.Block, State, null, false);
			}

			public void Visit(AST.ForStatement statement)
			{
				if (statement.Clause is AST.NumForClause)
				{
					AST.NumForClause clause = (AST.NumForClause)statement.Clause;
					
					AST.Expression exp1 = clause.Expr1;
					AST.Expression exp2 = clause.Expr2;
					AST.Expression exp3 = clause.Expr3;

					if (exp3 == null)
					{
						exp3 = exp2;
						exp2 = new AST.NumberExpression(1);
					}

					SavedState save = State.SaveState();

					// Initialize the variables for the loop.
					int valPosition = State.stackPosition;
					exp1.Visit(new ExpressionCompiler(State, 1));

					int stepPosition = State.stackPosition;
					exp2.Visit(new ExpressionCompiler(State, 1));

					int maxPosition = State.stackPosition;
					exp3.Visit(new ExpressionCompiler(State, 1));

					// Verify that the variables are all numbers
					foreach (int position in new int[] { valPosition, stepPosition, maxPosition })
					{
						State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETGLOBAL(State.AddConstant("tonumber"))); State.stackPosition++;
						State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - position)); State.stackPosition++;
						State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(1)); State.stackPosition--;
						State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(1, true));
					}

					String forCondLabel = State.getNewLabel();

					State.bytecodes.Add(VirtualMachine.OpCodes.AND); State.stackPosition--;
					State.bytecodes.Add(VirtualMachine.OpCodes.AND); State.stackPosition--;
					State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE); State.stackPosition--;
					State.jumps.Add(new KeyValuePair<string, int>(forCondLabel, State.bytecodes.Count - 1));

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETGLOBAL(State.AddConstant("error"))); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(0));
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(0, true)); State.stackPosition--;

					// Condition for the loop

					State.labels.Add(forCondLabel, State.bytecodes.Count);

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(0.0))); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - stepPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.LESS); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - valPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - maxPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.LESSEQ); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.AND); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - stepPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(0.0))); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.LESSEQ); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - maxPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - valPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.LESSEQ); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.AND); State.stackPosition--;

					State.bytecodes.Add(VirtualMachine.OpCodes.OR); State.stackPosition--;

					String bodyLabel = State.getNewLabel();
					String afterLabel = State.getNewLabel();

					State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE); State.stackPosition--;
					State.jumps.Add(new KeyValuePair<string, int>(bodyLabel, State.bytecodes.Count - 1));

					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
					State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count - 1));

					// Body

					SavedState loopSave = State.SaveState();

					String oldBreakLabel = State.currentBreakLabel;
					State.currentBreakLabel = afterLabel;

					State.labels.Add(bodyLabel, State.bytecodes.Count);

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - valPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(1));
					State.newClosedVars[clause.Name] = State.closureStackPosition; State.closureStackPosition++;
					State.stackVars[clause.Name] = State.stackPosition - 1;

					ChunkCompiler chunk = new ChunkCompiler(statement.Block, State, loopSave, false);

					State.currentBreakLabel = oldBreakLabel;

					State.RestoreState(loopSave);

					// Increment value

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - valPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - stepPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.ADD); State.stackPosition--;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - valPosition)); State.stackPosition--;
					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
					State.jumps.Add(new KeyValuePair<string, int>(forCondLabel, State.bytecodes.Count - 1));

					State.labels.Add(afterLabel, State.bytecodes.Count);

					State.RestoreState(save);
				}
				else
				{
					AST.ListForClause clause = (AST.ListForClause)statement.Clause;

					SavedState forSave = State.SaveState();

					int fPosition = State.stackPosition;
					int sPosition = State.stackPosition + 1;
					int vPosition = State.stackPosition + 2;
					CompileExpressionList(clause.Expressions, 3);

					String afterLabel = State.getNewLabel();
					String startLabel = State.getNewLabel();
					String bodyLabel = State.getNewLabel();
					State.labels.Add(startLabel, State.bytecodes.Count);

					SavedState loopSave = State.SaveState();

					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - fPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - sPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - vPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(2)); State.stackPosition -= 2;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(clause.Names.Count, true)); State.stackPosition--;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(clause.Names.Count));
					foreach (String name in clause.Names)
					{
						State.stackVars[name] = State.stackPosition;
						State.stackPosition++;

						State.newClosedVars[name] = State.closureStackPosition;
						State.closureStackPosition++;
					}
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(clause.Names.Count)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - vPosition)); State.stackPosition--;


					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - vPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE); State.stackPosition--;
					State.jumps.Add(new KeyValuePair<string, int>(bodyLabel, State.bytecodes.Count - 1));
					State.Cleanup(loopSave);
					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
					State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count - 1));

					State.labels[bodyLabel] = State.bytecodes.Count;
					String oldBreakLabel = State.currentBreakLabel;
					State.currentBreakLabel = afterLabel;
					ChunkCompiler chunk = new ChunkCompiler(statement.Block, State, loopSave, false);

					State.currentBreakLabel = oldBreakLabel;

					State.RestoreState(loopSave);

					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
					State.jumps.Add(new KeyValuePair<string, int>(startLabel, State.bytecodes.Count - 1));

					State.labels.Add(afterLabel, State.bytecodes.Count);
					State.RestoreState(forSave);
				}
			}

			public void Visit(AST.FunctionCallStatement statement)
			{
				statement.Expr.Visit(new ExpressionCompiler(State, 0));
				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(0, true));
				State.stackPosition--;
			}

			public void Visit(AST.AssignmentStatement statement)
			{
				int numVars = statement.Variables.Count;

				int startStackPosition = State.stackPosition;

				int[] leftStackPositions = new int[numVars];
				int[] rightStackPositions = new int[numVars];

				for (int i = 0; i < numVars; i++)
				{
					leftStackPositions[i] = State.stackPosition;
					statement.Variables[i].Visit(new LeftExpressionCompiler(State));
				}

				int initialStackPosition = State.stackPosition;
				for (int i = 0; i < statement.Expressions.Count; i++)
				{
					int numResults = 1;
					if (i == statement.Expressions.Count - 1)
					{
						numResults = numVars - i;
					}
					rightStackPositions[i] = State.stackPosition;
					statement.Expressions[i].Visit(new ExpressionCompiler(State, numResults));
				}

				int nowStackPosition = State.stackPosition;
				for (int i = 0; i < statement.Expressions.Count - (nowStackPosition - initialStackPosition); i++)
				{
					rightStackPositions[(nowStackPosition - initialStackPosition) + i] = State.stackPosition;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(null)));
				}

				for (int i = 0; i < numVars; i++)
				{
					statement.Variables[i].Visit(new AssignerExpressionCompiler(State, leftStackPositions[i], rightStackPositions[i]));
				}

				State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(State.stackPosition - startStackPosition)); State.stackPosition = startStackPosition;
			}

			public void Visit(AST.RepeatStatement statement)
			{
				String afterLabel = State.getNewLabel();
				String startLabel = State.getNewLabel();

				String oldBreakLabel = State.currentBreakLabel;
				State.currentBreakLabel = afterLabel;

				State.labels[startLabel] = State.bytecodes.Count;
				ChunkCompiler chunk = new ChunkCompiler(statement.Block, State, null, false);

				State.currentBreakLabel = oldBreakLabel;

				statement.Condition.Visit(new ExpressionCompiler(State, 1));

				State.bytecodes.Add(VirtualMachine.OpCodes.JMPTRUE);
				State.stackPosition--;
				State.jumps.Add(new KeyValuePair<string, int>(startLabel, State.bytecodes.Count - 1));

				State.labels[afterLabel] = State.bytecodes.Count;
			}

			public void Visit(AST.ReturnStatement statement)
			{
				foreach (AST.Expression expr in statement.Expressions)
				{
					ExpressionCompiler compiler = new ExpressionCompiler(State, 1);
					if (expr == statement.Expressions.Last())
					{
						compiler = new ExpressionCompiler(State, 0);
					}

					expr.Visit(compiler);
				}

				if (State.newClosedVars.Count > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPCLOSED(State.closureStackPosition - State.initialClosureStackPosition));
				}

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeRET(statement.Expressions.Count));

				hasReturned = true;
			}

			public void Visit(AST.BreakStatement statement)
			{
				CleanupChunk();

				State.bytecodes.Add(VirtualMachine.OpCodes.JMP);
				State.jumps.Add(new KeyValuePair<String, int>(State.currentBreakLabel, State.bytecodes.Count - 1));
			}

			public void Visit(AST.FunctionStatement statement)
			{
				FunctionCompiler funcCompiler = new FunctionCompiler(State, statement.Body);

				VirtualMachine.Function function = new VirtualMachine.Function(funcCompiler.State.bytecodes.ToArray(), funcCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), funcCompiler.State.childFunctions);
				State.childFunctions.Add(function);

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSURE(State.AddConstant(function.Id)));

				State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(State.AddConstant(statement.MainName)));
			}
			#endregion

			#region Helper Members

			private int GetConstantIndex(Object c)
			{
				return State.constants.IndexOf(c);
			}

			private void CompileExpressionList(List<AST.Expression> expressions, int numVars) {
				int initialStackPosition = State.stackPosition;
				for (int i = 0; i < expressions.Count; i++)
				{
					int numResults = 1;
					if (i == expressions.Count - 1)
					{
						numResults = numVars - i;
					}
					expressions[i].Visit(new ExpressionCompiler(State, numResults));
				}

				int nowStackPosition = State.stackPosition;
				for (int i = 0; i < numVars - (nowStackPosition - initialStackPosition); i++)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(null)));
					State.stackPosition++;
				}
			}
			#endregion
		}

		public class LeftExpressionCompiler : AST.ExpressionVisitor
		{
			public CompilerState State { get; private set; }
			public int NumResults { get; private set; }

			public LeftExpressionCompiler(CompilerState state)
			{
				State = state;
				NumResults = 1;
			}

			#region ExpressionVisitor Members

			public void Visit(AST.NameExpression expression)
			{
				return;
			}

			public void Visit(AST.NumberExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.StringExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.BoolExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.NilExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.VarArgsExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.UnOpExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.BinOpExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.LookupExpression expression)
			{
				expression.Obj.Visit(new ExpressionCompiler(State, 1));
			}

			public void Visit(AST.IndexExpression expression)
			{
				expression.Obj.Visit(new ExpressionCompiler(State, 1));
				expression.Index.Visit(new ExpressionCompiler(State, 1));
			}

			public void Visit(AST.FunctionApplicationExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.Constructor expression)
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		public class AssignerExpressionCompiler : AST.ExpressionVisitor
		{
			public CompilerState State { get; private set; }
			public int NumResults { get; private set; }

			int leftPosition;
			int rightPosition;

			public AssignerExpressionCompiler(CompilerState state, int left, int right)
			{
				State = state;
				NumResults = 1;
				leftPosition = left;
				rightPosition = right;
			}

			#region ExpressionVisitor Members

			public void Visit(AST.NameExpression expression)
			{
				if (State.stackVars.ContainsKey(expression.Value))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTSTACK(State.stackPosition - State.stackVars[expression.Value])); State.stackPosition--;
				}
				else if (State.closedVars.ContainsKey(expression.Value))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTUPVAL(State.initialClosureStackPosition - State.closedVars[expression.Value])); State.stackPosition--;
				}
				else
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePUTGLOBAL(State.AddConstant(expression.Value)));	State.stackPosition--;
				}
			}

			public void Visit(AST.NumberExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.StringExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.BoolExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.NilExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.VarArgsExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.UnOpExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.BinOpExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.FunctionExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.LookupExpression expression)
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - leftPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeLOADK(State.AddConstant(expression.Name))); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
			}

			public void Visit(AST.IndexExpression expression)
			{
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - leftPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - (leftPosition + 1))); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.MakeGETSTACK(State.stackPosition - rightPosition)); State.stackPosition++;
				State.bytecodes.Add(VirtualMachine.OpCodes.PUTTABLE); State.stackPosition -= 3;
			}

			public void Visit(AST.FunctionApplicationExpression expression)
			{
				throw new NotImplementedException();
			}

			public void Visit(AST.Constructor expression)
			{
				throw new NotImplementedException();
			}

			#endregion
		}

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

			public void Visit(AST.UnOpExpression expression)
			{
				expression.Expr.Visit(this);

				if (expression.Op.Equals("not"))
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.NOT);
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
					expression.LeftExpr.Visit(this);
					expression.RightExpr.Visit(this);
					State.bytecodes.Add(VirtualMachine.OpCodes.ADD);
				}
				else if (expression.Op.Equals("=="))
				{
					expression.LeftExpr.Visit(this);
					expression.RightExpr.Visit(this);
					State.bytecodes.Add(VirtualMachine.OpCodes.EQ);
				}
				else if (expression.Op.Equals("or"))
				{
					String afterLabel = State.getNewLabel();
					expression.LeftExpr.Visit(this);

					State.jumps.Add(new KeyValuePair<string,int>(afterLabel, State.bytecodes.Count));
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeJMPTRUEPreserve());
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(1)); State.stackPosition--;

					expression.RightExpr.Visit(this);
					State.labels.Add(afterLabel, State.bytecodes.Count);
				}
				else if (expression.Op.Equals("and"))
				{
					String afterLabel = State.getNewLabel();
					String nextLabel = State.getNewLabel();

					expression.LeftExpr.Visit(this);

					State.jumps.Add(new KeyValuePair<string,int>(nextLabel, State.bytecodes.Count));
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeJMPTRUEPreserve());
					State.jumps.Add(new KeyValuePair<string, int>(afterLabel, State.bytecodes.Count));
					State.bytecodes.Add(VirtualMachine.OpCodes.JMP);

					State.labels.Add(nextLabel, State.bytecodes.Count);
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPSTACK(1)); State.stackPosition--;
					expression.RightExpr.Visit(this);

					State.labels.Add(afterLabel, State.bytecodes.Count);
				}
				else
				{
					throw new NotImplementedException();
				}

				State.stackPosition--;
			}

			public void Visit(AST.FunctionExpression expression)
			{
				FunctionCompiler funcCompiler = new FunctionCompiler(State, expression.Body);

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

			public void Visit(AST.IndexExpression expression)
			{
				expression.Obj.Visit(new ExpressionCompiler(State, 1));
				expression.Index.Visit(new ExpressionCompiler(State, 1));
				State.bytecodes.Add(VirtualMachine.OpCodes.GETTABLE); State.stackPosition--;
			}

			public void Visit(AST.FunctionApplicationExpression expression)
			{
				int funcIndex = State.stackPosition;
				expression.Obj.Visit(new ExpressionCompiler(State, 1));

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

				State.bytecodes.Add(VirtualMachine.OpCodes.MakeCALL(expression.Args.Count));

				if (NumResults > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(NumResults, true));
					State.stackPosition = funcIndex + NumResults;
				}
				else {
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
						lField.Expr.Visit(this); // Stack position ++;
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

		public class FunctionCompiler
		{
			public CompilerState State { get; set; }
			public String Id { get; set; }

			public FunctionCompiler(CompilerState oldState, AST.Body body) {
				State = new CompilerState();

				State.initialClosureStackPosition = oldState.closureStackPosition;
				State.closureStackPosition = oldState.closureStackPosition;
				State.closedVars = oldState.closedVars;
				foreach (KeyValuePair<String, int> i in oldState.newClosedVars)
				{
					State.closedVars[i.Key] = i.Value;
				}

				State.stackPosition = 1;

				if (body.ParamList.NamedParams.Count > 0)
				{
					State.bytecodes.Add(VirtualMachine.OpCodes.MakePOPVARGS(body.ParamList.NamedParams.Count));
					State.bytecodes.Add(VirtualMachine.OpCodes.MakeCLOSEVAR(body.ParamList.NamedParams.Count));
					State.stackPosition += body.ParamList.NamedParams.Count;
				}

				int stackIndex = 1;
				foreach (String param in body.ParamList.NamedParams)
				{
					State.stackVars[param] = stackIndex;
					stackIndex++;

					State.newClosedVars[param] = State.closureStackPosition;
					State.closureStackPosition++;
				}

				if (body.ParamList.HasVarArgs)
				{
					State.stackVars["..."] = 0;
				}

				new ChunkCompiler(body.Chunk, State, null, true);
				State.ResolveJumps();

				Id = Guid.NewGuid().ToString();
			}
		}

		public VirtualMachine.Function CompileAST(AST ast)
		{
			ChunkCompiler chunkCompiler = new ChunkCompiler(ast.Contents, new CompilerState(), null, true);
			chunkCompiler.State.ResolveJumps();
			return new VirtualMachine.Function(chunkCompiler.State.bytecodes.ToArray(), chunkCompiler.State.constants.ToArray(), Guid.NewGuid().ToString(), chunkCompiler.State.childFunctions);
		}
	}
}
