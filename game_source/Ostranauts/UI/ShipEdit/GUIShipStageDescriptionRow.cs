using System;
using Ostranauts.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.ShipEdit
{
	public class GUIShipStageDescriptionRow : MonoBehaviour
	{
		private void Awake()
		{
			this.btnDelete.onClick.AddListener(new UnityAction(this.OnDeleteClicked));
		}

		private void OnDeleteClicked()
		{
			MonoSingleton<GUIShipEdit>.Instance.ShowHelp = false;
			MonoSingleton<GUIShipEdit>.Instance.BuildConstructionList(false);
		}

		[SerializeField]
		private Button btnDelete;
	}
}
