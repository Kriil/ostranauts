using System;
using System.Collections.Generic;

namespace Ostranauts.Tools.ExtensionMethods
{
	public static class ListExtenions
	{
		public static List<T> Randomize<T>(this List<T> list)
		{
			Random random = new Random();
			List<T> list2 = new List<T>(list);
			for (int i = 0; i < list2.Count - 1; i++)
			{
				int index = random.Next(i + 1);
				T value = list2[index];
				list2[index] = list2[i];
				list2[i] = value;
			}
			return list2;
		}
	}
}
