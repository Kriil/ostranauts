using System;
using Ostranauts.Pledges;

namespace Ostranauts.Core.Tutorials
{
	public class HelmetAtmosphereUnsafe : TutorialBeat
	{
		public HelmetAtmosphereUnsafe()
		{
			if (this._ctWearsSuit == null)
			{
				this._ctWearsSuit = DataHandler.GetCondTrigger("TIsAirtight");
			}
		}

		public override string ObjectiveName
		{
			get
			{
				return "Helmet Atmosphere Unsafe";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Find a room with safe levels of O2 & CO2 and remove your helmet.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Helmet Vented.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return string.Empty;
			}
		}

		public override void Process()
		{
			if (this._ctWearsSuit.Triggered(CrewSim.coPlayer, null, true))
			{
				return;
			}
			if (CrewSim.coPlayer.currentRoom != null && PledgeSurviveO2.CoHasO2(CrewSim.coPlayer.currentRoom.CO) && PledgeSurviveCO2.CoHasSafeCO2Lvl(CrewSim.coPlayer.currentRoom.CO))
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialHelmetAtmoShow");
				CrewSim.coPlayer.AddCondAmount("TutorialHelmetAtmoComplete", 1.0, 0.0, 0f);
				base.Finished = true;
			}
			base.Process();
		}

		public CondTrigger _ctWearsSuit;
	}
}
