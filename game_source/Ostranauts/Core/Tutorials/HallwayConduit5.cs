using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit5 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Complete Installation";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Installation takes time. Remember you can speed up time when attempting long tasks.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Time manipulated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit6";
			}
		}

		public override void MakeObjective()
		{
			CrewSim.coPlayer.StartCoroutine(this.DelayStartSoUserWatchesInstallation());
			this.SetPersistentRef("IsTutorialHallwayDoor", this.door);
		}

		public IEnumerator DelayStartSoUserWatchesInstallation()
		{
			yield return new WaitForSeconds(3.5f);
			this.startingTimescale = Mathf.Min(1f, Time.timeScale);
			CrewSim.OnTimeScaleUpdated.AddListener(new UnityAction(this.ListenForTimeScaleChange));
			base.MakeObjective();
			yield break;
		}

		public void ListenForTimeScaleChange()
		{
			if (Time.timeScale > this.startingTimescale)
			{
				base.Finished = true;
			}
		}

		public override void Process()
		{
			if (this.door.CO && this.door.CO.HasCond("IsPowered"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		public override void RemoveAllListeners()
		{
			CrewSim.OnTimeScaleUpdated.RemoveListener(new UnityAction(this.ListenForTimeScaleChange));
		}

		public float startingTimescale;

		public UniqueIDCOPair door = new UniqueIDCOPair();
	}
}
