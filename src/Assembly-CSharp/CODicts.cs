using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// Global CondOwner lookup/index. Keeps fast name-to-CondOwner lists and emits
// add/remove events for a few watched item categories.
public class CODicts : MonoBehaviour
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event AddCOEventHandler AddedDamagedWall;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event RemovedCOEventHandler RemovedDamageWall;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event AddCOEventHandler AddedDamagedFloor;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event RemovedCOEventHandler RemovedDamagedFloor;

	private void Start()
	{
		this.COAddEvents.Add("ItmWall1x1Dmg", this.AddedDamagedWall);
		this.CORemoveEvents.Add("ItmWall1x1Dmg", this.RemovedDamageWall);
		this.COAddEvents.Add("ItmFloorGrate01Dmg", this.AddedDamagedFloor);
		this.CORemoveEvents.Add("ItmFloorGrate01Dmg", this.RemovedDamagedFloor);
	}

	public CondOwner GetTriggeredCOByType(CondTrigger ct, string COName)
	{
		CondOwner result = null;
		if (this.COMapList.ContainsKey(COName))
		{
			for (int i = 0; i < this.COMapList[COName].Count; i++)
			{
				if (ct == null || ct.Triggered(this.COMapList[COName][i], null, true))
				{
					result = this.COMapList[COName][i];
				}
			}
		}
		return result;
	}

	public List<CondOwner> GetTriggeredCOListByType(CondTrigger ct, string COName)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.COMapList.ContainsKey(COName))
		{
			for (int i = 0; i < this.COMapList[COName].Count; i++)
			{
				if (ct == null || ct.Triggered(this.COMapList[COName][i], null, true))
				{
					list.Add(this.COMapList[COName][i]);
				}
			}
		}
		return list;
	}

	public CondOwner GetNearestCO(Vector3 location, string COName)
	{
		CondOwner condOwner = null;
		if (this.COMapList.ContainsKey(COName) && this.COMapList[COName].Count > 0)
		{
			condOwner = this.COMapList[COName][0];
			float sqrMagnitude = (location - condOwner.tf.position).sqrMagnitude;
			for (int i = 1; i < this.COMapList[COName].Count; i++)
			{
				Vector3 vector = location - this.COMapList[COName][i].tf.position;
				if (sqrMagnitude > vector.sqrMagnitude)
				{
					sqrMagnitude = vector.sqrMagnitude;
					condOwner = this.COMapList[COName][i];
				}
			}
		}
		return condOwner;
	}

	public void AddCO(CondOwner CO)
	{
		List<CondOwner> list;
		this.COMapList.TryGetValue(CO.strName, out list);
		if (list != null)
		{
			if (list.Contains(CO))
			{
				return;
			}
			list.Add(CO);
		}
		else
		{
			this.COMapList[CO.strName] = new List<CondOwner>
			{
				CO
			};
		}
		if (this.COAddEvents.ContainsKey(CO.strName) && this.COAddEvents[CO.strName] != null)
		{
			this.COAddEvents[CO.strName](this, new AddedCOEventArgs(CO));
		}
	}

	public void RemoveCO(CondOwner CO)
	{
		List<CondOwner> list;
		this.COMapList.TryGetValue(CO.strName, out list);
		if (list != null)
		{
			list.Remove(CO);
		}
		if (this.CORemoveEvents.ContainsKey(CO.strName) && this.CORemoveEvents[CO.strName] != null)
		{
			this.CORemoveEvents[CO.strName](this, new RemovedCOEventArgs(CO));
		}
	}

	private void Update()
	{
	}

	public Dictionary<string, List<CondOwner>> COMapList = new Dictionary<string, List<CondOwner>>();

	public Dictionary<string, AddCOEventHandler> COAddEvents = new Dictionary<string, AddCOEventHandler>();

	public Dictionary<string, RemovedCOEventHandler> CORemoveEvents = new Dictionary<string, RemovedCOEventHandler>();
}