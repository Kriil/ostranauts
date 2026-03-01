using System;

// Save payload for one live Interaction in a queue or reply thread.
// This preserves action-chain state so AI/player tasks can resume after loading.
public class JsonInteractionSave
{
	// Empty ctor for deserialization.
	public JsonInteractionSave()
	{
	}

	// Captures the current runtime interaction state using actor ids instead of live references.
	public JsonInteractionSave(Interaction ia)
	{
		this.strName = ia.strName;
		this.strChainStart = ia.strChainStart;
		this.strChainOwner = ia.strChainOwner;
		this.bLogged = ia.bLogged;
		this.bRaisedUI = ia.bRaisedUI;
		this.bManual = ia.bManual;
		this.bTryWalk = ia.bTryWalk;
		this.bCancel = ia.bCancel;
		this.bRetestItems = ia.bRetestItems;
		if (ia.objUs != null)
		{
			this.objUs = ia.objUs.strID;
		}
		if (ia.objThem != null)
		{
			this.objThem = ia.objThem.strID;
		}
		if (ia.obj3rd != null)
		{
			this.obj3rd = ia.obj3rd.strID;
		}
	}

	// Interaction definition id plus chain metadata for multi-step task flows.
	public string strName { get; set; }

	public string strChainStart { get; set; }

	public string strChainGUID { get; set; }

	public string strChainOwner { get; set; }

	public bool bLogged { get; set; }

	public bool bRaisedUI { get; set; }

	public bool bTryWalk { get; set; }

	public bool bCancel { get; set; }

	public bool bRetestItems { get; set; }

	public bool bManual { get; set; }

	public string objUs { get; set; }

	public string objThem { get; set; }

	public string obj3rd { get; set; }

	public string strPlot { get; set; }

	// Contract/item arrays look like deferred references for items reserved, used, removed, or sought by this action.
	public string[] aLootItemGiveContract { get; set; }

	public string[] aLootItemUseContract { get; set; }

	public string[] aLootItemRemoveContract { get; set; }

	public string[] aLootItemTakeContract { get; set; }

	public string[] aSeekItemsForContract { get; set; }

	public string[] aDependents { get; set; }

	public string[] aSocialPrereqsFound { get; set; }

	public override string ToString()
	{
		return this.strName;
	}

	// Shallow clone used when copying queued actions without rebuilding the whole payload.
	public JsonInteractionSave Clone()
	{
		return base.MemberwiseClone() as JsonInteractionSave;
	}
}
