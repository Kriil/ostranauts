using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GUIPDAGigRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void Awake()
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		this.onPointerClick.Invoke();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
	}

	public void OnPointerExit(PointerEventData eventData)
	{
	}

	public void SetJob(JsonJobSave jjs)
	{
		if (jjs == null)
		{
			return;
		}
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		string text4 = string.Empty;
		if (jjs.JobTemplate() != null)
		{
			text = jjs.JobTemplate().strName;
			Interaction interaction = DataHandler.GetInteraction(jjs.JobTemplate().strIASetupClient, null, false);
			if (interaction != null)
			{
				text = interaction.strTitle;
			}
			text3 = jjs.strThemID;
			if (jjs.strJobItems != null)
			{
				text3 = DataHandler.GetString("GUI_JOBSPDA_ROW_ITEMS", false);
				JsonJobItems jobItems = DataHandler.GetJobItems(jjs.strJobItems);
				if (jobItems != null)
				{
					text3 += jobItems.strFriendlyName;
				}
				if (jjs.strRegIDDropoff != null)
				{
					string str = jjs.strRegIDPickup;
					Ship shipByRegID = CrewSim.system.GetShipByRegID(jjs.strRegIDDropoff);
					if (shipByRegID != null)
					{
						str = shipByRegID.publicName;
						if (shipByRegID.LoadState >= Ship.Loaded.Edit)
						{
							CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsGigNexusKiosk");
							List<CondOwner> cos = shipByRegID.GetCOs(condTrigger, false, true, false);
							if (cos.Count > 0)
							{
								this.coFocus = cos[0];
							}
						}
					}
					text2 = DataHandler.GetString("GUI_JOBSPDA_ROW_LOCKER_DROPOFF", false) + str;
				}
				else
				{
					text3 = DataHandler.GetString("GUI_JOBSPDA_ROW_TARGET", false) + jjs.COThem().FriendlyName;
					if (jjs.COThem().ship.LoadState >= Ship.Loaded.Edit)
					{
						this.coFocus = jjs.COThem();
					}
				}
			}
			else
			{
				text3 = DataHandler.GetString("GUI_JOBSPDA_ROW_TARGET", false) + jjs.COThem().FriendlyName;
				text2 = DataHandler.GetString("GUI_JOBSPDA_ROW_LOCATION", false) + GUIJobs.GetShipName(jjs.COThem().ship, CrewSim.coPlayer.ship);
				if (jjs.COThem().ship.LoadState >= Ship.Loaded.Edit)
				{
					this.coFocus = jjs.COThem();
				}
			}
			double num = jjs.fEpochExpired - StarSystem.fEpoch;
			if (num > 0.0)
			{
				text4 = DataHandler.GetString("GUI_JOBSPDA_ROW_DURATION", false) + MathUtils.GetDurationFromS(num, 4);
			}
			else
			{
				text4 = DataHandler.GetString("GUI_JOBSPDA_ROW_DURATION", false) + DataHandler.GetString("GUI_JOBS_MAIN_DURATION_EXPIRED", false);
			}
		}
		base.transform.Find("txtName").GetComponent<TMP_Text>().text = text;
		base.transform.Find("txtBody").GetComponent<TMP_Text>().text = string.Concat(new string[]
		{
			text3,
			"\n",
			text2,
			"\n",
			text4
		});
	}

	public UnityEvent onPointerClick;

	public CondOwner coFocus;
}
