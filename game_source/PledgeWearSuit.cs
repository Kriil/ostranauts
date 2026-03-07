using System;
using System.Collections.Generic;

public class PledgeWearSuit : Pledge2
{
	public override bool Init(CondOwner coUs, JsonPledge jpIn, CondOwner coThem = null)
	{
		return base.Init(coUs, jpIn, coUs);
	}

	public override bool Init(string strUs, JsonPledge jpIn, string strThem = null)
	{
		return base.Init(strUs, jpIn, strUs);
	}

	public override bool Do()
	{
		if (base.Us == null)
		{
			return false;
		}
		if (this.Finished())
		{
			Task2 queueTask = this.GetQueueTask();
			if (queueTask != null)
			{
				CrewSim.objInstance.workManager.ClaimTaskDirect(queueTask.GetIA());
			}
			return true;
		}
		if (!base.Triggered())
		{
			return false;
		}
		if (base.Us.ship == null)
		{
			return false;
		}
		if (base.Us.HasCond("IsSeekHelmetCooldown") || base.Us.HasCond("IsSeekSuitCooldown"))
		{
			return false;
		}
		base.Us.AddCondAmount("IsPledgeWearSuitDone", 1.0, 0.0, 0f);
		if (!PledgeWearSuit.ctWearingSuit.Triggered(base.Us, null, true))
		{
			CondOwner condOwner = PledgeWearSuit.FindItem(base.Us, PledgeWearSuit.ctSuit);
			if (condOwner != null)
			{
				CondOwner objCOParent = condOwner.objCOParent;
				bool flag = false;
				while (objCOParent != null)
				{
					if (objCOParent.HasCond("IsHuman"))
					{
						flag = true;
						break;
					}
					objCOParent = objCOParent.objCOParent;
				}
				if (flag)
				{
					List<string> list = new List<string>();
					foreach (KeyValuePair<string, JsonSlotEffects> keyValuePair in condOwner.mapSlotEffects)
					{
						if (keyValuePair.Key.Contains("shirt_"))
						{
							list.Add(keyValuePair.Key);
							if (keyValuePair.Value != null && keyValuePair.Value.aSlotsSecondary != null)
							{
								list.AddRange(keyValuePair.Value.aSlotsSecondary);
							}
							break;
						}
					}
					Slots component = base.Us.GetComponent<Slots>();
					foreach (string strSlot in list)
					{
						Slot slot = component.GetSlot(strSlot);
						if (slot != null)
						{
							foreach (CondOwner condOwner2 in slot.aCOs)
							{
								if (!(condOwner2 == null))
								{
									component.UnSlotItem(condOwner2, false);
									base.Us.DropCO(condOwner2, false, base.Us.ship, 0f, 0f, true, null);
								}
							}
						}
					}
				}
				Interaction interaction = DataHandler.GetInteraction("EquipItem", null, false);
				base.Us.QueueInteraction(condOwner, interaction, true);
				return true;
			}
			base.Us.AddCondAmount("IsSeekSuitCooldown", 1.0, 0.0, 0f);
		}
		if (!base.Us.HasCond("IsAirtight"))
		{
			CondOwner condOwner3 = PledgeWearSuit.FindItem(base.Us, PledgeWearSuit.ctHelmet);
			if (condOwner3 != null)
			{
				Interaction interaction2 = DataHandler.GetInteraction("EquipItem", null, false);
				base.Us.QueueInteraction(condOwner3, interaction2, true);
				return true;
			}
			base.Us.AddCondAmount("IsSeekHelmetCooldown", 1.0, 0.0, 0f);
		}
		return false;
	}

	public void SetQueueTask(Task2 task)
	{
		if (base.Us == null || task == null)
		{
			return;
		}
		base.Us.ApplyGPMChanges(new string[]
		{
			"PledgeData,WearSuitQueue," + task.strInteraction + "|" + task.strTargetCOID
		});
	}

	private Task2 GetQueueTask()
	{
		if (base.Us == null)
		{
			return null;
		}
		string gpminfo = base.Us.GetGPMInfo("PledgeData", "WearSuitQueue");
		if (string.IsNullOrEmpty(gpminfo) || gpminfo.IndexOf("|") < 0)
		{
			return null;
		}
		string[] array = gpminfo.Split(new char[]
		{
			'|'
		});
		return CrewSim.objInstance.workManager.GetTask(array[1], array[0]);
	}

	public static CondOwner FindItem(CondOwner coUs, CondTrigger ctItemType)
	{
		if (coUs == null || ctItemType == null || ctItemType.IsBlank())
		{
			return null;
		}
		CondOwner result = null;
		List<CondOwner> cos = coUs.GetCOs(false, ctItemType);
		if (cos != null && cos.Count > 0)
		{
			return cos[0];
		}
		if (coUs.ship == null)
		{
			return null;
		}
		List<Ship> list = new List<Ship>();
		bool flag = coUs.HasCond("IsEmergencyOverride");
		bool flag2 = coUs.Company == null || coUs.Company.mapRoster[coUs.strID].bShoreLeave;
		if (flag || flag2)
		{
			foreach (Ship ship in coUs.ship.GetAllDockedShips())
			{
				if (flag || (flag2 && coUs.OwnsShip(ship.strRegID)))
				{
					list.Add(ship);
				}
			}
		}
		list.Insert(0, coUs.ship);
		foreach (Ship ship2 in list)
		{
			cos = coUs.ship.GetCOs(ctItemType, true, false, false);
			foreach (CondOwner condOwner in cos)
			{
				if (PledgeWearSuit.ctNotCarried.Triggered(condOwner, null, true))
				{
					return condOwner;
				}
			}
		}
		return result;
	}

	public static void PledgeEquipItem(CondOwner coUs, CondOwner coItem, string strPledge)
	{
		if (coUs == null || coItem == null)
		{
			return;
		}
		JsonPledge pledge = DataHandler.GetPledge(strPledge);
		if (pledge == null)
		{
			return;
		}
		Pledge2 pledge2 = PledgeFactory.Factory(coUs, pledge, coItem);
		coUs.AddPledge(pledge2);
	}

	public static CondTrigger ctNotCarried
	{
		get
		{
			if (PledgeWearSuit._ctNotCarried == null)
			{
				PledgeWearSuit._ctNotCarried = DataHandler.GetCondTrigger("TIsNotCarried");
			}
			return PledgeWearSuit._ctNotCarried;
		}
	}

	public static CondTrigger ctHelmet
	{
		get
		{
			if (PledgeWearSuit._ctHelmet == null)
			{
				PledgeWearSuit._ctHelmet = DataHandler.GetCondTrigger("TIsHelmet");
			}
			return PledgeWearSuit._ctHelmet;
		}
	}

	public static CondTrigger ctSuit
	{
		get
		{
			if (PledgeWearSuit._ctSuit == null)
			{
				PledgeWearSuit._ctSuit = DataHandler.GetCondTrigger("TIsSpaceSuit");
			}
			return PledgeWearSuit._ctSuit;
		}
	}

	public static CondTrigger ctWearingSuit
	{
		get
		{
			if (PledgeWearSuit._ctWearingSuit == null)
			{
				PledgeWearSuit._ctWearingSuit = DataHandler.GetCondTrigger("TIsWearingSpaceSuit");
			}
			return PledgeWearSuit._ctWearingSuit;
		}
	}

	private static CondTrigger _ctNotCarried;

	private static CondTrigger _ctHelmet;

	private static CondTrigger _ctSuit;

	private static CondTrigger _ctWearingSuit;
}
