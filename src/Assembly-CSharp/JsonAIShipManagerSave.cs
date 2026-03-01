using System;

[Serializable]
// Save payload for the AI ship traffic manager.
// Likely restores transit scheduling, scavenger counts, and active NPC ships
// that move through the current star system.
public class JsonAIShipManagerSave
{
	// Last ATC/traffic script or state token processed by the manager.
	public string strATCLast { get; set; }

	// Absolute in-game times for the next spawn/schedule checks.
	public double fTimeOfNextTransit { get; set; }

	public double fTimeOfNextScav { get; set; }

	public double fTimeOfNextHauler { get; set; }

	public int nScavSpawnsRemaining { get; set; }

	public double fTimeOfNextPassShip { get; set; }

	public double fTimeOfNextTradeCheck { get; set; }

	// Likely cached ferry/transit entries used to rebuild route state on load.
	public JsonFerryInfo[] aFIs;

	// Live AI ship save payloads owned by this manager.
	public JsonAIShipSave[] aAIShips;
}
