using System;

// Top-level savegame payload.
// This bundles the world snapshot: ships, loose CondOwners, star system state,
// player references, economy state, jobs, plots, and manager save data.
public class JsonGameSave
{
	// Save slot/display name plus the main world-state collections.
	public string strName { get; set; }

	public JsonShip[] aShips { get; set; }

	public JsonCondOwnerSave[] aCOs { get; set; }

	// Current player ship and player CondOwner ids used to restore focus/ownership after load.
	public string strShip { get; set; }

	public string strPlayerCO { get; set; }

	public string strVersion { get; set; }

	public JsonStarSystemSave objSystem { get; set; }

	public JsonLedgerLI[] aLIs { get; set; }

	public float fTotalGameSec { get; set; }

	public float fTotalGameSecUnscaled { get; set; }

	public JsonCompany jComp { get; set; }

	public JsonObjective[] aObjectives { get; set; }

	public string[] subscribedShips { get; set; }

	public JsonAIShipManagerSave objAIShipManager { get; set; }

	public JsonRacingManagerSave objRacingManager { get; set; }

	public JsonMarketSave objMarketSave { get; set; }

	public Task2[] aTasksUnclaimed { get; set; }

	public JsonJobSave[] aJobs { get; set; }

	public JsonPlotSave[] aPlots { get; set; }

	public JsonPlotSave[] aPlotsOld { get; set; }

	public string[] aCustomInfos { get; set; }
}
