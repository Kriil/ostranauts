using System;
using System.Collections.Generic;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
	private int Range
	{
		get
		{
			if (this._range < 0)
			{
				this._range = 0;
				CondOwner component = base.GetComponent<CondOwner>();
				if (component != null)
				{
					int.TryParse(component.mapGUIPropMaps["Panel A"]["strRange"], out this._range);
				}
			}
			return this._range;
		}
	}

	private void Init()
	{
		this.UpdateAppearance();
		this.bInit = true;
	}

	public void UpdateAppearance()
	{
		CondOwner component = base.gameObject.GetComponent<CondOwner>();
		Dictionary<string, string> dictionary = component.mapGUIPropMaps["Panel A"];
		string text = "Loot";
		if (dictionary.ContainsKey("strType"))
		{
			text = dictionary["strType"];
		}
		else
		{
			dictionary["strType"] = text;
		}
		MeshRenderer component2 = base.gameObject.GetComponent<MeshRenderer>();
		if (text.IndexOf("Pspec") >= 0)
		{
			component2.sharedMaterial = DataHandler.GetMaterial(component2, "IcoLootPspec", "blank", "blank", "blank");
		}
		else
		{
			component2.sharedMaterial = DataHandler.GetMaterial(component2, "IcoLoot", "blank", "blank", "blank");
		}
		int num = 1 + 2 * int.Parse(dictionary["strRange"]);
		base.transform.localScale = new Vector3((float)num, (float)num, 1f);
	}

	public bool IsOverWall()
	{
		CondOwner component = base.gameObject.GetComponent<CondOwner>();
		if (component == null || component.ship == null)
		{
			return false;
		}
		Dictionary<string, string> dictionary = component.mapGUIPropMaps["Panel A"];
		string value = "Loot";
		if (dictionary.ContainsKey("strType"))
		{
			value = dictionary["strType"];
		}
		else
		{
			dictionary["strType"] = value;
		}
		int num = 1 + 2 * int.Parse(dictionary["strRange"]);
		float num2 = (float)Mathf.FloorToInt((float)num / 2f);
		int num3 = (int)(base.transform.position.x - num2);
		int num4 = (int)(base.transform.position.x + num2);
		int num5 = (int)(base.transform.position.y - num2);
		int num6 = (int)(base.transform.position.y + num2);
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num5; j <= num6; j++)
			{
				Tile tileAtWorldCoords = component.ship.GetTileAtWorldCoords1((float)i, (float)j, false, true);
				if (tileAtWorldCoords != null && tileAtWorldCoords.IsWall)
				{
					return true;
				}
			}
		}
		return false;
	}

	public Tile GetSpawnTile(Ship objShip)
	{
		JsonZone spawnZone = this.GetSpawnZone(objShip);
		int num = 0;
		for (int i = 0; i < spawnZone.aTiles.Length; i++)
		{
			num = spawnZone.aTiles[i];
			if (objShip.TileIndexValid(num) && objShip.aTiles[num].bPassable)
			{
				return objShip.aTiles[num];
			}
		}
		if (objShip.TileIndexValid(num))
		{
			return objShip.aTiles[num];
		}
		return null;
	}

	public JsonZone GetSpawnZone(Ship objShip)
	{
		CondOwner component = base.GetComponent<CondOwner>();
		int nRange = 0;
		int.TryParse(component.mapGUIPropMaps["Panel A"]["strRange"], out nRange);
		return TileUtils.GetZoneFromTileRadius(objShip, component.tf.position, nRange, true, false);
	}

	public void DoLoot(Ship objShip)
	{
		if (!this.bInit)
		{
			this.Init();
		}
		CondOwner component = base.GetComponent<CondOwner>();
		string[] array = null;
		string a = null;
		JsonShipUniques jsonShipUniques = null;
		if (objShip.json.aUniques != null)
		{
			for (int i = 0; i < objShip.json.aUniques.Length; i++)
			{
				if (objShip.mapIDRemap.TryGetValue(objShip.json.aUniques[i].strCOID, out a) && a == component.strID)
				{
					jsonShipUniques = objShip.json.aUniques[i];
					array = objShip.json.aUniques[i].aConds;
					break;
				}
			}
		}
		if (component.mapGUIPropMaps.ContainsKey("Panel A"))
		{
			bool flag = LootSpawner.ShipMatch(objShip, component);
			if (flag)
			{
				string a2 = component.mapGUIPropMaps["Panel A"]["strType"];
				string text = component.mapGUIPropMaps["Panel A"]["strLoot"];
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				int j = 1;
				int.TryParse(component.mapGUIPropMaps["Panel A"]["strCount"], out j);
				if (a2 == "Loot")
				{
					JsonZone spawnZone = this.GetSpawnZone(objShip);
					CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
					List<CondOwner> cosInZone = objShip.GetCOsInZone(spawnZone, condTrigger, true, false);
					List<CondOwner> list = new List<CondOwner>();
					for (int k = 0; k < j; k++)
					{
						list.AddRange(DataHandler.GetLoot(text).GetCOLoot(null, false, null));
					}
					if (list.Count > 0 && array != null && array.Length > 0)
					{
						for (int l = 0; l < array.Length; l++)
						{
							DataHandler.CreateSimpleConditionFromString(array[l]);
						}
						list[0].SetCondAmount(array[0], 1.0, 0.0);
						CrewSimTut.UniqueToStrID.TryAdd(array[0], list[0].strID);
						if (jsonShipUniques != null)
						{
							jsonShipUniques.strCOID = list[0].strID;
						}
					}
					List<CondOwner> list2 = TileUtils.DropCOsNearby(list, objShip, spawnZone, cosInZone, condTrigger, true, false);
					while (list2.Count > 0)
					{
						list2[0].Destroy();
						list2.RemoveAt(0);
					}
				}
				else if (a2 == "Pspec Loot")
				{
					Loot loot = DataHandler.GetLoot(text);
					List<string> list3 = new List<string>();
					while (j > 0)
					{
						list3.AddRange(loot.GetLootNames(null, false, null));
						j--;
					}
					foreach (string strName in list3)
					{
						JsonPersonSpec personSpec = DataHandler.GetPersonSpec(strName);
						if (personSpec != null)
						{
							PersonSpec personSpec2 = new PersonSpec(personSpec, true);
							CondOwner condOwner = personSpec2.MakeCondOwner(PersonSpec.StartShip.OLD, objShip);
							condOwner.tf.position = this.GetSpawnPosition(objShip);
							condOwner.currentRoom = objShip.GetRoomAtWorldCoords1(condOwner.tf.position, false);
						}
					}
				}
				else if (a2 == "Lot Loot")
				{
					JsonZone spawnZone2 = this.GetSpawnZone(objShip);
					CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsLootSpawnOK");
					List<CondOwner> cosInZone2 = objShip.GetCOsInZone(spawnZone2, condTrigger2, true, false);
					List<CondOwner> lotCOs = component.GetLotCOs(false);
					foreach (CondOwner co in lotCOs)
					{
						component.RemoveLotCO(co);
					}
					List<CondOwner> list4 = TileUtils.DropCOsNearby(lotCOs, objShip, spawnZone2, cosInZone2, condTrigger2, false, true);
					while (list4.Count > 0)
					{
						list4[0].Destroy();
						list4.RemoveAt(0);
					}
				}
				else if (text != null)
				{
					JsonPersonSpec personSpec3 = DataHandler.GetPersonSpec(text);
					while (j > 0 && personSpec3 != null)
					{
						PersonSpec personSpec4 = new PersonSpec(personSpec3, true);
						CondOwner condOwner2 = personSpec4.MakeCondOwner(PersonSpec.StartShip.OLD, objShip);
						condOwner2.tf.position = this.GetSpawnPosition(objShip);
						condOwner2.currentRoom = objShip.GetRoomAtWorldCoords1(condOwner2.tf.position, false);
						j--;
					}
				}
			}
		}
	}

	public static bool ShipMatch(Ship objShip, CondOwner coLootSpawner)
	{
		string value = null;
		bool result = false;
		if (objShip.DMGStatus == Ship.Damage.New)
		{
			if (coLootSpawner.mapGUIPropMaps["Panel A"].TryGetValue("strNew", out value))
			{
				bool.TryParse(value, out result);
			}
		}
		else if (objShip.DMGStatus == Ship.Damage.Damaged || objShip.DMGStatus == Ship.Damage.Used)
		{
			if (coLootSpawner.mapGUIPropMaps["Panel A"].TryGetValue("strDamaged", out value))
			{
				bool.TryParse(value, out result);
			}
		}
		else if (objShip.DMGStatus == Ship.Damage.Derelict && coLootSpawner.mapGUIPropMaps["Panel A"].TryGetValue("strDerelict", out value))
		{
			bool.TryParse(value, out result);
		}
		return result;
	}

	public Vector3 GetSpawnPosition(Ship objShip)
	{
		if (objShip.aTiles != null && objShip.aTiles.Count > 0)
		{
			Tile spawnTile = this.GetSpawnTile(objShip);
			if (spawnTile != null)
			{
				return spawnTile.tf.position;
			}
		}
		else if (this.Range > 0)
		{
			int num = UnityEngine.Random.Range(-this.Range, this.Range + 1);
			int num2 = UnityEngine.Random.Range(-this.Range, this.Range + 1);
			Vector3 position = base.transform.position;
			return new Vector3(position.x + (float)num, position.y + (float)num2, position.z);
		}
		return base.transform.position;
	}

	private bool bInit;

	private int _range = -1;
}
