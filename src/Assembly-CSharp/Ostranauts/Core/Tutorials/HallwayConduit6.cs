using System;
using System.Collections;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit6 : TutorialBeat
	{
		public HallwayConduit6()
		{
			this.SetPersistentRef("IsTutorialHallwayDoor", this.door);
			CrewSim.coPlayer.StartCoroutine(this.WaitForCompletedInstall());
		}

		public override string ObjectiveName
		{
			get
			{
				return "Open The Door";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Installing the Conduit restored power to the door. Search the room beyond for your reward.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Threshold crossed.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit7";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.door.CO;
			}
		}

		public override void MakeObjective()
		{
		}

		public IEnumerator WaitForCompletedInstall()
		{
			yield return new WaitUntil(() => this.door.CO && this.door.CO.HasCond("IsPowered"));
			base.MakeObjective();
			yield break;
		}

		public void ListenForDoorInteraction(Interaction interaction)
		{
			if (interaction.objThem.strID == this.door.ID)
			{
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			CondOwner coPlayer = CrewSim.coPlayer;
			coPlayer.OnQueueInteraction = (Action<Interaction>)Delegate.Combine(coPlayer.OnQueueInteraction, new Action<Interaction>(this.ListenForDoorInteraction));
		}

		public override void RemoveAllListeners()
		{
			CondOwner coPlayer = CrewSim.coPlayer;
			coPlayer.OnQueueInteraction = (Action<Interaction>)Delegate.Remove(coPlayer.OnQueueInteraction, new Action<Interaction>(this.ListenForDoorInteraction));
		}

		public static Interaction PlayerInstallInteraction;

		public UniqueIDCOPair door = new UniqueIDCOPair();
	}
}
