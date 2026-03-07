using System;
using UnityEngine.UI;

namespace Ostranauts.Electrical
{
	[Serializable]
	public class GUIConnection
	{
		public string id;

		public Toggle toggle;

		public Button screw;

		public Image img;

		public bool status;
	}
}
