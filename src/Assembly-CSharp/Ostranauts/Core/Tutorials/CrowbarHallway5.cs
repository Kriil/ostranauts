using System;

namespace Ostranauts.Core.Tutorials
{
	public class CrowbarHallway5 : TutorialBeat
	{
		public CrowbarHallway5()
		{
			this.SetPersistentRef("IsCrowbarHallwayDoor2", this.crowbarHallwayDoorFinal);
		}

		public override string ObjectiveName
		{
			get
			{
				return "Pry Open The Door";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "There's an unpowered door connecting back to the rest of K-LEG. Pry it open with the crowbar.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Civic duty performed.";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.crowbarHallwayDoorFinal.CO;
			}
		}

		public override void Process()
		{
			if (this.crowbarHallwayDoorFinal.CO && CrewSim.coPlayer && this.crowbarHallwayDoorFinal.CO.HasCond("IsOpen"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		private UniqueIDCOPair crowbarHallwayDoorFinal = new UniqueIDCOPair();
	}
}
