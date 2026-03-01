using System;
using System.Collections.Generic;

public class PledgeCrime : Pledge2
{
	public override bool Do()
	{
		if (base.Us == null || base.Us.ship == null)
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
		if (interaction.CTTestUs != null && !interaction.CTTestUs.Triggered(base.Us, null, true))
		{
			return false;
		}
		Pledge2 pledge = this.FindCriminal(interaction);
		if (pledge == null || base.Us.HasPledge(pledge))
		{
			return false;
		}
		interaction.objUs = base.Us;
		interaction.objThem = pledge.Them;
		interaction.ApplyChain(null);
		return true;
	}

	private Pledge2 FindCriminal(Interaction ia)
	{
		List<CondOwner> people = base.Us.ship.GetPeople(true);
		people.Remove(base.Us);
		MathUtils.ShuffleList<CondOwner>(people);
		foreach (CondOwner condOwner in people)
		{
			if (Visibility.IsCondOwnerLOSVisibleFromCo(base.Us, condOwner))
			{
				this.Them = condOwner;
				bool flag = base.Triggered();
				this.Them = null;
				if (flag)
				{
					JsonPledge pledge = DataHandler.GetPledge(ia.strPledgeAdd);
					Pledge2 pledge2 = PledgeFactory.Factory(base.Us, pledge, condOwner);
					if (!base.Us.HasPledge(pledge2))
					{
						return pledge2;
					}
				}
			}
		}
		return null;
	}

	public override bool IsEmergency()
	{
		Interaction interaction = DataHandler.GetInteraction(this.jp.strIATrigger, null, false);
		return interaction != null && (interaction.CTTestUs == null || interaction.CTTestUs.Triggered(base.Us, null, true)) && this.FindCriminal(interaction) != null;
	}

	public override CondOwner Them
	{
		get
		{
			return base.Them;
		}
		set
		{
			if (value != null)
			{
				this.strThemID = value.strID;
			}
			else
			{
				this.strThemID = null;
			}
			this.objThemTemp = value;
		}
	}
}
