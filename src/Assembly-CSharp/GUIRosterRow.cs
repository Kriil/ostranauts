using System;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIRosterRow : MonoBehaviour
{
	private void Awake()
	{
		this.aHours = new GUIRosterHour[24];
		if (this.chkShore == null)
		{
			this.chkShore = base.transform.Find("pnlPerms/chkShore").GetComponent<Toggle>();
		}
		this.chkShore.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ShoreLeave();
		});
		if (this.chkAirlock == null)
		{
			this.chkAirlock = base.transform.Find("pnlPerms/chkAirlock").GetComponent<Toggle>();
		}
		this.chkAirlock.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.AirlockPermission();
		});
		if (this.chkRestore == null)
		{
			this.chkRestore = base.transform.Find("pnlPerms/chkRestore").GetComponent<Toggle>();
		}
		this.chkRestore.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.RestorePermission();
		});
		this.txtName = base.transform.Find("txtName").GetComponent<TMP_Text>();
		this.cgIdle = base.transform.Find("pnlIdle").GetComponent<CanvasGroup>();
		this.cgIdle.alpha = 0f;
		this.cgIdle.GetComponentInChildren<Image>().color = DataHandler.GetColor("NotifChartreuse");
	}

	public void SetOwner(string strName, JsonCompanyRules jRules)
	{
		this.jRules = jRules;
		this.chkShore.isOn = jRules.bShoreLeave;
		this.chkAirlock.isOn = jRules.bAirlockPermission;
		this.chkRestore.isOn = jRules.bRestorePermission;
		this.txtName.text = strName;
		this.coWorker = null;
		DataHandler.mapCOs.TryGetValue(this.txtName.text, out this.coWorker);
		Transform parent = base.transform.Find("pnlHours");
		GUIRosterHour original = Resources.Load<GUIRosterHour>("GUIShip/GUIRoster/GUIRosterHour");
		bool flag = this.aHours[0] == null;
		for (int i = 0; i < this.aHours.Length; i++)
		{
			if (flag)
			{
				this.aHours[i] = UnityEngine.Object.Instantiate<GUIRosterHour>(original, parent);
				this.aHours[i].txtLabel.text = i.ToString();
				this.aHours[i].onChange = new Action<int, int>(this.UpdateShift);
			}
			this.aHours[i].SetShift(jRules.aHours[i]);
			this.aHours[i].nHour = i;
		}
		Button button = Resources.Load<Button>("GUIShip/GUIRoster/GUIRosterUntime");
		button = UnityEngine.Object.Instantiate<Button>(button, parent);
		button.onClick.AddListener(delegate()
		{
			this.UntimeWarning();
		});
		TMP_Text component = button.transform.Find("Text").GetComponent<TMP_Text>();
		component.text = this.aHours.Length.ToString();
		if (CrewSim.objInstance.workManager.aNonIdleCrewIDs.Contains(this.coWorker))
		{
			this.cgIdle.alpha = 0f;
		}
		else
		{
			this.cgIdle.alpha = 1f;
		}
		this._initFinished = true;
	}

	private void UntimeWarning()
	{
		AudioManager.am.PlayAudioEmitter("ShipUIBtnRosterError", false, false);
		if (CrewSim.coPlayer == null)
		{
			return;
		}
		string @string = DataHandler.GetString("GUI_ROSTER_UNTIME_WARN", false);
		CrewSim.coPlayer.LogMessage(@string, "Bad", CrewSim.coPlayer.strName);
	}

	private void ShoreLeave()
	{
		if (this.jRules != null)
		{
			this.jRules.bShoreLeave = this.chkShore.isOn;
		}
	}

	private void AirlockPermission()
	{
		if (this.jRules != null)
		{
			this.jRules.bAirlockPermission = this.chkAirlock.isOn;
		}
		if (this._initFinished && CrewSim.coPlayer != null)
		{
			CrewSim.coPlayer.ZeroCondAmount("TutorialRosterShow");
			CrewSim.coPlayer.AddCondAmount("TutorialRosterComplete", 1.0, 0.0, 0f);
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			if (GUIRosterRow.Opened != null)
			{
				GUIRosterRow.Opened.Invoke();
			}
		}
	}

	private void RestorePermission()
	{
		if (this.jRules != null)
		{
			this.jRules.bRestorePermission = this.chkRestore.isOn;
		}
	}

	private void UpdateShift(int nHour, int nShift)
	{
		if (this.jRules != null)
		{
			this.jRules.aHours[nHour] = nShift;
			if (nHour == StarSystem.nUTCHour && this.coWorker != null)
			{
				this.coWorker.ShiftChange(this.coWorker.Company.GetShift(nHour, this.coWorker), false);
			}
		}
	}

	public GUIRosterHour[] aHours;

	public Toggle chkShore;

	public Toggle chkAirlock;

	public Toggle chkRestore;

	public TMP_Text txtName;

	public CanvasGroup cgIdle;

	public JsonCompanyRules jRules;

	private CondOwner coWorker;

	private bool _initFinished;

	public static UnityEvent Opened = new UnityEvent();
}
