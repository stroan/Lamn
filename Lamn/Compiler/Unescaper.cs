using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class Unescaper
	{
		public static String unescapeString(String s)
		{
			String retValue = "";

			int i = 0;
			while (i < s.Length)
			{
				char c = s[i];
				i++;
				if (c != '\\' || i == s.Length)
				{
					retValue += c;
				}
				else
				{
					char nc = s[i];
					i++;
					switch (nc)
					{
						case 'a':
							retValue += "\a";
							break;
						case 'b':
							retValue += "\b";
							break;
						case 'f':
							retValue += "\f";
							break;
						case 'n':
							retValue += "\n";
							break;
						case 'r':
							retValue += "\r";
							break;
						case 't':
							retValue += "\t";
							break;
						case 'v':
							retValue += "\v";
							break;
						case 'x':
							if (i < s.Length - 2 && isHexChar(s[i]) && isHexChar(s[i + 1]))
							{
								retValue += (char)UInt16.Parse(s[i].ToString() + s[i + 1].ToString(), System.Globalization.NumberStyles.HexNumber);
								i += 2;
							}
							else
							{
								retValue += nc;
							}
							break;
						default:
							if (isDecimalDigit(nc))
							{
								String digitString = "" + nc;
								int dc = 0;
								while (i + dc < s.Length && isDecimalDigit(s[i + dc]) && dc < 2)
								{
									digitString += s[i + dc];
									dc++;
								}

								i += dc;
								retValue += (char)UInt16.Parse(digitString, System.Globalization.NumberStyles.Integer);
							}
							else
							{
								retValue += nc;
							}
							break;
					}
				}
			}

			return retValue;
		}

		private static Boolean isHexChar(char c)
		{
			return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
		}

		private static Boolean isDecimalDigit(char c)
		{
			return (c >= '0' && c <= '9');
		}
	}
}
