using System;

// Derelict-ring spawn recipe.
// Likely used by StarSystem to generate salvageable wrecks around a body,
// using a loot ship template plus faction/ownership metadata.
public class JsonSpawnDerelict
{
	// `strLootShipType` likely references a loot or ship template id used for the wreck.
	public string strLootShipType { get; set; }

	public string strSpawnAroundBody { get; set; }

	public int nSpawnCountMin { get; set; }

	public int nSpawnCountMax { get; set; }

	public double fSpawnRadiusMinKM { get; set; }

	public double fSpawnRadiusMaxKM { get; set; }

	public float fVelMaxKMpS { get; set; }

	public bool bIsBodyLocked { get; set; }

	// Optional faction tags and owner id applied to the spawned derelicts.
	public string[] aFactions { get; set; }

	public string strOwner { get; set; }
}
