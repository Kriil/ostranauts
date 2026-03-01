using System;
using System.Reflection;

namespace Ostranauts.Debugging
{
	public class TestCaseDTO
	{
		public string Title = "Missing";

		public string Description = "None";

		public int Order = 10;

		public MethodInfo TestMethod;
	}
}
