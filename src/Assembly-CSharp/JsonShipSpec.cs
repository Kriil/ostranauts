using System;
using System.Collections.Generic;

// Ship-matching filter/spec record. Likely used by jobs, spawns, comms, or
// other scripted world logic to test whether a live ship fits certain criteria.
public class JsonShipSpec
{
	// Likely the spec id used by other JSON/runtime systems to reference this
	// ship filter entry.
	public string strName { get; set; }

	public int[] aDMGStatus { get; set; }

	public string[] aOwners { get; set; }

	public string[] aFactions { get; set; }

	public int nIsAIShip { get; set; }

	public int nIsDocked { get; set; }

	public int nIsFlyingDark { get; set; }

	public int nIsHidden { get; set; }

	public int nIsLocalAuthority { get; set; }

	public int nIsPlayerShip { get; set; }

	public int nIsStation { get; set; }

	public int nIsStationOrHidden { get; set; }

	public int nIsStationRegional { get; set; }

	public int nIsStationHidden { get; set; }

	public int nLoadState { get; set; }

	public string strDockedWith { get; set; }

	public string strLootRegIDs { get; set; }

	public string strCTTest { get; set; }

	public string strLootATCRegions { get; set; }

	// Tests one live ship against the data-driven filter. This checks damage
	// state, faction/owner membership, role flags, docking, load state, and
	// optional CondTrigger/loot-driven region constraints.
	public bool Matches(Ship ship, CondOwner coUs = null)
	{
		if (ship == null || ship.bDestroyed)
		{
			return false;
		}
		if (this.aDMGStatus != null && this.aDMGStatus.Length > 0 && Array.IndexOf<int>(this.aDMGStatus, (int)ship.DMGStatus) < 0)
		{
			return false;
		}
		if (this.aFactions != null && this.aFactions.Length > 0)
		{
			bool flag = false;
			List<JsonFaction> shipFactions = ship.GetShipFactions();
			foreach (string text in this.aFactions)
			{
				if (string.IsNullOrEmpty(text))
				{
					flag = true;
					break;
				}
				if (text == "[us]")
				{
					if (!(coUs == null) && coUs.SharesFactionsWith(shipFactions))
					{
						flag = true;
						break;
					}
				}
				else if (!ship.HasFaction(text))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (this.aOwners != null && this.aOwners.Length > 0)
		{
			bool flag2 = false;
			string shipOwner = CrewSim.system.GetShipOwner(ship.strRegID);
			foreach (string text2 in this.aOwners)
			{
				if (string.IsNullOrEmpty(text2))
				{
					flag2 = true;
					break;
				}
				if (text2 == "[us]")
				{
					if (!(coUs == null) && !(coUs.strID != shipOwner))
					{
						flag2 = true;
						break;
					}
				}
				else if (!(text2 != shipOwner))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
		}
		if (this.nIsAIShip != 0 && !this.IntMatchesBool(this.nIsAIShip, ship.IsLocalAuthority))
		{
			return false;
		}
		if (this.nIsDocked != 0 && !this.IntMatchesBool(this.nIsDocked, ship.IsDockedFull()))
		{
			return false;
		}
		if (this.nIsFlyingDark != 0 && !this.IntMatchesBool(this.nIsFlyingDark, ship.IsFlyingDark()))
		{
			return false;
		}
		if (this.nIsHidden != 0 && !this.IntMatchesBool(this.nIsHidden, ship.HideFromSystem))
		{
			return false;
		}
		if (this.nIsLocalAuthority != 0 && !this.IntMatchesBool(this.nIsLocalAuthority, ship.IsLocalAuthority))
		{
			return false;
		}
		if (this.nIsPlayerShip != 0 && !this.IntMatchesBool(this.nIsPlayerShip, ship.IsPlayerShip()))
		{
			return false;
		}
		if (this.nIsStation != 0 && !this.IntMatchesBool(this.nIsStation, ship.IsStation(false)))
		{
			return false;
		}
		if (this.nIsStationOrHidden != 0 && !this.IntMatchesBool(this.nIsStationOrHidden, ship.IsStation(false) || ship.IsStationHidden(false)))
		{
			return false;
		}
		if (this.nIsStationRegional != 0 && !this.IntMatchesBool(this.nIsStationRegional, ship.objSS != null && ship.objSS.bIsRegion))
		{
			return false;
		}
		if (this.nIsStationHidden != 0 && !this.IntMatchesBool(this.nIsStationHidden, ship.IsStationHidden(false)))
		{
			return false;
		}
		if (this.nLoadState != 0)
		{
			if (this.nLoadState > 0 && ship.LoadState < Ship.Loaded.Edit)
			{
				return false;
			}
			if (this.nLoadState < 0 && ship.LoadState >= Ship.Loaded.Edit)
			{
				return false;
			}
		}
		if (this.strDockedWith != null)
		{
			JsonShipSpec shipSpec = DataHandler.GetShipSpec(this.strDockedWith);
			if (shipSpec != null && !shipSpec.Matches(ship, coUs))
			{
				return false;
			}
		}
		if (!string.IsNullOrEmpty(this.strLootRegIDs))
		{
			List<string> lootNames = DataHandler.GetLoot(this.strLootRegIDs).GetLootNames(null, false, null);
			if (lootNames.Count >= 1 && !lootNames.Contains(ship.strRegID))
			{
				return false;
			}
		}
		if (!string.IsNullOrEmpty(this.strLootATCRegions))
		{
			List<string> lootNames2 = DataHandler.GetLoot(this.strLootATCRegions).GetLootNames(null, false, null);
			Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(ship.objSS.vPosx, ship.objSS.vPosy);
			if (lootNames2.Count > 0 && (nearestStationRegional == null || !lootNames2.Contains(nearestStationRegional.strRegID)))
			{
				return false;
			}
		}
		if (!string.IsNullOrEmpty(this.strCTTest))
		{
			if (ship.ShipCO == null)
			{
				return false;
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger(this.strCTTest);
			if (!condTrigger.Triggered(ship.ShipCO, null, true))
			{
				return false;
			}
		}
		return true;
	}

	private bool IntMatchesBool(int nIn, bool bIn)
	{
		return nIn == 0 || (nIn > 0 && bIn) || (nIn < 0 && !bIn);
	}

	public JsonShipSpec Clone()
	{
		JsonShipSpec jsonShipSpec = (JsonShipSpec)base.MemberwiseClone();
		if (this.aDMGStatus != null)
		{
			jsonShipSpec.aDMGStatus = (int[])this.aDMGStatus.Clone();
		}
		if (this.aOwners != null)
		{
			jsonShipSpec.aOwners = (string[])this.aOwners.Clone();
		}
		if (this.aFactions != null)
		{
			jsonShipSpec.aFactions = (string[])this.aFactions.Clone();
		}
		return jsonShipSpec;
	}

	public JsonShipSpec CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		JsonShipSpec jsonShipSpec = this.Clone();
		jsonShipSpec.strName = this.strName.Replace(strFind, strReplace);
		DataHandler.dictShipSpecs[jsonShipSpec.strName] = jsonShipSpec;
		return jsonShipSpec;
	}

	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonShipSpec jsonShipSpec = null;
		if (!DataHandler.dictShipSpecs.TryGetValue(strOrigName, out jsonShipSpec))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonShipSpec jsonShipSpec2 = null;
		if (!DataHandler.dictShipSpecs.TryGetValue(text, out jsonShipSpec2))
		{
			jsonShipSpec2 = jsonShipSpec.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
