using System;
using UnityEngine;

// Lightweight shift descriptor used by company schedules.
// These are the values referenced by `JsonCompanyRules.aHours`.
public class JsonShift
{
	public JsonShift()
	{
	}

	// Convenience constructor for the built-in Free/Sleep/Work entries.
	public JsonShift(int nID, string strName, string strCondLoot, Color clr)
	{
		this.nID = nID;
		this.strName = strName;
		this.strCondLoot = strCondLoot;
		this.clr = clr;
	}

	// Shift id, display name, linked condition/loot id, and UI tint color.
	public int nID = -1;

	public string strName;

	public string strCondLoot;

	public Color clr;
}
