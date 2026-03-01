using System;

// Serialized snapshot for one star system inside a full game save.
// Likely written as part of JsonGameSave so orbital state, spawned content,
// and ship ownership can be restored after loading.
public class JsonStarSystemSave
{
	// `strName` matches the star system definition id used by the runtime registry.
	public string strName { get; set; }

	// In-game epoch timestamp for orbital/body simulation when the save was written.
	public double dfEpoch { get; set; }

	// Saved orbital/body state for planets, stations, and other body orbits in this system.
	public JsonBodyOrbitSave[] aBOs { get; set; }

	// Flat key/value pairs of ship id -> serialized ship reference used during load relinking.
	public string[] dictShips { get; set; }

	// Flat key/value pairs of ship id -> owner CondOwner id.
	public string[] dictShipOwners { get; set; }

	// Flat key/value pairs describing parent/child relationships between body orbits.
	public string[] dictBOHierarchy { get; set; }

	// Spawn records for dynamic bodies still eligible to appear in this system.
	public JsonSpawnBodyOrbit[] aSpawnBodies { get; set; }

	// Spawn records for station generation.
	public JsonSpawnStation[] aSpawnStations { get; set; }

	// Spawn records for derelict rings or salvageable wreck content.
	public JsonSpawnDerelict[] aSpawnDerelictRings { get; set; }

	// Company and faction state cached with this star system snapshot.
	public JsonCompany[] aComps { get; set; }

	public JsonFaction[] aFactions { get; set; }

	// Persistent ship traffic/messages associated with this star system.
	public JsonShipMessage[] aShipMessages { get; set; }
}
