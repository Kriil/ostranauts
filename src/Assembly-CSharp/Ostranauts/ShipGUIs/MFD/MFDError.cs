using System;
using System.Collections.Generic;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDError : MFDPage
	{
		public MFDError()
		{
			this.Title = "ERROR";
			this.Left = new List<string>
			{
				"INITIALIZATION ERROR",
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty
			};
			base.UpdateDisplay();
		}
	}
}
