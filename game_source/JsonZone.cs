using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JsonZone : IComparable<JsonZone>
{
	public string strName { get; set; }

	public string strRegID { get; set; }

	public bool bTriggerOnOwner { get; set; }

	public int[] aTiles { get; set; }

	public int[] aOldTiles { get; set; }

	public string[] aTileConds { get; set; }

	public string[] categoryConds { get; set; }

	public string strPersonSpec { get; set; }

	public Color zoneColor { get; set; }

	public string strTargetPSpec { get; set; }

	public string[] ranks { get; set; }

	public void Destroy()
	{
		this.strName = null;
		this.strRegID = null;
		this.aTiles = null;
		this.aTileConds = null;
		this.strPersonSpec = null;
		this.strTargetPSpec = null;
		this.categoryConds = null;
	}

	public JsonZone Clone()
	{
		JsonZone jsonZone = new JsonZone();
		jsonZone.strName = this.strName;
		jsonZone.strRegID = this.strRegID;
		jsonZone.bTriggerOnOwner = this.bTriggerOnOwner;
		jsonZone.aTiles = ((this.aTiles == null) ? new int[0] : ((int[])this.aTiles.Clone()));
		jsonZone.aTileConds = ((this.aTileConds == null) ? new string[0] : ((string[])this.aTileConds.Clone()));
		jsonZone.strPersonSpec = this.strPersonSpec;
		jsonZone.zoneColor = this.zoneColor;
		if (this.ranks != null)
		{
			if (this.ranks.Contains("IsPlayerCrew"))
			{
				this.strTargetPSpec = "ZoneCrew";
			}
			else if (this.ranks.Contains("IsPlayer"))
			{
				this.strTargetPSpec = "ZoneCaptain";
			}
			else
			{
				this.strTargetPSpec = "ZoneCaptainAndCrew";
			}
			this.ranks = null;
		}
		jsonZone.strTargetPSpec = this.strTargetPSpec;
		jsonZone.categoryConds = ((this.categoryConds == null) ? new string[0] : this.categoryConds);
		return jsonZone;
	}

	public void RemoveTile(int nIndex, Ship objShip)
	{
		if (this.aTiles == null)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < this.aTiles.Length; i++)
		{
			if (this.aTiles[i] != nIndex)
			{
				list.Add(this.aTiles[i]);
			}
		}
		if (list.Count == 0)
		{
			objShip.mapZones.Remove(this.strName);
		}
		else
		{
			list.Sort();
			this.aTiles = list.ToArray();
		}
	}

	public int CompareTo(JsonZone other)
	{
		if (other == null || other.categoryConds == null)
		{
			return -1;
		}
		if (this.categoryConds == null)
		{
			return 1;
		}
		int num = this.categoryConds.Length;
		int num2 = other.categoryConds.Length;
		if ((num == 1 && num2 != 1) || (num < num2 && num != 0) || (num > 1 && num2 == 0))
		{
			return -1;
		}
		if (num == num2)
		{
			return 0;
		}
		if (num == 0 || num > num2)
		{
			return 1;
		}
		return 0;
	}

	public bool Matches(CondOwner coTest, bool bCheckOwner)
	{
		if (coTest == null)
		{
			return true;
		}
		PersonSpec personSpec = null;
		if (this.strPersonSpec != null)
		{
			personSpec = StarSystem.GetPerson(DataHandler.GetPersonSpec(this.strPersonSpec), null, false, null, null);
		}
		if (bCheckOwner && this.bTriggerOnOwner && (personSpec == null || personSpec.GetCO() == coTest))
		{
			return true;
		}
		if (string.IsNullOrEmpty(this.strTargetPSpec))
		{
			return true;
		}
		JsonPersonSpec personSpec2 = DataHandler.GetPersonSpec(this.strTargetPSpec);
		if (personSpec2 == null)
		{
			return true;
		}
		if (personSpec == null)
		{
			if (personSpec2.Matches(coTest))
			{
				return true;
			}
		}
		else if (personSpec.IsCOMyMother(personSpec2, coTest))
		{
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		return this.strName;
	}

	internal void AddTiles(List<int> selectedTileIndices)
	{
		foreach (int item in this.aTiles)
		{
			if (!selectedTileIndices.Contains(item))
			{
				selectedTileIndices.Add(item);
			}
		}
		this.aTiles = selectedTileIndices.ToArray();
	}

	internal void RemoveTiles(List<int> selectedTileIndices)
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		foreach (int item in this.aTiles)
		{
			if (selectedTileIndices.Contains(item))
			{
				list2.Add(item);
			}
			else
			{
				list.Add(item);
			}
		}
		this.aTiles = list.ToArray();
		this.aOldTiles = list2.ToArray();
	}
}
