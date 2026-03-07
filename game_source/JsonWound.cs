using System;
using System.Collections.Generic;

public class JsonWound
{
	public string strName { get; set; }

	public string strPainPNG { get; set; }

	public string strLootBluntVerbs { get; set; }

	public string strLootCutVerbs { get; set; }

	public bool bBleeds { get; set; }

	public List<string> aSlotOverlaps { get; set; }

	public float fHitChance { get; set; }

	public Dictionary<string, string> mapEffects;
}
