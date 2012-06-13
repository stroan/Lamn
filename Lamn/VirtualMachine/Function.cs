using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	public class Function
	{
		public UInt32[] Bytecodes { get; private set; }
		public Object[] Constants { get; private set; }
		public String Id { get; private set; }
		
		public String Name { get; private set; }
		public Dictionary<int, Lamn.Compiler.Lexer.Position> Positions { get; private set; }

		public Dictionary<String, Function> ChildFunctions { get; private set; }

		public Function(UInt32[] bytecodes, Object[] constants, String id, List<Function> children,
		                Dictionary<int, Lamn.Compiler.Lexer.Position> positions, String name)
		{
			Bytecodes = bytecodes;
			Constants = constants;
			Id = id;
			Positions = positions;
			Name = name;

			ChildFunctions = new Dictionary<string, Function>();
			foreach (Function child in children)
			{
				ChildFunctions[child.Id] = child;
			}
		}

		public static void Print(Function f, System.IO.TextWriter writer)
		{
			writer.WriteLine("Function " + f.Id + "(" + f.Name + ")");
			for (int i = 0; i < f.Bytecodes.Length; i++)
			{
				UInt32 instruction = f.Bytecodes[i];

				String name;
				UInt32 opCode = instruction & OpCodes.OPCODE_MASK;
				switch (opCode)
				{
					case OpCodes.LOADK:
						name = "LOADK";
						break;
					case OpCodes.ADD:
						name = "ADD";
						break;
					case OpCodes.MINUS:
						name = "MINUS";
						break;
					case OpCodes.MUL:
						name = "MUL";
						break;
					case OpCodes.DIV:
						name = "MUL";
						break;
					case OpCodes.NEG:
						name = "NEG";
						break;
					case OpCodes.POW:
						name = "POW";
						break;
					case OpCodes.RET:
						name = "RET";
						break;
					case OpCodes.CALL:
						name = "CALL";
						break;
					case OpCodes.POPVARGS:
						name = "POPVARGS";
						break;
					case OpCodes.CLOSEVARS:
						name = "CLOSEVARS";
						break;
					case OpCodes.POPCLOSED:
						name = "POPCLOSED";
						break;
					case OpCodes.CLOSURE:
						name = "CLOSURE";
						break;
					case OpCodes.GETUPVAL:
						name = "GETUPVAL";
						break;
					case OpCodes.PUTUPVAL:
						name = "PUTUPVAL";
						break;
					case OpCodes.GETGLOBAL:
						name = "GETGLOBAL";
						break;
					case OpCodes.PUTGLOBAL:
						name = "PUTGLOBAL";
						break;
					case OpCodes.GETSTACK:
						name = "GETSTACK";
						break;
					case OpCodes.PUTSTACK:
						name = "PUTSTACK";
						break;
					case OpCodes.JMP:
						name = "JMP";
						break;
					case OpCodes.JMPTRUE:
						name = "JMPTRUE";
						break;
					case OpCodes.POPSTACK:
						name = "POPSTACK";
						break;
					case OpCodes.EQ:
						name = "EQ";
						break;
					case OpCodes.NOT:
						name = "NOT";
						break;
					case OpCodes.AND:
						name = "AND";
						break;
					case OpCodes.OR:
						name = "OR";
						break;
					case OpCodes.CONCAT:
						name = "CONCAT";
						break;
					case OpCodes.LESSEQ:
						name = "LESSEQ";
						break;
					case OpCodes.LESS:
						name = "LESS";
						break;
					case OpCodes.NEWTABLE:
						name = "NEWTABLE";
						break;
					case OpCodes.PUTTABLE:
						name = "PUTTABLE";
						break;
					case OpCodes.GETTABLE:
						name = "GETTABLE";
						break;
					default:
						throw new VMException();
				}

				UInt32 op1 = (instruction & OpCodes.OP1_MASK) >> OpCodes.OP1_SHIFT;
				UInt32 op2 = (instruction & OpCodes.OP2_MASK) >> OpCodes.OP2_SHIFT;
				
				
				writer.Write(String.Format("0x{0:x4} {1,10} {2:x} {3:x}", new Object[] { i, name, op1, op2 }));
				if (f.Positions.ContainsKey(i)) 
				{
					writer.Write ("\tL:" + f.Positions[i].LineNumber + 
					              "  C:" + f.Positions[i].ColumnNumber);					
				}
				writer.WriteLine();
			}
			writer.WriteLine("");

			writer.WriteLine(String.Format("Constants: {0:d}", f.Constants.Length));
			for (int i = 0; i < f.Constants.Length; i++)
			{
				String constantStr = "null";
				if (f.Constants[i] != null)
				{
					constantStr = f.Constants[i].ToString();
				}
				writer.WriteLine(String.Format("{0:d}: {1,-5}", i, constantStr));
			}

			writer.WriteLine("");
			writer.WriteLine("");

			foreach (Function child in f.ChildFunctions.Values)
			{
				Print(child, writer);
			}
		}
	}
}
