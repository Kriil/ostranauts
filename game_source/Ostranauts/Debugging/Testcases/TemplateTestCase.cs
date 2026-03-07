using System;
using System.Collections;
using Ostranauts.Tools.ExtensionMethods;

namespace Ostranauts.Debugging.Testcases
{
	public class TemplateTestCase : ITestCase
	{
		public bool SetupComplete
		{
			get
			{
				return true;
			}
		}

		public void SetupTest()
		{
		}

		public void Teardown()
		{
		}

		[TestCase(Title = "Template", Description = "explain what this does", Order = 1)]
		private IEnumerator TemplateTest()
		{
			yield return null;
			yield break;
		}
	}
}
