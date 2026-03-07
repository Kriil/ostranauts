using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Transit
{
	public class GUITransitButton : MonoBehaviour
	{
		public void SetData(JsonTransitConnection connection, bool isEnabled, bool isOn)
		{
			if (connection == null)
			{
				return;
			}
			this._jsonTransit = connection;
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
			this.txtStopName.text = text;
			this.btnTransitStop.onClick.AddListener(new UnityAction(this.OnButtonDown));
			GUITransit.OnButtonPressed.AddListener(new UnityAction<JsonTransitConnection>(this.OnUserPressedButton));
			this.btnTransitStop.interactable = isEnabled;
			this.pnlLocked.SetActive(!isEnabled);
			this.guiLamp.State = ((!isOn) ? 0 : 3);
		}

		private void OnDestroy()
		{
			if (GUITransit.OnButtonPressed != null)
			{
				GUITransit.OnButtonPressed.RemoveListener(new UnityAction<JsonTransitConnection>(this.OnUserPressedButton));
			}
		}

		private void OnUserPressedButton(JsonTransitConnection jTransit)
		{
			if (jTransit == null)
			{
				return;
			}
			this.guiLamp.State = ((jTransit != this._jsonTransit) ? 0 : 3);
		}

		private void OnButtonDown()
		{
			GUITransit.OnButtonPressed.Invoke(this._jsonTransit);
		}

		[SerializeField]
		private TextMeshProUGUI txtStopName;

		[SerializeField]
		private Button btnTransitStop;

		[SerializeField]
		private GUILamp guiLamp;

		[SerializeField]
		private GameObject pnlLocked;

		private JsonTransitConnection _jsonTransit;
	}
}
