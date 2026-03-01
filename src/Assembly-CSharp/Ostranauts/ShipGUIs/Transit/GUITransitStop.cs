using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Transit
{
	public class GUITransitStop : MonoBehaviour
	{
		public void SetData(JsonTransitConnection connection, int index)
		{
			string text = connection.strLabelNameOptional;
			if (string.IsNullOrEmpty(connection.strLabelNameOptional))
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(connection.strTargetRegID);
				if (shipByRegID != null)
				{
					text = shipByRegID.publicName;
				}
				else
				{
					text = connection.strTargetRegID;
				}
			}
			this.txtElevatorText.text = text;
			if (this.imgFrame != null)
			{
				this.imgFrame.color = GUITransitStop._frameColor[index % GUITransitStop._frameColor.Length];
			}
			if (this.canvas != null)
			{
				Canvas componentInParent = base.transform.parent.GetComponentInParent<Canvas>();
				int num = 0;
				if (componentInParent != null)
				{
					num = componentInParent.sortingOrder;
				}
				this.canvas.sortingOrder = num + index + 1;
			}
		}

		[SerializeField]
		private TMP_Text txtElevatorText;

		[SerializeField]
		private Image imgFrame;

		[SerializeField]
		private Canvas canvas;

		private static Color[] _frameColor = new Color[]
		{
			new Color(0.76862746f, 0.42745098f, 0.36862746f),
			new Color(0.9607843f, 0.4117647f, 0.3764706f),
			new Color(0.22745098f, 0.28627452f, 0.36078432f),
			new Color(0.5372549f, 0.5764706f, 0.4862745f),
			new Color(0.23529412f, 0.5686275f, 0.9019608f)
		};
	}
}
