using System;

[Serializable]
// Save payload for the racing subsystem.
// Stores best lap history plus the current league/tournament state.
public class JsonRacingManagerSave
{
	// Per-track or per-event personal bests shown in racing UI.
	public JsonLapTime[] aPersonalBestTimes { get; set; }

	// Current racing league standings/progression.
	public JsonRacingLeagueSave objLeagueSave { get; set; }
}
