using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Jobs/gigs terminal UI. Likely shows open versus taken contracts, lets the
// player inspect details, and handles take/abandon/turn-in actions.
public class GUIJobs : GUIData
{
	// Unity setup: binds the list/sidebar widgets, wires button handlers, and
	// enables pause-on-open behavior for the contract board.
	protected override void Awake()
	{
		base.Awake();
		this.bPausesGame = true;
		this.btnA1 = base.transform.Find("btnA1").GetComponent<Button>();
		this.btnA2 = base.transform.Find("btnA2").GetComponent<Button>();
		this.btnA3 = base.transform.Find("btnA3").GetComponent<Button>();
		this.btnA4 = base.transform.Find("btnA4").GetComponent<Button>();
		this.btnAUp = base.transform.Find("btnAUp").GetComponent<Button>();
		this.btnADn = base.transform.Find("btnADn").GetComponent<Button>();
		this.btnB1 = base.transform.Find("btnB1").GetComponent<Button>();
		this.btnB2 = base.transform.Find("btnB2").GetComponent<Button>();
		this.btnB3 = base.transform.Find("btnB3").GetComponent<Button>();
		this.btnB4 = base.transform.Find("btnB4").GetComponent<Button>();
		this.btnBUp = base.transform.Find("btnBUp").GetComponent<Button>();
		this.btnBDn = base.transform.Find("btnBDn").GetComponent<Button>();
		this.txtA1 = base.transform.Find("pnlJobs/txt1").GetComponent<TMP_Text>();
		this.txtA2 = base.transform.Find("pnlJobs/txt2").GetComponent<TMP_Text>();
		this.txtA3 = base.transform.Find("pnlJobs/txt3").GetComponent<TMP_Text>();
		this.txtA4 = base.transform.Find("pnlJobs/txt4").GetComponent<TMP_Text>();
		this.txtB1 = base.transform.Find("pnlSidebar/txt1").GetComponent<TMP_Text>();
		this.txtB2 = base.transform.Find("pnlSidebar/txt2").GetComponent<TMP_Text>();
		this.txtB3 = base.transform.Find("pnlSidebar/txt3").GetComponent<TMP_Text>();
		this.txtB4 = base.transform.Find("pnlSidebar/txt4").GetComponent<TMP_Text>();
		this.cgA1txt = this.txtA1.GetComponent<CanvasGroup>();
		this.cgA2txt = this.txtA2.GetComponent<CanvasGroup>();
		this.cgA3txt = this.txtA3.GetComponent<CanvasGroup>();
		this.cgA4txt = this.txtA4.GetComponent<CanvasGroup>();
		this.cgB1txt = this.txtB1.GetComponent<CanvasGroup>();
		this.cgB2txt = this.txtB2.GetComponent<CanvasGroup>();
		this.cgB3txt = this.txtB3.GetComponent<CanvasGroup>();
		this.cgB4txt = this.txtB4.GetComponent<CanvasGroup>();
		this.txtValue = base.transform.Find("pnlSidebar/pnlMain/Viewport/pnlMainContent/txtValue").GetComponent<TMP_Text>();
		this.tfJobs = base.transform.Find("pnlJobs/pnlMain/Viewport/pnlMainContent");
		this.tfSidebar = base.transform.Find("pnlSidebar/pnlMain/Viewport/pnlMainContent");
		this.srJobs = base.transform.Find("pnlJobs/pnlMain").GetComponent<ScrollRect>();
		this.srSidebar = base.transform.Find("pnlSidebar/pnlMain").GetComponent<ScrollRect>();
		this.btnA1.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnA1);
		});
		this.btnA2.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnA2);
		});
		this.btnA3.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnA3);
		});
		this.btnA4.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnA4);
		});
		this.btnB1.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnB1);
		});
		this.btnB2.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnB2);
		});
		this.btnB3.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnB3);
		});
		this.btnB4.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnB4);
		});
		this.btnAUp.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnAUp);
		});
		this.btnADn.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnADn);
		});
		this.btnBUp.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnBUp);
		});
		this.btnBDn.onClick.AddListener(delegate()
		{
			this.BtnPress(this.btnBDn);
		});
		AudioManager.AddBtnAudio(this.btnA1.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnA2.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnA3.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnA4.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnB1.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnB2.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnB3.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnB4.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnAUp.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnADn.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnBUp.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		AudioManager.AddBtnAudio(this.btnBDn.gameObject, "ShipUIBtnGigIn", "ShipUIBtnGigOut");
		this.aRows = new List<GUIJobsRow>();
	}

	// Switches the left column between open-gig and taken-gig list modes.
	private void SetStateA(GUIJobs.GUIJobStateA nState)
	{
		CanvasManager.HideCanvasGroup(this.cgA1txt);
		CanvasManager.HideCanvasGroup(this.cgA2txt);
		CanvasManager.HideCanvasGroup(this.cgA3txt);
		CanvasManager.HideCanvasGroup(this.cgA4txt);
		CanvasManager.ShowCanvasGroup(this.cgA3txt);
		CanvasManager.ShowCanvasGroup(this.cgA4txt);
		this.txtA3.text = DataHandler.GetString("GUI_JOBS_BTN_REFRESH", false);
		this.txtA4.text = DataHandler.GetString("GUI_JOBS_BTN_RETURNTOP", false);
		this.nStateA = nState;
		if (nState != GUIJobs.GUIJobStateA.ListClosed)
		{
			if (nState == GUIJobs.GUIJobStateA.ListOpen)
			{
				this.RefreshJobs(false);
				CanvasManager.ShowCanvasGroup(this.cgA2txt);
				this.txtA2.text = DataHandler.GetString("GUI_JOBS_BTN_TAKEN_GIGS", false);
			}
		}
		else
		{
			this.RefreshJobs(true);
			CanvasManager.ShowCanvasGroup(this.cgA1txt);
			this.txtA1.text = DataHandler.GetString("GUI_JOBS_BTN_OPEN_GIGS", false);
		}
	}

	// Switches the right sidebar between empty/detail/result states for the
	// currently selected job row.
	private void SetStateB(GUIJobs.GUIJobStateB nState)
	{
		CanvasManager.HideCanvasGroup(this.cgB1txt);
		CanvasManager.HideCanvasGroup(this.cgB2txt);
		CanvasManager.HideCanvasGroup(this.cgB3txt);
		CanvasManager.HideCanvasGroup(this.cgB4txt);
		this.nStateB = nState;
		switch (nState)
		{
		case GUIJobs.GUIJobStateB.Empty:
			this.txtValue.text = DataHandler.GetString("GUI_JOBS_MAIN_DEFAULT", false);
			break;
		case GUIJobs.GUIJobStateB.Display:
			if (this.nStateA == GUIJobs.GUIJobStateA.ListOpen)
			{
				CanvasManager.ShowCanvasGroup(this.cgB3txt);
				this.txtB3.text = DataHandler.GetString("GUI_JOBS_BTN_TAKE_GIG", false);
			}
			else if (this.nStateA == GUIJobs.GUIJobStateA.ListClosed)
			{
				CanvasManager.ShowCanvasGroup(this.cgB2txt);
				this.txtB2.text = DataHandler.GetString("GUI_JOBS_BTN_ABANDON", false);
				if (this.jrSelected.Job.fEpochExpired > StarSystem.fEpoch)
				{
					CanvasManager.ShowCanvasGroup(this.cgB4txt);
					this.txtB4.text = DataHandler.GetString("GUI_JOBS_BTN_TURNIN", false);
				}
			}
			if (this.jrSelected.Job != null)
			{
				this.txtValue.text = GUIJobs.GetDisplayGigText(this.jrSelected.Job);
			}
			else
			{
				this.txtValue.text = GUIJobs.GetDisplayGigText(null);
			}
			base.StartCoroutine(CrewSim.objInstance.ScrollTop(this.srSidebar));
			break;
		case GUIJobs.GUIJobStateB.Abandoned:
			this.txtValue.text = DataHandler.GetString("GUI_JOBS_MAIN_ABANDONED", false);
			break;
		case GUIJobs.GUIJobStateB.Taken:
			this.txtValue.text = DataHandler.GetString("GUI_JOBS_MAIN_TAKEN", false);
			break;
		case GUIJobs.GUIJobStateB.TurnedIn:
			this.txtValue.text = DataHandler.GetString("GUI_JOBS_MAIN_TURNEDIN1", false) + DataHandler.GetString("GUI_JOBS_BONUS_" + this.nPayoutTier, false) + DataHandler.GetString("GUI_JOBS_MAIN_TURNEDIN2", false);
			if (this.nPayoutTier >= 8)
			{
				AudioManager.am.PlayAudioEmitter("ShipUIBtnPayout04", false, false);
			}
			else if (this.nPayoutTier >= 4)
			{
				AudioManager.am.PlayAudioEmitter("ShipUIBtnPayout03", false, false);
			}
			else if (this.nPayoutTier >= 2)
			{
				AudioManager.am.PlayAudioEmitter("ShipUIBtnPayout02", false, false);
			}
			else
			{
				AudioManager.am.PlayAudioEmitter("ShipUIBtnPayout01", false, false);
			}
			break;
		case GUIJobs.GUIJobStateB.ErrorIncomplete:
		{
			string text = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_INCOMPLETE1", false);
			text += this.jrSelected.Job.strFailReasons;
			text += DataHandler.GetString("GUI_JOBS_MAIN_ERROR_INCOMPLETE2", false);
			this.txtValue.text = text;
			break;
		}
		case GUIJobs.GUIJobStateB.ErrorCannotTake:
		{
			string text2 = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_CANNOTTAKE1", false);
			text2 += this.jrSelected.Job.strFailReasons;
			text2 += DataHandler.GetString("GUI_JOBS_MAIN_ERROR_CANNOTTAKE2", false);
			this.txtValue.text = text2;
			break;
		}
		}
	}

	private void BtnPress(Button btn)
	{
		if (btn == this.btnA1)
		{
			this.SetStateA(GUIJobs.GUIJobStateA.ListOpen);
		}
		if (btn == this.btnA2)
		{
			this.SetStateA(GUIJobs.GUIJobStateA.ListClosed);
		}
		if (btn == this.btnA3)
		{
			this.SetStateA(this.nStateA);
		}
		if (btn == this.btnA4)
		{
			if (this.aRows.Count > 0)
			{
				this.SelectRow(this.aRows[0]);
			}
			base.StartCoroutine(CrewSim.objInstance.ScrollTop(this.srJobs));
		}
		if (btn == this.btnB1)
		{
		}
		if (btn == this.btnB2 && this.jrSelected != null && this.cgB2txt.alpha > 0f)
		{
			GigManager.AbandonJob(this.jrSelected.Job, this.coUser);
			this.SetStateA(this.nStateA);
			this.SetStateB(GUIJobs.GUIJobStateB.Abandoned);
		}
		if (btn == this.btnB3 && this.jrSelected != null && this.cgB3txt.alpha > 0f)
		{
			if (GigManager.TakeJob(this.jrSelected.Job, this.coUser, this.COSelf))
			{
				this.SetStateA(this.nStateA);
				this.SetStateB(GUIJobs.GUIJobStateB.Taken);
			}
			else
			{
				this.SetStateB(GUIJobs.GUIJobStateB.ErrorCannotTake);
			}
		}
		if (btn == this.btnB4 && this.jrSelected != null && this.cgB4txt.alpha > 0f)
		{
			this.nPayoutTier = GigManager.GetTier(this.jrSelected.Job);
			if (GigManager.TurnInJob(this.jrSelected.Job, this.coUser, this.COSelf))
			{
				this.SetStateA(this.nStateA);
				this.SetStateB(GUIJobs.GUIJobStateB.TurnedIn);
			}
			else
			{
				this.SetStateA(this.nStateA);
				this.SetStateB(GUIJobs.GUIJobStateB.ErrorIncomplete);
			}
		}
		if (btn == this.btnAUp)
		{
			if (this.aRows.Count == 0)
			{
				return;
			}
			int num = 0;
			if (this.jrSelected != null)
			{
				num = this.aRows.IndexOf(this.jrSelected);
			}
			num--;
			if (num < 0)
			{
				num = this.aRows.Count - 1;
			}
			this.SelectRow(this.aRows[num]);
			this.SnapTo(this.aRows[num].GetComponent<RectTransform>());
		}
		if (btn == this.btnADn)
		{
			if (this.aRows.Count == 0)
			{
				return;
			}
			int num2 = 0;
			if (this.jrSelected != null)
			{
				num2 = this.aRows.IndexOf(this.jrSelected);
			}
			num2++;
			if (num2 > this.aRows.Count - 1)
			{
				num2 = 0;
			}
			this.SelectRow(this.aRows[num2]);
			this.SnapTo(this.aRows[num2].GetComponent<RectTransform>());
		}
		if (btn == this.btnBUp)
		{
			RectTransform component = this.srSidebar.GetComponent<RectTransform>();
			float y = component.rect.size.y;
			RectTransform component2 = this.txtValue.GetComponent<RectTransform>();
			float y2 = component2.rect.size.y;
			this.srSidebar.verticalNormalizedPosition += y / y2;
		}
		if (btn == this.btnBDn)
		{
			RectTransform component3 = this.srSidebar.GetComponent<RectTransform>();
			float y3 = component3.rect.size.y;
			RectTransform component4 = this.txtValue.GetComponent<RectTransform>();
			float y4 = component4.rect.size.y;
			this.srSidebar.verticalNormalizedPosition -= y3 / y4;
		}
	}

	public void SnapTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		RectTransform component = this.tfJobs.GetComponent<RectTransform>();
		Vector2 a = this.srJobs.transform.InverseTransformPoint(component.position);
		Vector2 b = this.srJobs.transform.InverseTransformPoint(target.position);
		b.y -= target.rect.y;
		Vector2 anchoredPosition = a - b;
		anchoredPosition.x = component.anchoredPosition.x;
		component.anchoredPosition = anchoredPosition;
		if (this.srJobs.verticalNormalizedPosition < 0f)
		{
			this.srJobs.verticalNormalizedPosition = 0f;
		}
		else if (this.srJobs.verticalNormalizedPosition > 1f)
		{
			this.srJobs.verticalNormalizedPosition = 1f;
		}
	}

	private void RefreshJobs(bool bTaken)
	{
		IEnumerator enumerator = this.tfJobs.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		this.aRows.Clear();
		if (GigManager.aJobs == null)
		{
			Debug.LogError("ERROR: GigManager aJobs is null.");
			return;
		}
		GigManager.GetJobs();
		GameObject original = Resources.Load("GUIShip/GUIJobs/prefabJobRow") as GameObject;
		bool flag = false;
		foreach (JsonJobSave jsonJobSave in GigManager.aJobs)
		{
			if (bTaken == jsonJobSave.bTaken)
			{
				if (GigManager.CheckLocal(jsonJobSave, this.coUser, double.PositiveInfinity))
				{
					Transform transform2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfJobs).transform;
					GUIJobsRow component = transform2.GetComponent<GUIJobsRow>();
					component.Job = jsonJobSave;
					component.Selected = false;
					component.AltRow = flag;
					flag = !flag;
					this.aRows.Add(component);
				}
			}
		}
		if (this.aRows.Count > 0)
		{
			this.SelectRow(this.aRows[0]);
		}
		else
		{
			this.SetStateB(GUIJobs.GUIJobStateB.Empty);
		}
	}

	private void SelectRow(GUIJobsRow jRow)
	{
		if (jRow == null)
		{
			return;
		}
		foreach (GUIJobsRow guijobsRow in this.aRows)
		{
			guijobsRow.Selected = (jRow == guijobsRow);
		}
		this.jrSelected = jRow;
		this.SetStateB(GUIJobs.GUIJobStateB.Display);
	}

	public static string GetDisplayGigText(JsonJobSave jjs)
	{
		if (jjs == null)
		{
			return DataHandler.GetString("GUI_JOBS_MAIN_DEFAULT", false);
		}
		GUIJobs.jobOutput.Length = 0;
		Interaction interactionSetupClient = jjs.GetInteractionSetupClient();
		GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_TITLE", false));
		if (interactionSetupClient != null)
		{
			GUIJobs.jobOutput.Append(interactionSetupClient.strTitle);
		}
		else
		{
			GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_TITLE_ERROR", false));
		}
		GUIJobs.jobOutput.AppendLine();
		if (jjs.bTaken)
		{
			double num = jjs.fEpochExpired - jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0;
			double num2 = jjs.fEpochExpired - StarSystem.fEpoch;
			if (num2 > 0.0)
			{
				GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_DURATION", false));
				GUIJobs.jobOutput.Append(MathUtils.GetDurationFromS(num2, 4));
				double dfAmount = num + jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 2.0 - StarSystem.fEpoch;
				GUIJobs.jobOutput.Append(" (" + MathUtils.GetDurationFromS(dfAmount, 4));
				dfAmount = num + jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 4.0 - StarSystem.fEpoch;
				GUIJobs.jobOutput.Append("|" + MathUtils.GetDurationFromS(dfAmount, 4));
				dfAmount = num + jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 8.0 - StarSystem.fEpoch;
				GUIJobs.jobOutput.Append("|" + MathUtils.GetDurationFromS(dfAmount, 4) + ")");
			}
			else
			{
				GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_DURATION_EXPIRED", false));
			}
		}
		else
		{
			GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_DURATION", false) + MathUtils.GetDurationFromS(jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0, 4));
			GUIJobs.jobOutput.Append(" (" + MathUtils.GetDurationFromS(jjs.JobTemplate().fDuration * jjs.fTimeMult / 2.0 * 3600.0, 4));
			GUIJobs.jobOutput.Append("|" + MathUtils.GetDurationFromS(jjs.JobTemplate().fDuration * jjs.fTimeMult / 4.0 * 3600.0, 4));
			GUIJobs.jobOutput.Append("|" + MathUtils.GetDurationFromS(jjs.JobTemplate().fDuration * jjs.fTimeMult / 8.0 * 3600.0, 4) + ")");
		}
		GUIJobs.jobOutput.AppendLine();
		GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_PAYOUT", false));
		GUIJobs.jobOutput.Append((jjs.fPayout * jjs.fPayoutMult + (double)((int)jjs.fItemValue)).ToString("n"));
		GUIJobs.jobOutput.Append(" (" + (jjs.fPayout * jjs.fPayoutMult * 2.0).ToString("n"));
		GUIJobs.jobOutput.Append("|" + (jjs.fPayout * jjs.fPayoutMult * 4.0).ToString("n"));
		GUIJobs.jobOutput.Append("|" + (jjs.fPayout * jjs.fPayoutMult * 8.0).ToString("n") + ")");
		GUIJobs.jobOutput.AppendLine();
		GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_COST", false));
		GUIJobs.jobOutput.Append((jjs.fCostContract + (double)((int)jjs.fItemValue)).ToString("n"));
		GUIJobs.jobOutput.AppendLine();
		GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_CLIENT", false));
		GUIJobs.jobOutput.Append(jjs.COClient().FriendlyName);
		if (jjs.strRegIDPickup != null)
		{
			GUIJobs.jobOutput.AppendLine();
			string value = jjs.strRegIDPickup;
			Ship shipByRegID = CrewSim.system.GetShipByRegID(jjs.strRegIDPickup);
			if (shipByRegID != null)
			{
				value = shipByRegID.publicName;
			}
			GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_PICKUP_REGID", false));
			GUIJobs.jobOutput.Append(value);
		}
		if (jjs.strRegIDDropoff != null)
		{
			GUIJobs.jobOutput.AppendLine();
			string value2 = jjs.strRegIDPickup;
			Ship shipByRegID2 = CrewSim.system.GetShipByRegID(jjs.strRegIDDropoff);
			if (shipByRegID2 != null)
			{
				value2 = shipByRegID2.publicName;
			}
			GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_DROPOFF_REGID", false));
			GUIJobs.jobOutput.Append(value2);
		}
		GUIJobs.jobOutput.AppendLine();
		GUIJobs.jobOutput.AppendLine();
		GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_GIG", false));
		if (interactionSetupClient != null)
		{
			GrammarUtils.PrepareGigFormat();
			GUIJobs.jobOutput.Append(GrammarUtils.GenerateDescription(interactionSetupClient));
			if (jjs.strTxt1 != null)
			{
				string @string = DataHandler.GetString(jjs.strTxt1, false);
				GUIJobs.jobOutput.Replace("[txt1]", @string);
			}
			if (jjs.strJobItems != null)
			{
				JsonJobItems jobItems = DataHandler.GetJobItems(jjs.strJobItems);
				GUIJobs.jobOutput.Replace("[itm]", jobItems.strFriendlyName);
			}
			Ship shipByRegID3 = CrewSim.system.GetShipByRegID(jjs.strRegIDPickup);
			if (shipByRegID3 != null)
			{
				GUIJobs.jobOutput.Replace("[regid_origin]", GUIJobs.GetShipName(shipByRegID3, CrewSim.coPlayer.ship));
			}
			shipByRegID3 = CrewSim.system.GetShipByRegID(jjs.strRegIDDropoff);
			if (shipByRegID3 != null)
			{
				GUIJobs.jobOutput.Replace("[regid_dest]", GUIJobs.GetShipName(shipByRegID3, CrewSim.coPlayer.ship));
			}
		}
		else
		{
			GUIJobs.jobOutput.Append(DataHandler.GetString("GUI_JOBS_MAIN_TITLE_ERROR", false));
		}
		string result = GUIJobs.jobOutput.ToString();
		GUIJobs.jobOutput.Length = 0;
		return result;
	}

	public static string GetShipName(Ship ship, Ship shipRangeTo = null)
	{
		if (ship == null)
		{
			return null;
		}
		string text = string.Empty;
		if (ship.objSS.bIsBO)
		{
			text = "<i>" + ship.publicName + "</i>";
		}
		else
		{
			text = DataHandler.GetString("GUI_JOBS_MAIN_SHIP_PREFIX", false) + "<i>" + ship.publicName + "</i>";
		}
		if (shipRangeTo != null)
		{
			text = text + " (" + MathUtils.GetDistUnits(shipRangeTo.objSS.GetRangeTo(ship.objSS)) + ")";
		}
		return text;
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.coUser = coSelf.GetInteractionCurrent().objThem;
		this.SetStateA(GUIJobs.GUIJobStateA.ListOpen);
		this.RefreshJobs(false);
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	private Button btnA1;

	private Button btnA2;

	private Button btnA3;

	private Button btnA4;

	private Button btnAUp;

	private Button btnADn;

	private Button btnBUp;

	private Button btnBDn;

	private Button btnB1;

	private Button btnB2;

	private Button btnB3;

	private Button btnB4;

	private TMP_Text txtA1;

	private TMP_Text txtA2;

	private TMP_Text txtA3;

	private TMP_Text txtA4;

	private TMP_Text txtB1;

	private TMP_Text txtB2;

	private TMP_Text txtB3;

	private TMP_Text txtB4;

	private TMP_Text txtValue;

	private CanvasGroup cgA1txt;

	private CanvasGroup cgA2txt;

	private CanvasGroup cgA3txt;

	private CanvasGroup cgA4txt;

	private CanvasGroup cgB1txt;

	private CanvasGroup cgB2txt;

	private CanvasGroup cgB3txt;

	private CanvasGroup cgB4txt;

	private Transform tfSidebar;

	private Transform tfJobs;

	private List<GUIJobsRow> aRows;

	private ScrollRect srJobs;

	private ScrollRect srSidebar;

	private CondOwner coUser;

	private GUIJobsRow jrSelected;

	private GUIJobs.GUIJobStateA nStateA;

	private GUIJobs.GUIJobStateB nStateB;

	private int nPayoutTier = 1;

	private static StringBuilder jobOutput = new StringBuilder(500);

	private enum GUIJobStateA
	{
		ListOpen,
		ListClosed
	}

	private enum GUIJobStateB
	{
		Empty,
		Display,
		Abandoned,
		Taken,
		TurnedIn,
		ErrorIncomplete,
		ErrorCannotTake
	}
}
