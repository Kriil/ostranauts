using System;
using UnityEngine;

public static class ConsoleProDebug
{
	public static void Clear()
	{
	}

	public static void LogToFilter(string inLog, string inFilterName)
	{
		Debug.Log(inLog + "\nCPAPI:{\"cmd\":\"Filter\" \"name\":\"" + inFilterName + "\"}");
	}

	public static void Watch(string inName, string inValue)
	{
		Debug.Log(string.Concat(new string[]
		{
			inName,
			" : ",
			inValue,
			"\nCPAPI:{\"cmd\":\"Watch\" \"name\":\"",
			inName,
			"\"}"
		}));
	}
}
