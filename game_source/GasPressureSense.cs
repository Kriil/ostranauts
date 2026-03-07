using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;

public class GasPressureSense : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.co = base.GetComponent<CondOwner>();
		this.strPoint = string.Empty;
		this.strSignalCond = string.Empty;
		this.strInteractionAlarm = string.Empty;
		this.strInteractionClear = string.Empty;
		this.strCTClear = string.Empty;
	}

	private void Update()
	{
		this.Run();
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	public void FixForVentSensors()
	{
		List<CondOwner> list = new List<CondOwner>();
		this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.strPoint, false), DataHandler.GetCondTrigger(this.strCTClear), true, false, list);
		if (list.Count > 0)
		{
			Interaction interaction = DataHandler.GetInteraction(this.strInteractionClear, null, false);
			if (interaction != null && interaction.CTTestUs.Triggered(this.co, null, true))
			{
				this.co.QueueInteraction(this.co, interaction, false);
			}
		}
		else
		{
			Interaction interaction2 = DataHandler.GetInteraction(this.strInteractionAlarm, null, false);
			if (interaction2 != null && interaction2.CTTestUs.Triggered(this.co, null, true))
			{
				this.co.QueueInteraction(this.co, DataHandler.GetInteraction(this.strInteractionAlarm, null, false), false);
			}
			this.doNotExchange = true;
		}
	}

	public void Run()
	{
		if (this.co == null)
		{
			return;
		}
		if (this.co.HasCond(this.strSignalCond))
		{
			List<CondOwner> list = new List<CondOwner>();
			this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.strPoint, false), DataHandler.GetCondTrigger(this.strCTClear), true, false, list);
			if (list.Count > 0)
			{
				Interaction interaction = DataHandler.GetInteraction(this.strInteractionClear, null, false);
				if (interaction != null && interaction.CTTestUs.Triggered(this.co, null, true))
				{
					this.co.QueueInteraction(this.co, interaction, false);
				}
			}
			else
			{
				Interaction interaction2 = DataHandler.GetInteraction(this.strInteractionAlarm, null, false);
				if (interaction2 != null && interaction2.CTTestUs.Triggered(this.co, null, true))
				{
					this.co.QueueInteraction(this.co, DataHandler.GetInteraction(this.strInteractionAlarm, null, false), false);
					AlarmObjective objective = new AlarmObjective(AlarmType.low_pressure, this.co, DataHandler.GetString("OBJV_LOW_PRESSURE_TITLE", false), "TIsAlarmSafePressure", this.co.ship.strRegID, DataHandler.GetString("OBJV_LOW_PRESSURE_DESC", false));
					MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
				}
			}
			this.co.AddCondAmount(this.strSignalCond, -1.0, 0.0, 0f);
		}
	}

	public void SetData(Dictionary<string, string> gpm)
	{
		if (gpm == null)
		{
			return;
		}
		this.strPoint = gpm["strPoint"];
		this.strSignalCond = gpm["strSignalCond"];
		this.strInteractionAlarm = gpm["strInteractionAlarm"];
		this.strInteractionClear = gpm["strInteractionClear"];
		this.strCTClear = gpm["strCTClear"];
	}

	private CondOwner co;

	private string strPoint;

	private string strSignalCond;

	private string strInteractionAlarm;

	private string strInteractionClear;

	private string strCTClear;

	public bool doNotExchange;
}
