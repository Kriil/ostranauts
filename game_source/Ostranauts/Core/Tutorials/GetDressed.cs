using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Tutorials
{
	public class GetDressed : TutorialBeat
	{
		public GetDressed()
		{
			this.SetPersistentRef("IsStartingLeftSneaker", this.leftSneaker);
			this.SetPersistentRef("IsStartingRightSneaker", this.rightSneaker);
			this.SetPersistentRef("IsStartingClothes", this.clothes);
			this.allClothes.Add(this.leftSneaker);
			this.allClothes.Add(this.rightSneaker);
			this.allClothes.Add(this.clothes);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Get Dressed";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Move the yellow jumpsuit and shoes on the ground onto your character in the inventory screen.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Dressed and Ready.";
			}
		}

		public override string CTString
		{
			get
			{
				return "TIsTutorialClothesComplete";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "MouseoverObjective";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.ObjectiveSequence();
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

		public CondOwner ObjectiveSequence()
		{
			for (int i = 0; i < this.allClothes.Count; i++)
			{
				if (this.allClothes[i].CO && this.allClothes[i].CO.RootParent(null) != CrewSim.coPlayer)
				{
					return this.allClothes[i].CO;
				}
			}
			return CrewSim.coPlayer;
		}

		public override void Process()
		{
			if (this.clothes.CO && this.leftSneaker.CO && this.rightSneaker.CO && CrewSim.coPlayer && this.clothes.CO.RootParent(null) == CrewSim.coPlayer && this.leftSneaker.CO.RootParent(null) == CrewSim.coPlayer && this.rightSneaker.CO.RootParent(null) == CrewSim.coPlayer)
			{
				base.Finished = true;
			}
			base.Process();
		}

		public UniqueIDCOPair leftSneaker = new UniqueIDCOPair();

		public UniqueIDCOPair rightSneaker = new UniqueIDCOPair();

		public UniqueIDCOPair clothes = new UniqueIDCOPair();

		private List<UniqueIDCOPair> allClothes = new List<UniqueIDCOPair>();
	}
}
