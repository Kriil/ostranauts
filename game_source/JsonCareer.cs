using System;

// Career/background definition for character generation.
// This appears to drive starting skills, events, and condition/loot grants for
// the player's or an NPC's life-path history.
public class JsonCareer
{
	// `strName` is the internal id; `strNameFriendly` is the UI-facing career label.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strCTPrereqs { get; set; }

	// Likely references loot ids that add conditions during career resolution.
	public string strLootConds { get; set; }

	public string strLootCondsNext { get; set; }

	public string[] aSkillsFirst { get; set; }

	public string[] aSkillsNext { get; set; }

	public string[] aSkillsHobby { get; set; }

	public string[] aEvents { get; set; }

	public string[] aEventsMoney { get; set; }

	public string[] aEventsShip { get; set; }

	// Counts for how many entries to draw from the first/next/hobby pools.
	public int nFirst { get; set; }

	public int nNext { get; set; }

	public int nHobby { get; set; }

	public bool bHide { get; set; }

	public bool bComingSoon { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
