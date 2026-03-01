using System;

public class PledgeRepeat : Pledge2
{
	public override bool IsEmergency()
	{
		return !(base.Us == null) && !base.Us.HasCond("IsAIManual") && base.IsEmergency();
	}

	public override bool Do()
	{
		if (base.Us == null || base.Us.HasCond("IsAIManual"))
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
		if (this.jp.bThemForgetOnDo)
		{
			base.ForgetThem();
		}
		if (this.Them == null)
		{
			if (interaction.strThemType == Interaction.TARGET_SELF)
			{
				this.Them = base.Us;
			}
			else
			{
				if (interaction.PSpecTestThem != null)
				{
					this.Them = base.Us.ship.GetCOFirstOccurrence(interaction.PSpecTestThem, base.Us.pspec, this.jp.bThemAllowDocked);
				}
				else
				{
					this.Them = base.Us.ship.GetCOFirstOccurrence(interaction.CTTestThem, true, this.jp.bThemAllowDocked, false);
				}
				if (this.Them != null)
				{
					if (!interaction.Triggered(base.Us, this.Them, false, false, true, true, null))
					{
						return false;
					}
					base.Us.QueueInteraction(this.Them, interaction, false);
					return true;
				}
			}
		}
		if (this.Them == null || !interaction.Triggered(base.Us, this.Them, false, false, true, true, null))
		{
			return false;
		}
		base.Us.QueueInteraction(this.Them, interaction, false);
		return true;
	}
}
