using System;
using System.Collections.Generic;

public class PledgeFaction : Pledge2
{
	public override bool Do()
	{
		if (base.Us == null)
		{
			return false;
		}
		if (this.Finished())
		{
			return true;
		}
		Interaction interaction = DataHandler.GetInteraction(this.jp.strIATrigger, null, false);
		if (interaction == null)
		{
			return false;
		}
		List<CondOwner> people = base.Us.ship.GetPeople(true);
		MathUtils.ShuffleList<CondOwner>(people);
		foreach (CondOwner condOwner in people)
		{
			if (!(base.Us == condOwner))
			{
				if (Visibility.IsCondOwnerLOSVisibleFromCo(base.Us, condOwner))
				{
					JsonPersonSpec personSpec = DataHandler.GetPersonSpec("RELFactionFightSafe");
					if (base.Us.pspec != null && personSpec != null && !base.Us.pspec.IsCOMyMother(personSpec, condOwner))
					{
						if (!condOwner.HasCond("IsInCombat"))
						{
							continue;
						}
						bool flag = false;
						foreach (Pledge2 pledge in condOwner.GetPledgesOfType(DataHandler.GetPledge("AICombat")))
						{
							if (base.Us.pspec.IsCOMyMother(personSpec, pledge.Them))
							{
								flag = true;
								CondOwner them = pledge.Them;
								break;
							}
						}
						if (!flag)
						{
							continue;
						}
					}
					float factionScore = base.Us.GetFactionScore(condOwner.GetAllFactions());
					if (JsonFaction.GetReputation(factionScore) == JsonFaction.Reputation.Dislikes)
					{
						this.Them = condOwner;
						if (base.Triggered())
						{
							JsonPledge pledge2 = DataHandler.GetPledge("AICombat");
							Pledge2 pledge3 = PledgeFactory.Factory(base.Us, pledge2, condOwner);
							base.Us.AddPledge(pledge3);
							base.Us.QueueInteraction(this.Them, interaction, false);
							return true;
						}
						this.Them = null;
						this.objThemTemp = null;
						this.strThemID = null;
					}
				}
			}
		}
		return false;
	}
}
