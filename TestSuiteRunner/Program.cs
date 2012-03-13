using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Lamn;

namespace TestSuiteRunner
{
	class Program
	{
		static void Main(string[] args)
		{
			List<String> testFiles = getTestFiles();

			foreach (String testFile in testFiles)
			{
				System.Console.Out.Write("Running " + testFile + ": ");

				LamnEngine l = new LamnEngine();
				StringWriter outputBuff = new StringWriter();
				l.OutputStream = outputBuff;

				try
				{
					l.Run(System.IO.File.ReadAllText(testFile));
					System.Console.Out.WriteLine("Tests passed");
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine("Assert failed");
					System.Console.Out.WriteLine(outputBuff.ToString());
					System.Console.Out.WriteLine(e);
					break;
				}
			}
		}

		static List<String> getTestFiles()
		{
			List<String> tests = new List<String>();
			tests.AddRange(Directory.GetFiles("../../../TestFiles", "*.lua", SearchOption.AllDirectories));
			return tests;
		}
	}
}
