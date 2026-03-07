using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit8 : TutorialBeat
	{
		public HallwayConduit8()
		{
			this.SetPersistentRef("IsTutorialHallwayRack", this.rack);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Take The Goods";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Shift-click the rack's contents to move each item from the rack to your inventory.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Weld-done!";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit9";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.rack.CO;
			}
		}

		public override void Process()
		{
			if (this.rack.CO)
			{
				List<CondOwner> cos = this.rack.CO.GetCOs(true, null);
				if (cos.Count == 0)
				{
					base.Finished = true;
				}
			}
			base.Process();
		}

		public UniqueIDCOPair rack = new UniqueIDCOPair();
	}
}
