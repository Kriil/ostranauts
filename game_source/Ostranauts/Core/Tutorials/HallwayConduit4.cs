using System;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit4 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Position Placeholder Conduit";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Overlay the Placeholder Conduit in its intended location, then left-click to begin installation.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Installing begun.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit5";
			}
		}

		public void ListenForInstallConduit(Interaction interaction)
		{
			if (interaction.strName == "ACTConduit01InstallAllow")
			{
				interaction.objUs.SetCondAmount("IsTutorialHallwayConduit", 1.0, 0.0);
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			CondOwner coPlayer = CrewSim.coPlayer;
			coPlayer.OnQueueInteraction = (Action<Interaction>)Delegate.Combine(coPlayer.OnQueueInteraction, new Action<Interaction>(this.ListenForInstallConduit));
		}

		public override void RemoveAllListeners()
		{
			CondOwner coPlayer = CrewSim.coPlayer;
			coPlayer.OnQueueInteraction = (Action<Interaction>)Delegate.Remove(coPlayer.OnQueueInteraction, new Action<Interaction>(this.ListenForInstallConduit));
		}
	}
}
