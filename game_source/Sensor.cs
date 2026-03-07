using System;
using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.coUs = base.GetComponent<CondOwner>();
		this.strPoint = string.Empty;
		this.mapTests = new Dictionary<string, string>();
	}

	private void Update()
	{
		if (this.dfEpochLast <= 0.0)
		{
			this.dfEpochLast = StarSystem.fEpoch;
		}
		double num = StarSystem.fEpoch - this.dfEpochLast;
		if (num >= this.dfUpdateInterval)
		{
			this.Run();
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	public void Run()
	{
		if (this.coUs == null || this.coUs.ship == null)
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strPoint, false), null, false, false, list);
		foreach (KeyValuePair<string, string> keyValuePair in this.mapTests)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger(keyValuePair.Key);
			foreach (CondOwner condOwner in list)
			{
				if (condTrigger.Triggered(condOwner, null, true))
				{
					Interaction interaction = DataHandler.GetInteraction(keyValuePair.Value, null, false);
					if (interaction != null && interaction.CTTestUs.Triggered(this.coUs, null, true))
					{
						this.coUs.QueueInteraction(condOwner, interaction, false);
					}
				}
			}
		}
		this.dfEpochLast = StarSystem.fEpoch;
	}

	public void SetData(Dictionary<string, string> gpm)
	{
		if (gpm == null)
		{
			return;
		}
		this.strPoint = gpm["strPoint"];
		string[] aStrings = gpm["mapTests"].Split(new char[]
		{
			','
		});
		this.mapTests = DataHandler.ConvertStringArrayToDict(aStrings, null);
	}

	private CondOwner coUs;

	private string strPoint;

	private Dictionary<string, string> mapTests;

	private double dfEpochLast = -1.0;

	private double dfUpdateInterval = 1.0;
}
