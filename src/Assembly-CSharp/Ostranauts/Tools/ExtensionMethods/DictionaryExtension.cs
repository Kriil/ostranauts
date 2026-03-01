using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.JsonTypes.Interfaces;

namespace Ostranauts.Tools.ExtensionMethods
{
	public static class DictionaryExtension
	{
		public static void Verify<T>(this Dictionary<string, T> dictionary) where T : IVerifiable
		{
			foreach (KeyValuePair<string, T> keyValuePair in dictionary)
			{
				T value = keyValuePair.Value;
				foreach (KeyValuePair<string, IEnumerable> kvp in value.GetVerifiables())
				{
					if (!DataHandler.IsNameRegistered(kvp))
					{
						string text = "cross-reference";
						JsonLogger.ReportProblem(string.Concat(new string[]
						{
							"Missing ",
							text,
							": <color=#F6CF00>",
							kvp.Key,
							"</color> found in ",
							typeof(T).Name,
							" ",
							keyValuePair.Key
						}), ReportTypes.GenericLog);
					}
				}
			}
		}

		public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key))
			{
				return false;
			}
			dictionary.Add(key, value);
			return true;
		}

		public static void Increment<T>(this Dictionary<T, int> dictionary, T key)
		{
			int num;
			dictionary.TryGetValue(key, out num);
			dictionary[key] = num + 1;
		}

		public static void Decrement<T>(this Dictionary<T, int> dictionary, T key)
		{
			int num = 0;
			dictionary.TryGetValue(key, out num);
			num--;
			if (num <= 0)
			{
				dictionary.Remove(key);
			}
			else
			{
				dictionary[key] = num;
			}
		}

		public static Dictionary<TKey, TValue> CloneShallow<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
		{
			return new Dictionary<TKey, TValue>(dictionary);
		}
	}
}
