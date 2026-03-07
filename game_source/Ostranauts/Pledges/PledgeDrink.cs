using System;

namespace Ostranauts.Pledges
{
	public class PledgeDrink : BasePledgeFindItem
	{
		private CondTrigger Drinks
		{
			get
			{
				if (this._drinks == null)
				{
					this._drinks = DataHandler.GetCondTrigger("TIsHydratorGood");
				}
				return this._drinks;
			}
		}

		private CondTrigger DrinksEmerg
		{
			get
			{
				if (this._drinksEmerg == null)
				{
					this._drinksEmerg = DataHandler.GetCondTrigger("TIsHydrator");
				}
				return this._drinksEmerg;
			}
		}

		protected override CondTrigger EmergencyConditions
		{
			get
			{
				return DataHandler.GetCondTrigger("TIsDehydrated");
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
			if (base.Us.HasCond("IsSeekDrinkCooldown") || base.Us.HasCond("IsInCombat"))
			{
				return false;
			}
			base.Us.AddCondAmount("IsSeekDrinkCooldown", 1.0, 0.0, 0f);
			if (!base.Us.HasCond("DcHydration01"))
			{
				CondOwner condOwner = base.FindItem(base.Us, this.Drinks);
				if (condOwner == null)
				{
					condOwner = base.FindItem(base.Us, this.DrinksEmerg);
				}
				if (condOwner != null)
				{
					Interaction interaction = DataHandler.GetInteraction("SeekDrink", null, false);
					base.Us.QueueInteraction(condOwner, interaction, true);
					return true;
				}
			}
			return false;
		}

		private CondTrigger _drinks;

		private CondTrigger _drinksEmerg;
	}
}
