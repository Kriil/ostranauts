using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Objectives;
using UnityEngine;

namespace Ostranauts.Core.Tutorials
{
	public abstract class TutorialBeat
	{
		public TutorialBeat()
		{
			this.AddInitialListeners();
			if (!this.CheckUserPreSatisfied())
			{
				this.MakeObjective();
			}
			else
			{
				this.Finished = true;
				this.OnFinish();
				this.SetNext();
			}
		}

		public bool Finished { get; set; }

		public Objective AssociatedObjective { get; set; }

		public abstract string ObjectiveName { get; }

		public abstract string ObjectiveDesc { get; }

		public abstract string ObjectiveDescComplete { get; }

		public virtual string NextDefault
		{
			get
			{
				return string.Empty;
			}
		}

		public virtual string NextOverride { get; set; }

		public virtual string NextBeat
		{
			get
			{
				if (string.IsNullOrEmpty(this.NextOverride))
				{
					return this.NextDefault;
				}
				return this.NextOverride;
			}
			set
			{
				this.NextOverride = value;
			}
		}

		public virtual string CTString
		{
			get
			{
				return null;
			}
		}

		public virtual CondOwner COTarget
		{
			get
			{
				return CrewSim.coPlayer;
			}
		}

		public List<TutorialBeat> CurrentBeats
		{
			get
			{
				return CrewSimTut.TutorialBeats;
			}
		}

		public virtual bool MakesNextStep(out TutorialBeat nextBeat)
		{
			nextBeat = null;
			return false;
		}

		public virtual float TutorialDelayTime
		{
			get
			{
				return 1f;
			}
		}

		public virtual void MakeObjective()
		{
			if (CrewSim.coPlayer && this.TutorialDelayTime > 0f)
			{
				CrewSim.coPlayer.StartCoroutine(this.DelayObjective());
			}
			else
			{
				this.MakeObjectiveInternal();
			}
		}

		public virtual void AddInitialListeners()
		{
		}

		public virtual void RemoveAllListeners()
		{
		}

		public virtual bool SetPersistentRef(string s, UniqueIDCOPair uniqueIDCOPair)
		{
			return CrewSimTut.UniqueToStrID.TryGetValue(s, out uniqueIDCOPair.ID);
		}

		public virtual bool SetPersistentRef(string s, ref string strID, ref CondOwner condOwner)
		{
			return CrewSimTut.UniqueToStrID.TryGetValue(s, out strID) && DataHandler.mapCOs.TryGetValue(strID, out condOwner);
		}

		public IEnumerator DelayObjective()
		{
			yield return new WaitForSeconds(this.TutorialDelayTime);
			this.MakeObjectiveInternal();
			yield break;
		}

		public void MakeObjectiveInternal()
		{
			this.AssociatedObjective = Objective.MakeTutorialObjective(this);
		}

		public virtual bool CheckUserPreSatisfied()
		{
			return false;
		}

		public virtual void Process()
		{
			if (this.Finished)
			{
				this.OnFinish();
				this.SetNext();
			}
		}

		public virtual void SetNext()
		{
			if (string.IsNullOrEmpty(this.NextBeat))
			{
				return;
			}
			Type type = Type.GetType("Ostranauts.Core.Tutorials." + this.NextBeat);
			if (type != null && type.IsSubclassOf(typeof(TutorialBeat)))
			{
				TutorialBeat item = Activator.CreateInstance(type) as TutorialBeat;
				this.CurrentBeats.Add(item);
			}
		}

		public virtual void OnFinish()
		{
			this.RemoveAllListeners();
		}
	}
}
