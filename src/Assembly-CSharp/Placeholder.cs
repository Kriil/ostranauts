using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

public class Placeholder : MonoBehaviour
{
	private CondOwner CoUs
	{
		get
		{
			if (this._coUs == null)
			{
				this._coUs = base.GetComponent<CondOwner>();
			}
			return this._coUs;
		}
	}

	private void Update()
	{
		if (this.dfEpochLastCheck <= 0.0)
		{
			this.dfEpochLastCheck = StarSystem.fEpoch;
		}
		if (StarSystem.fEpoch - this.dfEpochLastCheck > this.dfCheckPeriod)
		{
			this.ReTask();
			this.dfEpochLastCheck = StarSystem.fEpoch;
		}
	}

	private void ReTask()
	{
		if (this.CoUs.GetInteractionCurrent() == null)
		{
			if (this.strInstallIAName == null || this.strInstallIATitle == null)
			{
				Interaction interaction = DataHandler.GetInteraction(this.strInstallIA, null, true);
				this.strInstallIAName = interaction.strName;
				this.strInstallIATitle = interaction.strTitle;
				DataHandler.ReleaseTrackedInteraction(interaction);
			}
			Task2 task = new Task2();
			task.strDuty = "Construct";
			task.strInteraction = (this.strInstallIAName ?? string.Empty);
			task.strName = (this.strInstallIATitle ?? string.Empty);
			task.strTargetCOID = this.CoUs.strID;
			if (!CrewSim.objInstance.workManager.AddTask(task, 1))
			{
				if (this.dfCheckPeriod < 30.0)
				{
					this.dfCheckPeriod += 1.0;
				}
			}
			else
			{
				this.dfCheckPeriod = (double)(2.5f + UnityEngine.Random.Range(-0.5f, 0.5f));
			}
		}
	}

	public void Init(CondOwner coInstalledCO, CondOwner coActionCO, string strInstallIA)
	{
		this.strInstallIA = strInstallIA;
		this.strInstalledCO = coInstalledCO.strCODef;
		this.strActionCO = coActionCO.strCODef;
		this.strPersistentCO = coActionCO.strPersistentCO;
		this.strPersistentCT = coActionCO.strPersistentCT;
		this.CoUs.strPlaceholderInstallReq = coActionCO.strName;
		this.CoUs.strPlaceholderInstallFinish = coInstalledCO.strName;
		this.CoUs.strPersistentCO = coActionCO.strPersistentCO;
		this.CoUs.strPersistentCT = coActionCO.strPersistentCT;
		this.dfCheckPeriod += (double)UnityEngine.Random.Range(-0.5f, 0.5f);
	}

	public void Cancel(CondOwner coOwner = null)
	{
		CondOwner component = base.GetComponent<CondOwner>();
		Ship objShip = component.RemoveFromCurrentHome(false);
		List<CondOwner> list = component.GetLotCOs(false);
		Func<int[], int[]> sortingProvider = null;
		if (coOwner != null)
		{
			sortingProvider = delegate(int[] unsortedList)
			{
				if (coOwner == null || coOwner.tf == null || objShip == null)
				{
					return unsortedList;
				}
				return (from x in unsortedList
				orderby Vector3.Distance(coOwner.tf.position.ToVector2(), objShip.GetWorldCoordsAtTileIndex1(x))
				select x).ToArray<int>();
			};
		}
		foreach (CondOwner condOwner in list)
		{
			component.RemoveLotCO(condOwner);
			CondOwner condOwner2 = component.DropCO(condOwner, false, objShip, 0f, 0f, true, sortingProvider);
			if (condOwner2 != null)
			{
				objShip.AddCO(condOwner2, true);
			}
		}
		list.Clear();
		list = null;
		if (CrewSim.GetBracketTarget() == component)
		{
			CrewSim.objInstance.SetBracketTarget(null, false, false);
		}
		component.Destroy();
	}

	public string strInstalledCO;

	public string strActionCO;

	public string strPersistentCO;

	public string strPersistentCT;

	public string strInstallIA;

	public string strInstallIAName;

	public string strInstallIATitle;

	private double dfEpochLastCheck = -1.0;

	private double dfCheckPeriod = 2.5;

	private CondOwner _coUs;
}
