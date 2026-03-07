using System;

// Live job/contract save payload.
// This captures one offered or accepted job instance, including actor ids,
// route endpoints, generated text/items, timers, and payout multipliers.
public class JsonJobSave
{
	// Links this runtime instance back to the `JsonJob` template id.
	public string strJobName { get; set; }

	public string strClientID { get; set; }

	public string strThemID { get; set; }

	public string str3rdID { get; set; }

	public string strRegIDPickup { get; set; }

	public string strRegIDDropoff { get; set; }

	public string strTxt1 { get; set; }

	public string strJobItems { get; set; }

	public string strFailReasons { get; set; }

	public double fEpochOfferExpired { get; set; }

	public double fEpochExpired { get; set; }

	public double fCostContract { get; set; }

	public double fPayout { get; set; }

	public double fItemValue { get; set; }

	public double fPayoutMult { get; set; }

	public double fTimeMult { get; set; }

	public bool bTaken { get; set; }

	public bool bInvalid { get; set; }

	// Creates a detached copy for save duplication or temporary edits.
	public JsonJobSave Clone()
	{
		return new JsonJobSave
		{
			strJobName = this.strJobName,
			strClientID = this.strClientID,
			strThemID = this.strThemID,
			str3rdID = this.str3rdID,
			strRegIDPickup = this.strRegIDPickup,
			strRegIDDropoff = this.strRegIDDropoff,
			strTxt1 = this.strTxt1,
			strJobItems = this.strJobItems,
			strFailReasons = this.strFailReasons,
			fEpochOfferExpired = this.fEpochOfferExpired,
			fEpochExpired = this.fEpochExpired,
			fCostContract = this.fCostContract,
			fPayout = this.fPayout,
			fItemValue = this.fItemValue,
			fPayoutMult = this.fPayoutMult,
			fTimeMult = this.fTimeMult,
			bTaken = this.bTaken,
			bInvalid = this.bInvalid
		};
	}

