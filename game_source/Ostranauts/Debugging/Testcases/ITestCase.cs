using System;

namespace Ostranauts.Debugging.Testcases
{
	public interface ITestCase
	{
		bool SetupComplete { get; }

		void SetupTest();

		void Teardown();
	}
}
