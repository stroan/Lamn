using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.VirtualMachine
{
	class OpCodes
	{
		// Stack manipulation
		public const UInt32 LOADK     = 0x00000000;
		public const UInt32 GETSTACK  = 0x01000000;
		public const UInt32 PUTSTACK  = 0x02000000;
		public const UInt32 POPSTACK  = 0x03000000;
		public const UInt32 GETGLOBAL = 0x04000000;
		public const UInt32 PUTGLOBAL = 0x05000000;

		// Operators
		public const UInt32 ADD       = 0x10000000;
		public const UInt32 MINUS     = 0x11000000;
		public const UInt32 NEG       = 0x12000000;
		public const UInt32 MUL       = 0x13000000;
		public const UInt32 DIV       = 0x14000000;
		public const UInt32 POW       = 0x15000000;
		public const UInt32 CONCAT    = 0x16000000;

		// Calls and varargs
		public const UInt32 RET       = 0x20000000;
		public const UInt32 CALL      = 0x21000000;
		public const UInt32 JMP       = 0x22000000;
		public const UInt32 JMPTRUE   = 0x23000000;
		public const UInt32 POPVARGS  = 0x24000000;

		// Closures
		public const UInt32 CLOSEVARS = 0x30000000;
		public const UInt32 POPCLOSED = 0x31000000;
		public const UInt32 CLOSURE   = 0x32000000;
		public const UInt32 GETUPVAL  = 0x33000000;
		public const UInt32 PUTUPVAL  = 0x34000000;

		// Comparisons
		public const UInt32 EQ        = 0x40000000;
		public const UInt32 NOT       = 0x41000000;
		public const UInt32 AND       = 0x42000000;
		public const UInt32 OR        = 0x43000000;
		public const UInt32 LESSEQ    = 0x44000000;
		public const UInt32 LESS      = 0x45000000;

		// Tables
		public const UInt32 NEWTABLE  = 0x50000000;
		public const UInt32 PUTTABLE  = 0x51000000;
		public const UInt32 GETTABLE  = 0x52000000;

		// Masks
		public const UInt32 OPCODE_MASK = 0xFF000000;
		public const UInt32 OP1_MASK    = 0x00FFF000;
		public const UInt32 OP2_MASK    = 0x00000FFF;
		public const int OPCODE_SHIFT   = 24;
		public const int OP1_SHIFT      = 12;
		public const int OP2_SHIFT      = 0;

		public static UInt32 MakeLOADK(int index)
		{
			return LOADK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeCALL(int numArgs)
		{
			return CALL | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeCALL(int numArgs, int numToPop)
		{
			return CALL | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK) | (((UInt32)numToPop << OP2_SHIFT) & OP2_MASK);
		}

		public static UInt32 MakePOPVARGS(int numArgs)
		{
			return POPVARGS | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakePOPVARGS(int numArgs, bool remVarArgObj)
		{
			UInt32 retVal = POPVARGS | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
			if (remVarArgObj)
			{
				retVal = retVal | ((1 << OP2_SHIFT) & OP2_MASK);
			}
			return retVal;
		}

		public static UInt32 MakeRET(int numArgs)
		{
			return RET | (((UInt32)numArgs << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeCLOSEVAR(int numVars)
		{
			return CLOSEVARS | (((UInt32)numVars << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakePOPCLOSED(int numVars)
		{
			return POPCLOSED | (((UInt32)numVars << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeCLOSURE(int index)
		{
			return CLOSURE | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeGETUPVAL(int index)
		{
			return GETUPVAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakePUTUPVAL(int index)
		{
			return PUTUPVAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeGETGLOBAL(int index)
		{
			return GETGLOBAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakePUTGLOBAL(int index)
		{
			return PUTGLOBAL | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeGETSTACK(int index)
		{
			return GETSTACK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakePUTSTACK(int index)
		{
			return PUTSTACK | (((UInt32)index << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeJMP(int offset)
		{
			return JMP | (((UInt32)offset << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeJMPTRUE(int offset)
		{
			return JMPTRUE | (((UInt32)offset << OP1_SHIFT) & OP1_MASK);
		}

		public static UInt32 MakeJMPTRUEPreserve()
		{
			return JMPTRUE | (((UInt32)1 << OP2_SHIFT) & OP2_MASK);
		}

		public static UInt32 MakePOPSTACK(int count)
		{
			return POPSTACK | (((UInt32)count << OP1_SHIFT) & OP1_MASK);
		}
	}
}
