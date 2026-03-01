using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugRespawnShip : MonoBehaviour
{
	private void Awake()
	{
		this.dropdown = base.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();
		this.dropdown.ClearOptions();
		this.btnRespawnShip = base.transform.Find("SpawnButton").GetComponent<Button>();
		this.btnRespawnShip.onClick.AddListener(delegate()
		{
			this.OnButtonClick(false);
		});
		this.btnRespawnShipNPCs = base.transform.Find("SpawnButtonNPCs").GetComponent<Button>();
		this.btnRespawnShipNPCs.onClick.AddListener(delegate()
		{
			this.OnButtonClick(true);
		});
		this.btnPruneNPCs = base.transform.Find("btnPruneNPCs").GetComponent<Button>();
		this.btnPruneNPCs.onClick.AddListener(delegate()
		{
			this.PruneNPCs();
		});
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
			if (CrewSim.system.dictShips[text].LoadState < Ship.Loaded.Edit)
			{
				TMP_Dropdown.OptionData item2 = new TMP_Dropdown.OptionData(text);
				list.Add(item2);
			}
		}
		this.dropdown.AddOptions(list);
	}

	private void PruneNPCs()
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsNPCPrunable");
		List<Ship> list = new List<Ship>(CrewSim.system.GetAllLoadedShips());
		foreach (Ship ship in list)
		{
			if (ship != null && !ship.bDestroyed)
			{
				if (!(CrewSim.system.GetShipOwner(ship.strRegID) == CrewSim.coPlayer.strID))
				{
					if (ship.DMGStatus != Ship.Damage.Derelict)
					{
						if (ship.LoadState < Ship.Loaded.Edit)
						{
							if (ship.IsStation(false) || ship.IsStationHidden(false))
							{
								List<CondOwner> cos = ship.GetCOs(condTrigger, false, false, false);
								int num = ship.GetPeople(false).Count;
								foreach (CondOwner condOwner in cos)
								{
									if (num < 8)
									{
										break;
									}
									Debug.Log("Pruning " + condOwner.FriendlyName + " from " + ship.strRegID);
									condOwner.RemoveFromCurrentHome(true);
									condOwner.Destroy();
									num--;
								}
							}
							else if (ship.IsAIShip)
							{
								ship.ToggleVis(false, true);
								ship.Destroy(true);
								ship.strDebugInfo = ship.strRegID + ": Pruned";
								Debug.Log(ship.strDebugInfo);
							}
						}
					}
				}
			}
		}
	}

	public void OnButtonClick(bool bNPCs)
	{
		if (CrewSim.system == null)
		{
			Debug.LogError("ERROR: No star system loaded. Aborting.");
			return;
		}
		Ship ship = null;
		string text = this.dropdown.options[this.dropdown.value].text;
		CrewSim.system.dictShips.TryGetValue(text, out ship);
		if (ship == null || ship.json == null)
		{
			Debug.LogWarning("WARNING: Unable to find old ship: " + text + ". Will generate fresh copy.");
		}
		else if (ship.LoadState >= Ship.Loaded.Edit)
		{
			Debug.LogError("ERROR: Unable to respawn on-screen ship: " + this.dropdown.options[this.dropdown.value].text);
			return;
		}
		string shipOwner = CrewSim.system.GetShipOwner(text);
		Ship ship2 = null;
		if (ship != null)
		{
			List<CondOwner> people = ship.GetPeople(false);
			CrewSim.system.RemoveShip(ship);
			string shipTemplate = this.GetShipTemplate(ship.json.strName);
			ship2 = CrewSim.system.SpawnShip(shipTemplate, ship.strRegID, Ship.Loaded.Shallow, ship.DMGStatus, shipOwner, 100, false);
			if (ship2 != null)
			{
				ship2.objSS.CopyFrom(ship.objSS, false);
				foreach (CondOwner objCO in people)
				{
					if (bNPCs)
					{
						break;
					}
					CrewSim.MoveCO(objCO, ship2, false);
				}
				ship.Destroy(true);
			}
		}
		else
		{
			JsonStarSystemSave jsonStarSystemSave = null;
			if (!DataHandler.dictStarSystems.TryGetValue("NewGame", out jsonStarSystemSave))
			{
				Debug.LogError("ERROR: Unable to load star system template. Aborting.");
				return;
			}
			foreach (JsonSpawnStation jsonSpawnStation in jsonStarSystemSave.aSpawnStations)
			{
				if (!(jsonSpawnStation.strName != text))
				{
					CrewSim.system.SpawnStationFromJSON(jsonSpawnStation);
					ship2 = CrewSim.system.GetShipByRegID(text);
					break;
				}
			}
		}
		if (ship2 == null)
		{
			Debug.LogError("ERROR: Respawn ship failed. Aborting.");
			return;
		}
		ship2.ToggleVis(false, true);
		CrewSim.system.AddShip(ship2, shipOwner);
	}

	private string GetShipTemplate(string strJson)
	{
		switch (strJson)
		{
		case "OKLG_BIZ":
			strJson = "OKLG Bureaus";
			break;
		case "OKLG_MES":
			strJson = "OKLG Mescaform";
			break;
		case "OKLG":
			strJson = "OKLG Entrance";
			break;
		case "OKLG_FLOT":
			strJson = "Flotilla";
			break;
		case "OKLG_ATC":
			strJson = "ATC 01";
			break;
		case "OKLG_SEC":
			strJson = "Security Station";
			break;
		case "OKLG_NAV0":
			strJson = "Mooring Buoy";
			break;
		case "OKLG_NAV1":
			strJson = "OKLG NAV";
			break;
		case "OKLG_NAV2":
			strJson = "OKLG NAV2";
			break;
		case "OKLG_NAV3":
			strJson = "OKLG NAV3";
			break;
		}
		return strJson;
	}

	public TMP_Dropdown dropdown;

	public Button btnRespawnShip;

	public Button btnRespawnShipNPCs;

	public Button btnPruneNPCs;

	private bool bInit;
}
