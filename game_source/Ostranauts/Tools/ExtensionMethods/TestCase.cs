using System;

namespace Ostranauts.Tools.ExtensionMethods
{
	public class TestCase : Attribute
	{
		public string Title { get; set; }

		public string Description { get; set; }

		public int Order { get; set; }
	}
}
