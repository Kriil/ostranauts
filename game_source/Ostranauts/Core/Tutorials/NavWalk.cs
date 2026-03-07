using System;

namespace Ostranauts.Core.Tutorials
{
	public class NavWalk : TutorialBeat
	{
		public NavWalk()
		{
			if (CrewSimTut.playerShipNavStationRef)
			{
				this.nav = CrewSimTut.playerShipNavStationRef;
			}
			CrewSimTut.BeginTutorialBeat<HallwayConduitStart>();
			CrewSimTut.BeginTutorialBeat<CrowbarHallwayStart>();
		}

		public override string ObjectiveName
		{
			get
			{
				return "Visit Your Ship";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Walk through OKLG station. Board your ship docked at the airlock.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Ship Found.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "SelectMTT";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.nav;
			}
		}

		public override void Process()
		{
			if (CrewSimTut.playerShipRef != null && CrewSim.coPlayer.ship == CrewSimTut.playerShipRef)
			{
				base.Finished = true;
			}
			base.Process();
		}

		private CondOwner nav;
	}
}
