using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Tools
{
	public class JsonLogger : MonoBehaviour
	{
		private static void Log()
		{
			if (JsonLogger._instance == null)
			{
				GameObject gameObject = new GameObject("JsonLogger");
				JsonLogger._instance = gameObject.AddComponent<JsonLogger>();
			}
			if (JsonLogger._instance._outputCoRoutine == null)
			{
				JsonLogger._instance._outputCoRoutine = JsonLogger._instance.StartCoroutine(JsonLogger._instance.DoLog());
			}
		}

		private IEnumerator DoLog()
		{
			yield return new WaitForSeconds(0.3f);
			if (!string.IsNullOrEmpty(JsonLogger._exceptionOrigin))
			{
				Debug.LogError("<color=#f24f52>Error loading file: <size=22>" + JsonLogger._exceptionOrigin + " </size></color>");
			}
			foreach (string str in JsonLogger._failingStrings)
			{
				Debug.LogError("<color=#f24f52>Error in line: <size=22>" + str + " </size></color>");
			}
			foreach (string message in JsonLogger._contextInformation)
			{
				Debug.LogError(message);
			}
			JsonLogger._exceptionOrigin = string.Empty;
			JsonLogger._failingStrings.Clear();
			JsonLogger._contextInformation.Clear();
			this._outputCoRoutine = null;
			yield break;
		}

		public static void ReportProblem(string data, ReportTypes reportType)
		{
			switch (reportType)
			{
			case ReportTypes.SourceInfo:
				JsonLogger._exceptionOrigin = data;
				break;
			case ReportTypes.FailingString:
				JsonLogger._failingStrings.Add(data);
				break;
			case ReportTypes.ContextInfo:
				JsonLogger._contextInformation.Add("<color=#f24f52>" + data + " </color>");
				break;
			case ReportTypes.GenericLog:
				JsonLogger._contextInformation.Add(data);
				break;
			}
			JsonLogger.Log();
		}

		private static JsonLogger _instance;

		private static readonly List<string> _failingStrings = new List<string>();

		private static readonly List<string> _contextInformation = new List<string>();

		private static string _exceptionOrigin;

		private Coroutine _outputCoRoutine;
	}
}