	// Materializes the client-side setup interaction from the underlying job template.
	public Interaction GetInteractionSetupClient()
	{
		if (this.JobTemplate() == null || this.JobTemplate().strIASetupClient == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIASetupClient, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = this.COClient();
		interaction.objThem = this.COThem();
		interaction.obj3rd = this.CO3rd();
		return interaction;
	}

	// Materializes the player-side setup interaction for the actor taking the job.
	public Interaction GetInteractionSetupPlayer(CondOwner coTaker)
	{
		if (coTaker == null || this.JobTemplate() == null || this.JobTemplate().strIASetupPlayer == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIASetupPlayer, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = coTaker;
		interaction.objThem = this.COClient();
		return interaction;
	}

	// Client-side abandon interaction.
	public Interaction GetInteractionAbandonClient()
	{
		if (this.JobTemplate() == null || this.JobTemplate().strIAAbandonClient == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIAAbandonClient, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = this.COClient();
		interaction.objThem = this.COThem();
		interaction.obj3rd = this.CO3rd();
		return interaction;
	}

	// Player-side abandon interaction.
	public Interaction GetInteractionAbandonPlayer(CondOwner coTaker)
	{
		if (coTaker == null || this.JobTemplate() == null || this.JobTemplate().strIAAbandonPlayer == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIAAbandonPlayer, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = coTaker;
		interaction.objThem = coTaker;
		return interaction;
	}

	// Client-side completion interaction.
	public Interaction GetInteractionFinishClient()
	{
		if (this.JobTemplate() == null || this.JobTemplate().strIAFinishClient == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIAFinishClient, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = this.COClient();
		interaction.objThem = this.COThem();
		interaction.obj3rd = this.CO3rd();
		return interaction;
	}

	// Player-side completion interaction.
	public Interaction GetInteractionFinishPlayer(CondOwner coTaker)
	{
		if (coTaker == null || this.JobTemplate() == null || this.JobTemplate().strIAFinishPlayer == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIAFinishPlayer, null, false);
		if (interaction == null)
		{
			return null;
		}
		interaction.objUs = coTaker;
		interaction.objThem = this.COClient();
		return interaction;
	}

	public Interaction GetInteractionDo(CondOwner coTaker, CondOwner coTarget)
	{
		if (coTaker == null || this.JobTemplate() == null || this.JobTemplate().strIADo == null)
		{
			return null;
		}
		Interaction interaction;
		if (this.strJobItems != null && this.strRegIDDropoff == null)
		{
			JsonJobItems jobItems = DataHandler.GetJobItems(this.strJobItems);
			Loot loot = null;
			DataHandler.dictLoot.TryGetValue("TXTJobItemsTemplate", out loot);
			loot.aCOs = (jobItems.aCTsDeliver.Clone() as string[]);
			loot.strName = "JOBITEMS" + this.strJobItems;
			DataHandler.dictLoot[loot.strName] = loot;
			JsonInteraction jsonInteraction = null;
			if (!DataHandler.dictInteractions.TryGetValue(this.JobTemplate().strIADo, out jsonInteraction))
			{
				return null;
			}
			jsonInteraction = jsonInteraction.Clone();
			jsonInteraction.strName += loot.strName;
			jsonInteraction.aLootItms = new string[]
			{
				"give," + loot.strName + ",false,false"
			};
			DataHandler.dictInteractions[jsonInteraction.strName] = jsonInteraction;
			interaction = DataHandler.GetInteraction(jsonInteraction.strName, null, false);
		}
		else
		{
			interaction = DataHandler.GetInteraction(this.JobTemplate().strIADo, null, false);
		}
		if (interaction == null)
		{
			return null;
		}
		if (this.COThem() != coTarget)
		{
			return null;
		}
		if (interaction.Triggered(coTaker, coTarget, false, false, true, true, null))
		{
			interaction.objUs = coTaker;
			interaction.objThem = coTarget;
			return interaction;
		}
		return null;
	}

	public bool CanCOTakeThisJob(CondOwner co)
	{
		if (co == null || this.JobTemplate() == null || this.JobTemplate().strIASetupPlayer == null)
		{
			return false;
		}
		Interaction interaction = DataHandler.GetInteraction(this.JobTemplate().strIASetupPlayer, null, false);
		if (interaction == null)
		{
			return false;
		}
		if (co.GetCondAmount(Ledger.CURRENCY) < this.fCostContract + (double)((int)this.fItemValue))
		{
			this.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_INSUFFICIENTFUNDS", false);
			return false;
		}
		interaction.bVerboseTrigger = true;
		bool result = interaction.Triggered(co, this.COClient(), false, true, false, true, null);
		this.strFailReasons = interaction.FailReasons(true, true, false);
		return result;
	}

	public CondOwner COClient()
	{
		if ((this.coClient == null || this.coClient.tf == null) && this.strClientID != null)
		{
			DataHandler.mapCOs.TryGetValue(this.strClientID, out this.coClient);
		}
		return this.coClient;
	}

	public CondOwner COThem()
	{
		if ((this.coThem == null || this.coThem.tf == null) && this.strThemID != null)
		{
			DataHandler.mapCOs.TryGetValue(this.strThemID, out this.coThem);
		}
		return this.coThem;
	}

	public CondOwner CO3rd()
	{
		if ((this.co3rd == null || this.co3rd.tf == null) && this.str3rdID != null)
		{
			DataHandler.mapCOs.TryGetValue(this.str3rdID, out this.co3rd);
		}
		return this.co3rd;
	}

	public JsonJob JobTemplate()
	{
		if (this.jj == null && this.strJobName != null)
		{
			this.jj = DataHandler.GetJob(this.strJobName);
		}
		return this.jj;
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strJobName,
			" - ",
			this.strClientID,
			"->",
			this.strThemID,
			"; Taken: ",
			this.bTaken
		});
	}

	private CondOwner coClient;

	private CondOwner coThem;

	private CondOwner co3rd;

	private JsonJob jj;
}
