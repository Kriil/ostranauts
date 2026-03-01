using System;
using System.Collections.Generic;

// Save payload for a live CondOwner instance.
// CondOwner means any runtime thing that can carry Conditions: items, crew,
// ships, rooms, and similar entities. This is save-state, not the base
// definition from StreamingAssets/data/condowners.
public class JsonCondOwnerSave
{
	// Runtime instance id plus the definition id used to rebuild the object.
	public string strID { get; set; }

	public string strCODef { get; set; }

	public bool bAlive { get; set; }

	// Condition ids and rule ids currently attached to this owner.
	// Likely these ids map back to data/conditions and data/condrules by strName.
	public string[] aConds { get; set; }

	public int[] aCondReveals { get; set; }

	public string[] aCondRules { get; set; }

	public string strPersistentCT { get; set; }

	public string strPersistentCO { get; set; }

	public string strSourceCO { get; set; }

	public string strSourceInteract { get; set; }

	public string strCondID { get; set; }

	public string strLastSocial { get; set; }

	public int inventoryX { get; set; }

	public int inventoryY { get; set; }

	public string[] aMessages { get; set; }

	public string[] aMsgColors { get; set; }

	public JsonLogMessage[] aMessages2 { get; set; }

	public string[] aCondZeroes { get; set; }

	public double fDGasTemp { get; set; }

	public string[] mapDGasMols { get; set; }

	public double fLastICOUpdate { get; set; }

	// Pending interaction queue serialized for AI/player continuation after load.
	public JsonInteractionSave[] aQueue { get; set; }

	public ReplyThread[] aReplies { get; set; }

	public string[] aAttackIAs { get; set; }

	public int nDestTile { get; set; }

	public string strDestCO { get; set; }

	public string strDestShip { get; set; }

	public string strIdleAnim { get; set; }

	public string strContext { get; set; }

	public string strBodyType { get; set; }

	public string[] aFaceParts { get; set; }

	public Dictionary<string, double> dictRecentlyTried { get; set; }

	public Dictionary<string, double> dictRememberScores { get; set; }

	public string[] aRememberIAs { get; set; }

	public JsonChargenStack cgs { get; set; }

	public JsonSocial social { get; set; }

	public string strSlotName { get; set; }

	public string[] aStack { get; set; }

	public string[] aLot { get; set; }

	public JsonTicker[] aTickers { get; set; }

	public JsonPledgeSave[] aPledges { get; set; }

	public string[] aMyShips { get; set; }

	public string[] aFactions { get; set; }

	public string strComp { get; set; }

	public string strIMGPreview { get; set; }

	public string strFriendlyName { get; set; }

	public string strRegIDLast { get; set; }

	public double fMSRedamageAmount { get; set; }

	public override string ToString()
	{
		return this.strID;
	}

	public JsonCondHistory[] mapIAHist2;
}
