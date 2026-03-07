using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ostranauts.Core;
using Ostranauts.Debugging.Testcases;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

namespace Ostranauts.Debugging
{
	public class TestManager : MonoSingleton<TestManager>
	{
		private new void Awake()
		{
			base.Awake();
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			Debug.LogError("TestManager was called in standalone environment!");
		}

		public List<ITestCase> GetTestCases()
		{
			List<ITestCase> result;
			if ((result = this._testCases) == null)
			{
				result = (this._testCases = new List<ITestCase>());
			}
			return result;
		}

		public string[] GetTestNames()
		{
			List<string> list = new List<string>();
			foreach (ITestCase testCase in this._testCases)
			{
				list.Add(testCase.GetType().Name);
			}
			return list.ToArray();
		}

		public List<TestCaseDTO> GetSubTests(ITestCase testcase)
		{
			List<TestCaseDTO> list = new List<TestCaseDTO>();
			Type type = testcase.GetType();
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				object[] customAttributes = methodInfo.GetCustomAttributes(true);
				foreach (object obj in customAttributes)
				{
					if (obj is TestCase)
					{
						TestCaseDTO item = new TestCaseDTO
						{
							Title = ((TestCase)obj).Title,
							Description = ((TestCase)obj).Description,
							Order = ((TestCase)obj).Order,
							TestMethod = methodInfo
						};
						list.Add(item);
					}
				}
			}
			return list;
		}

		public void RunTest(ITestCase testCase)
		{
			if (testCase == null)
			{
				return;
			}
			testCase.SetupTest();
			List<TestCaseDTO> subTests = this.GetSubTests(testCase);
			base.StartCoroutine(this.TestRunner(testCase, from x in subTests
			orderby x.Order
			select x));
		}

		public static void Log(string output, LogColor color = LogColor.NoColor)
		{
			if (MonoSingleton<TestManager>.Instance._testLog == null)
			{
				MonoSingleton<TestManager>.Instance._testLog = new List<string>
				{
					TestManager.ColorFormat(output, color)
				};
			}
			else
			{
				MonoSingleton<TestManager>.Instance._testLog.Add("\n" + TestManager.ColorFormat(output, color));
			}
		}

		public static void AssertIsTrue(bool passed, string testName)
		{
			if (passed)
			{
				TestManager.Log("Test passed: " + testName, LogColor.Green);
			}
			else
			{
				TestManager.Log("Test failed: " + testName, LogColor.Red);
			}
		}

		public static void AssertIsFalse(bool outcome, string testName)
		{
			TestManager.AssertIsTrue(!outcome, testName);
		}

		private IEnumerator TestRunner(ITestCase testCase, IEnumerable<TestCaseDTO> subTests)
		{
			yield return new WaitUntil(() => testCase.SetupComplete);
			Debug.LogWarning("<color=#008080>Starting test " + testCase + "</color>");
			yield return null;
			foreach (TestCaseDTO subtest in subTests)
			{
				TestManager.Log("Test: " + subtest.Title, LogColor.Neutral);
				yield return (IEnumerator)subtest.TestMethod.Invoke(testCase, null);
				TestManager.PrintResults();
			}
			TestManager.PrintResults();
			testCase.Teardown();
			yield break;
		}

		private static string ColorFormat(string text, LogColor color)
		{
			if (color == LogColor.NoColor)
			{
				return text;
			}
			return string.Concat(new string[]
			{
				"<color=",
				TestManager._colorDict[color],
				">",
				text,
				"</color>"
			});
		}

		private static void PrintResults()
		{
			if (MonoSingleton<TestManager>.Instance._testLog == null)
			{
				return;
			}
			if (MonoSingleton<TestManager>.Instance._testLog.Any((string x) => x.Contains("failed")))
			{
				MonoSingleton<TestManager>.Instance._testLog[0] = "<color=#ff0000>" + MonoSingleton<TestManager>.Instance._testLog[0] + " - FAILED</color>";
			}
			else
			{
				MonoSingleton<TestManager>.Instance._testLog[0] = "<color=#00ff00>" + MonoSingleton<TestManager>.Instance._testLog[0] + " - PASSED</color>";
			}
			string text = string.Empty;
			foreach (string str in MonoSingleton<TestManager>.Instance._testLog)
			{
				text += str;
			}
			Debug.LogWarning(text);
			MonoSingleton<TestManager>.Instance._testLog = null;
		}

		private List<ITestCase> _testCases = new List<ITestCase>();

		private List<string> _testLog;

		private static readonly Dictionary<LogColor, string> _colorDict = new Dictionary<LogColor, string>
		{
			{
				LogColor.NoColor,
				string.Empty
			},
			{
				LogColor.Green,
				"#00ff00"
			},
			{
				LogColor.Neutral,
				"#008080"
			},
			{
				LogColor.Red,
				"#ff0000"
			}
		};
	}
}
