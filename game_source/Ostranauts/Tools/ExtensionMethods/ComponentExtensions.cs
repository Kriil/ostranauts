using System;
using UnityEngine;

namespace Ostranauts.Tools.ExtensionMethods
{
	public static class ComponentExtensions
	{
		public static string GetPath(this Transform current)
		{
			if (current.parent == null)
			{
				return "/" + current.name;
			}
			return current.parent.GetPath() + "/" + current.name;
		}
	}
}
