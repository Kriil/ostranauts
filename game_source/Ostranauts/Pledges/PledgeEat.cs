using System;

namespace Ostranauts.Pledges
{
	public class PledgeEat : BasePledgeFindItem
	{
		private CondTrigger Food
		{
			get
			{
				if (this._food == null)
				{
					this._food = DataHandler.GetCondTrigger("TIsFood");
				}
				return this._food;
			}
		}

		protected override CondTrigger EmergencyConditions
		{
			get
			{
				return DataHandler.GetCondTrigger("TIsNeedsToEat");
			}
		}

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
			if (base.Us.ship == null)
			{
				return false;
			}
			if (base.Us.HasCond("IsSeekFoodCooldown") || base.Us.HasCond("IsInCombat"))
			{
				return false;
			}
			base.Us.AddCondAmount("IsSeekFoodCooldown", 1.0, 0.0, 0f);
			bool flag = base.Us.HasCond("DcFood01");
			bool flag2 = base.Us.HasCond("DcSatiety02") || base.Us.HasCond("DcSatiety03") || base.Us.HasCond("DcSatiety04");
			if (!flag && !flag2)
			{
				CondOwner condOwner = base.FindItem(base.Us, this.Food);
				if (condOwner != null)
				{
					Interaction interaction = DataHandler.GetInteraction("SeekFoodDirect", null, false);
					base.Us.QueueInteraction(condOwner, interaction, true);
					return true;
				}
			}
			return false;
		}

		private CondTrigger _food;
	}
}
