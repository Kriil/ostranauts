using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Tutorials
{
	public class CollectEquipment : TutorialBeat
	{
		public CollectEquipment()
		{
			this.SetPersistentRef("IsTutorialDormRack", this.rack);
			if (this.rack.CO)
			{
				List<CondOwner> cos = this.rack.CO.GetCOs(true, null);
				for (int i = 0; i < cos.Count; i++)
				{
					if (cos[i].strName == "ItmBackpack03")
					{
						this.tote = cos[i];
					}
					if (cos[i].strName == "OutfitPS01")
					{
						this.suit = cos[i];
					}
					if (cos[i].strName == "OutfitHelmet02")
					{
						this.helmet = cos[i];
					}
				}
			}
		}

		public override string ObjectiveName
		{
			get
			{
				return "Collect Pressure Suit and Helmet";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Open the inventory of the nearby rack. Move the tote into your bag slot, then place the suit and helmet in the tote inventory.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Bag, suit, helmet, all present and correct.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "PickUpToolbox";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.rack.CO;
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			if (CrewSim.coPlayer != null && !CrewSim.coPlayer.HasCond("TutorialStillInDorm"))
			{
				this.NextOverride = "NavWalk";
				return true;
			}
			return false;
		}

		public override void Process()
		{
			if (this.rack.CO && this.tote && this.suit && this.helmet)
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				if (this.tote.slotNow != null && this.tote.slotNow.strName == "back")
				{
					flag = true;
				}
				if (this.helmet.objCOParent == this.tote)
				{
					flag2 = true;
				}
				if (this.suit.objCOParent == this.tote)
				{
					flag3 = true;
				}
				if (flag && flag2 && flag3)
				{
					base.Finished = true;
				}
			}
			base.Process();
		}

		public UniqueIDCOPair rack = new UniqueIDCOPair();

		private CondOwner tote;

		private CondOwner suit;

		private CondOwner helmet;
	}
}
