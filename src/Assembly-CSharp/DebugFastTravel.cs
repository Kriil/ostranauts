using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DebugFastTravel : MonoBehaviour
{
	private void Awake()
	{
		this.dropdown = base.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();
		this.dropdown.ClearOptions();
		this.button = base.transform.Find("TravelButton").GetComponent<Button>();
		this.button.onClick.AddListener(new UnityAction(this.OnButtonClick));
	}

	private void Start()
	{
		this.dropdown.ClearOptions();
		if (!this.bInit)
		{
			this.Init();
		}
	}

	public void Init()
	{
		if (CrewSim.system == null)
		{
			return;
		}
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in CrewSim.system.aBOs)
		{
			TMP_Dropdown.OptionData item = new TMP_Dropdown.OptionData(keyValuePair.Key);
			list.Add(item);
		}
		foreach (string text in CrewSim.system.dictShips.Keys)
		{
			if (!(CrewSim.coPlayer != null) || !(CrewSim.coPlayer.ship.strRegID == text))
			{
				TMP_Dropdown.OptionData item2 = new TMP_Dropdown.OptionData(text);
				list.Add(item2);
			}
		}
		this.dropdown.AddOptions(list);
	}

	public void OnButtonClick()
	{
		List<Ship> allDockedShips = CrewSim.coPlayer.ship.GetAllDockedShips();
		foreach (Ship objShipThem in allDockedShips)
		{
			CrewSim.UndockShip(CrewSim.coPlayer.ship, objShipThem, false, false);
		}
		CrewSim.coPlayer.ship.objSS.strBOPORShip = null;
		Ship ship;
		if (CrewSim.system.dictShips.TryGetValue(this.dropdown.options[this.dropdown.value].text, out ship))
		{
			CrewSim.coPlayer.ship.objSS.CopyFrom(ship.objSS, true);
			CrewSim.DockShip(CrewSim.coPlayer.ship, ship.strRegID);
		}
		else
		{
			foreach (KeyValuePair<string, BodyOrbit> keyValuePair in CrewSim.system.aBOs)
			{
				if (this.dropdown.options[this.dropdown.value].text == keyValuePair.Key)
				{
					BodyOrbit value = keyValuePair.Value;
					CrewSim.coPlayer.ship.objSS.vPosx = value.dXReal + value.fRadius * 1.25;
					CrewSim.coPlayer.ship.objSS.vPosy = value.dYReal + value.fRadius * 1.25;
					CrewSim.coPlayer.ship.objSS.vVelX = value.dVelX;
					CrewSim.coPlayer.ship.objSS.vVelY = value.dVelY;
					break;
				}
			}
		}
		CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bShoreLeave = true;
	}

	public TMP_Dropdown dropdown;

	public Button button;

	private bool bInit;
}
