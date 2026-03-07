using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.Events;
using Ostranauts.Social.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main social encounter UI. Likely presents dialogue choices, reveal/status
// panels, encounter art, and objective text during social "combat" exchanges.
public class GUISocialCombat2 : MonoBehaviour
{
	// Ensures the singleton instance is initialized before other callers use it.
	private void Awake()
	{
		if (GUISocialCombat2.objInstance == null)
		{
			this.Init();
		}
	}

	// Resolves the panel widgets, button templates, and portrait/status displays
	// used by the social encounter screen.
	private void Init()
	{
		GUISocialCombat2.objInstance = this;
		this.tfActionList = base.transform.Find("pnlActions/scrollList/scrollMask/pnlContent");
		this.txtMessageLog = base.transform.Find("pnlTextLog/Viewport/txt").GetComponent<TMP_Text>();
		this.txtPreview = base.transform.Find("pnlPreview/txt").GetComponent<TMP_Text>();
		this.txtEncTitle = base.transform.Find("pnlEncounter/txtTitle").GetComponent<TMP_Text>();
		this.txtEncDesc = base.transform.Find("pnlEncounter/pnlTxtScroll/Viewport/txt").GetComponent<TMP_Text>();
		this.txtObjective = base.transform.Find("pnlObjective/txt").GetComponent<TMP_Text>();
		this.txtObjectiveTitle = base.transform.Find("pnlObjective/pnlTitle/txtTitle").GetComponent<TMP_Text>();
		this.srLog = base.transform.Find("pnlTextLog").GetComponent<ScrollRect>();
		this.bmpEnc = base.transform.Find("pnlEncounter/bmp").GetComponent<RawImage>();
		this.toggleGroup = base.transform.Find("pnlActions/pnlToggleGroup").GetComponent<ToggleGroup>();
		this.gcbTemplate = (Resources.Load("btnSocialChoice") as GameObject).GetComponent<GUIContextButton>();
		this.gcbTemplateA = (Resources.Load("btnSocialChoiceA") as GameObject).GetComponent<GUIContextButton>();
		this.gcbTemplateB = (Resources.Load("btnSocialChoiceB") as GameObject).GetComponent<GUIContextButton>();
		this.cgEnc = base.transform.Find("pnlEncounter").GetComponent<CanvasGroup>();
		this.cgObjective = base.transform.Find("pnlObjective").GetComponent<CanvasGroup>();
		GUISocialCombat2.gssUs = base.transform.Find("pnlPortraitUs").GetComponent<GUISocialStatus>();
		GUISocialCombat2.gssThem = base.transform.Find("pnlPortraitThem").GetComponent<GUISocialStatus>();
		Button component = base.transform.Find("pnlConfirm/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.ConfirmAction();
		});
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnPDAClick03", "ShipUIBtnPDAClick04");
		GUISocialCombat2.aActions = new List<GUIContextButton>();
		Button component2 = base.transform.Find("pnlExit/btn").GetComponent<Button>();
		component2.onClick.AddListener(delegate()
		{
			this.ForceExit();
		});
		AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnPDAClick03", "ShipUIBtnPDAClick04");
		int integer = this.cgObjective.GetComponent<Animator>().GetInteger("AnimState");
		if (integer != 0)
		{
			Debug.Log("AnimState: " + this.cgObjective.GetComponent<Animator>().GetInteger("AnimState"));
			this.cgObjective.GetComponent<Animator>().SetInteger("AnimState", 0);
			Debug.Log("AnimState: " + this.cgObjective.GetComponent<Animator>().GetInteger("AnimState"));
		}
	}

	// Loads the loot-driven condition filters that decide which Conditions are
	// shown as social stats, traits, skills, or generic reveals in this UI.
	private static void LoadFilters()
	{
		GUISocialCombat2.aStatusFilter = GUISocialCombat2.CreateFilter(DataHandler.GetLoot("CONDSocialGUIFilterDCs").GetLootNames(null, false, null));
		GUISocialCombat2.aStatusFilterDCsMTT = GUISocialCombat2.CreateFilter(DataHandler.GetLoot("CONDSocialGUIFilterDCsMTT").GetLootNames(null, false, null));
		GUISocialCombat2.aStatusFilterSocial = GUISocialCombat2.CreateFilter(DataHandler.GetLoot("CONDSocialGUIFilterDCStatBars").GetLootNames(null, false, null));
		GUISocialCombat2.aTraitsFilter = GUISocialCombat2.CreateFilter(DataHandler.GetLoot("CONDSocialGUIFilter").GetLootNames(null, false, null));
		GUISocialCombat2.aSkillsFilter = GUISocialCombat2.CreateFilter(DataHandler.GetLoot("CONDSocialGUIFilterSkills").GetLootNames(null, false, null));
	}

	// Converts one loot list of condition ids into a quick lookup filter.
	private static HashSet<string> CreateFilter(List<string> lootNames)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (lootNames != null)
		{
			foreach (string item in lootNames)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	// Helper used by social reveal code to decide whether a condition should be
	// surfaced in the encounter UI.
	public static bool CountsAsSocialReveal(string strCondName, bool bSocial = true, bool bStatus = true, bool bTraits = true, bool bSkills = true)
	{
		if (GUISocialCombat2.aStatusFilter == null)
		{
			GUISocialCombat2.LoadFilters();
		}
		return (bStatus && GUISocialCombat2.aStatusFilter.Contains(strCondName)) || (bSocial && GUISocialCombat2.aStatusFilterSocial.Contains(strCondName)) || (bTraits && GUISocialCombat2.aTraitsFilter.Contains(strCondName)) || (bSkills && GUISocialCombat2.aSkillsFilter.Contains(strCondName));
	}

	// Buckets a revealed condition name into the display category lists used by
	// the Mind/Mood/Trait-style summary panels.
	public static void MMTCategory(string strCondName, List<string> aGeneral, List<string> aSocial, List<string> aTraits, List<string> aSkills, List<string> aDiscard, string strRename = null)
	{
		if (GUISocialCombat2.aStatusFilter == null)
		{
			GUISocialCombat2.LoadFilters();
		}
		if (string.IsNullOrEmpty(strRename))
		{
			strRename = strCondName;
		}
		if (aSkills != null && GUISocialCombat2.aSkillsFilter.Contains(strCondName))
		{
			aSkills.Add(strRename);
		}
		else if (aTraits != null && GUISocialCombat2.aTraitsFilter.Contains(strCondName))
		{
			aTraits.Add(strRename);
		}
		else if (aSocial != null && GUISocialCombat2.aStatusFilterDCsMTT.Contains(strCondName))
		{
			aSocial.Add(strRename);
		}
		else if (aGeneral != null && GUISocialCombat2.aStatusFilter.Contains(strCondName))
		{
			aGeneral.Add(strRename);
		}
		else if (aDiscard != null)
		{
			aDiscard.Add(strRename);
		}
	}

	// Populates the encounter with the acting crew, target, and available social
	// Interactions, then optionally delays the sim pause for UI presentation.
	public void SetData(CondOwner coUs, CondOwner coThem, bool bPause, List<Interaction> aList = null)
	{
		if (GUISocialCombat2.objInstance == null)
		{
			this.Init();
		}
		if (coUs == null)
		{
			return;
		}
		this.bIgnoreInvoke = false;
		if (bPause)
		{
			base.Invoke("Pause", Convert.ToSingle(GUISocialCombat2.fPauseDelay));
		}
		GUISocialCombat2.fPauseDelay = GUISocialCombat2.fPauseDelayDefault;
		GUISocialCombat2.coUs = coUs;
		GUISocialCombat2.coThem = coThem;
		this.UpdateCO(coUs);
		this.ClearActions();
		GUISocialCombat2.aActions.Clear();
		this.sbDebug = new StringBuilder();
		this.sbDebug.Append(coUs.strName);
		this.sbDebug.Append(" choices");
		this.sbDebug.AppendLine(":");
		if (GUISocialCombat2.strSubUI == "Landscape01")
		{
			Interaction interactionCurrent = coUs.GetInteractionCurrent();
			if (interactionCurrent != null)
			{
				CanvasManager.ShowCanvasGroup(this.cgEnc);
				foreach (string text in interactionCurrent.aInverse)
				{
					string[] array = text.Split(new char[]
					{
						','
					});
					Interaction interaction = DataHandler.GetInteraction(array[0], null, false);
					if (interaction != null)
					{
						if (interaction.strThemType == "Self")
						{
							this.AddAction(interaction, coUs, coUs);
						}
						else
						{
							interactionCurrent.AssignReplyRoles(interaction, array, false);
							this.AddAction(interaction, interaction.objUs, interaction.objThem);
						}
					}
				}
				this.txtEncTitle.text = interactionCurrent.strTitle;
				this.txtEncDesc.text = interactionCurrent.strDesc;
				this.bmpEnc.texture = DataHandler.LoadPNG(interactionCurrent.strImage + ".png", false, false);
			}
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgEnc);
			this.UpdateCO(coThem);
			if (aList != null && aList[0].strName != "SOCBlank")
			{
				foreach (Interaction ia in aList)
				{
					this.AddAction(ia);
				}
			}
			else
			{
				foreach (JsonJobSave jsonJobSave in GigManager.aJobs)
				{
					if (jsonJobSave != null && jsonJobSave.bTaken)
					{
						Interaction interactionDo = jsonJobSave.GetInteractionDo(coUs, coThem);
						if (interactionDo != null)
						{
							this.AddAction(interactionDo);
						}
					}
				}
				foreach (string strName in coUs.aInteractions)
				{
					Interaction interaction2 = DataHandler.GetInteraction(strName, null, false);
					this.AddAction(interaction2, coUs, coThem);
				}
			}
		}
		this.bPadBeforeEnd = true;
		if (GUISocialCombat2.aActions.Count == 1)
		{
			GUISocialCombat2.aActions[0].GetComponent<Toggle>().isOn = true;
		}
		else if (GUISocialCombat2.aActions.Count == 0)
		{
			this.EndSocialCombat();
		}
		else
		{
			CrewSim.objInstance.CamCenter(coUs);
			this.ThrobOn(coUs);
		}
		Debug.Log(this.sbDebug.ToString());
		base.StartCoroutine(CrewSim.objInstance.ScrollTop(base.transform.Find("pnlActions/scrollList").GetComponent<ScrollRect>()));
	}

	private void Pause()
	{
		if (this.bIgnoreInvoke)
		{
			return;
		}
		CrewSim.Paused = true;
	}

	public void ClearActions()
	{
		this.txtPreview.text = string.Empty;
		GUISocialCombat2.aActions.Clear();
		IEnumerator enumerator = this.tfActionList.GetEnumerator();
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
	}

	public void UpdateCO(CondOwner co)
	{
		if (GUISocialCombat2.objInstance == null)
		{
			return;
		}
		if (GUISocialCombat2.coUs == null)
		{
			return;
		}
		if (co == null)
		{
			return;
		}
		if (co == GUISocialCombat2.coUs)
		{
			GUISocialCombat2.gssUs.SetData(GUISocialCombat2.coUs, GUISocialCombat2.coThem, true, null);
			StringBuilder stringBuilder = new StringBuilder();
			int count = GUISocialCombat2.coUs.aMessages.Count;
			for (int i = 0; i < count; i++)
			{
				stringBuilder.Append("<color=#");
				stringBuilder.Append(ColorUtility.ToHtmlStringRGB(DataHandler.GetColor(GUISocialCombat2.coUs.aMessages[i].strColor)));
				stringBuilder.Append(">");
				bool flag = GUISocialCombat2.coUs.aMessages[i].strOwner != GUISocialCombat2.coUs.strName;
				if (flag)
				{
					stringBuilder.Append("<align=\"right\"><alpha=#80>");
				}
				stringBuilder.Append(GUISocialCombat2.coUs.aMessages[i].strMessage);
				if (flag)
				{
					stringBuilder.Append("<alpha=#FF></align>");
				}
				stringBuilder.Append("</color>");
				stringBuilder.AppendLine();
			}
			this.txtMessageLog.text = stringBuilder.ToString();
		}
		else if (co == GUISocialCombat2.coThem)
		{
			GUISocialCombat2.gssThem.SetData(GUISocialCombat2.coThem, GUISocialCombat2.coUs, false, null);
		}
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srLog));
	}

	public static string GetStakesInfo(CondOwner coUs, CondOwner coThem)
	{
		if (coUs == null || coThem == null || coUs.socUs == null)
		{
			return null;
		}
		Relationship relationship = coUs.socUs.GetRelationship(coThem.strName);
		if (relationship == null || string.IsNullOrEmpty(relationship.strContext))
		{
			return null;
		}
		SocialStakes stakeObject = GUISocialCombat2.GetStakeObject(relationship.strContext);
		if (stakeObject != null)
		{
			stakeObject.UpdateUs(relationship.pspec.GetCO());
			return stakeObject.MTTInfo;
		}
		return null;
	}

	public static void UpdateContext(Interaction ia)
	{
		if (ia.objUs == null || ia.objThem == null)
		{
			return;
		}
		Relationship relationship = ia.objUs.socUs.GetRelationship(ia.objThem.strName);
		if (relationship == null || string.IsNullOrEmpty(relationship.strContext))
		{
			return;
		}
		SocialStakes stakeObject = GUISocialCombat2.GetStakeObject(relationship.strContext);
		if (stakeObject != null)
		{
			stakeObject.UpdateUs(ia);
			stakeObject.UpdateThem(ia);
		}
		GUISocialCombat2.OnContextUpdated.Invoke(ia, stakeObject);
	}

	private static SocialStakes GetStakeObject(string strContext)
	{
		switch (strContext)
		{
		case "ACTHireCrewUs":
		case "ACTHireCrewThem":
			return new Hire();
		case "ACTQuitNegotiationUs":
		case "ACTQuitNegotiationThem":
			return new Quit();
		case "ACTPoliceShakedownUs":
		case "ACTPoliceShakedownThem":
			return new Bribe();
		case "Default":
			return null;
		}
		JsonContext context = DataHandler.GetContext(strContext);
		if (context != null)
		{
			return new SocialStakes(context.strName);
		}
		return null;
	}

	private void AddAction(Interaction ia, CondOwner coUs, CondOwner coThem)
	{
		if (ia != null)
		{
			ia.bVerboseTrigger = true;
			if (ia.Triggered(coUs, coThem, true, false, false, true, null))
			{
				ia.objUs = coUs;
				ia.objThem = coThem;
				GUIContextButton gcb = null;
				if (ia.bGamit)
				{
					gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplateB, this.tfActionList);
				}
				else if (ia.strLootCTsRemoveUs != null)
				{
					gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplateA, this.tfActionList);
				}
				else
				{
					gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplate, this.tfActionList);
				}
				string text = ia.strTitle;
				string reqUsedSuffix = ia.CTTestUs.GetReqUsedSuffix(coUs);
				if (reqUsedSuffix.Length > 0)
				{
					text = text + " " + reqUsedSuffix;
				}
				gcb.GetComponentInChildren<TMP_Text>().text = text;
				Toggle component = gcb.GetComponent<Toggle>();
				component.group = this.toggleGroup;
				component.onValueChanged.AddListener(delegate(bool A_1)
				{
					this.ChooseAction(gcb);
				});
				AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
				gcb.ia = ia;
				GUISocialCombat2.aActions.Add(gcb);
				this.sbDebug.Append("PASSED: ");
				this.sbDebug.Append(ia.strTitle + "(" + ia.strName + ")");
				this.sbDebug.AppendLine(": Passed.");
			}
			else
			{
				this.sbDebug.Append("FAILED: ");
				this.sbDebug.Append(ia.strTitle + "(" + ia.strName + ")");
				this.sbDebug.AppendLine(": Failed. " + ia.FailReasons(true, true, true));
			}
		}
	}

	private void AddAction(Interaction ia)
	{
		if (ia != null)
		{
			if (ia.objThem != GUISocialCombat2.coThem)
			{
			}
			GUIContextButton gcb = null;
			if (ia.bGamit)
			{
				gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplateB, this.tfActionList);
			}
			else if (ia.strLootCTsRemoveUs != null)
			{
				gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplateA, this.tfActionList);
			}
			else
			{
				gcb = UnityEngine.Object.Instantiate<GUIContextButton>(this.gcbTemplate, this.tfActionList);
			}
			string text = ia.strTitle;
			string reqUsedSuffix = ia.CTTestUs.GetReqUsedSuffix(GUISocialCombat2.coUs);
			if (reqUsedSuffix.Length > 0)
			{
				text = text + " " + reqUsedSuffix;
			}
			gcb.GetComponentInChildren<TMP_Text>().text = text;
			gcb.GetComponent<Toggle>().group = this.toggleGroup;
			gcb.GetComponent<Toggle>().onValueChanged.AddListener(delegate(bool A_1)
			{
				this.ChooseAction(gcb);
			});
			gcb.ia = ia;
			GUISocialCombat2.aActions.Add(gcb);
		}
	}

	public static string GetSocialPreview(Interaction ia)
	{
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		GUISocialCombat2.objInstance.GetPreviewScores(ia.LootCTsUs, dictionary);
		GUISocialCombat2.objInstance.GetPreviewScores(ia.LootCondsUs, dictionary);
		string text = GUISocialCombat2.objInstance.PreviewShort(dictionary, false);
		if (!string.IsNullOrEmpty(text))
		{
			text = "Us: " + text;
		}
		dictionary = new Dictionary<string, double>();
		GUISocialCombat2.objInstance.GetPreviewScores(ia.LootCTsThem, dictionary);
		GUISocialCombat2.objInstance.GetPreviewScores(ia.LootCondsThem, dictionary);
		string text2 = GUISocialCombat2.objInstance.PreviewShort(dictionary, true);
		if (!string.IsNullOrEmpty(text2))
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += "\n";
			}
			text = text + "Them: " + text2;
		}
		if ((ia.CTTestUs != null && (double)ia.CTTestUs.fChance < 1.0) || (ia.CTTestThem != null && (double)ia.CTTestThem.fChance < 1.0))
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += "\n";
			}
			text += "Not always available. ";
		}
		if ((ia.aInverse == null || ia.aInverse.Length == 0) && !ia.bCloser)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += "\n";
			}
			text += "Keeps control. ";
		}
		return text;
	}

	private void ChooseAction(GUIContextButton gcb)
	{
		if (gcb != null && gcb.GetComponent<Toggle>().isOn)
		{
			this.txtPreview.text = gcb.ia.strTitle + "\n";
			Dictionary<string, double> dictionary = new Dictionary<string, double>();
			this.GetPreviewScores(gcb.ia.LootCTsUs, dictionary);
			this.GetPreviewScores(gcb.ia.LootCondsUs, dictionary);
			string text = this.PreviewShort(dictionary, false);
			if (text.Length > 0)
			{
				TMP_Text tmp_Text = this.txtPreview;
				tmp_Text.text = tmp_Text.text + "Us: " + text + "\n";
			}
			dictionary = new Dictionary<string, double>();
			this.GetPreviewScores(gcb.ia.LootCTsThem, dictionary);
			this.GetPreviewScores(gcb.ia.LootCondsThem, dictionary);
			text = this.PreviewShort(dictionary, true);
			if (text.Length > 0)
			{
				TMP_Text tmp_Text2 = this.txtPreview;
				tmp_Text2.text = tmp_Text2.text + "<alpha=#80>Them: " + text + "<alpha=#FF>\n";
			}
			if (gcb.ia.aLootItemRemoveContract != null && gcb.ia.aLootItemRemoveContract.Count > 0)
			{
				TMP_Text tmp_Text3 = this.txtPreview;
				tmp_Text3.text = tmp_Text3.text + "Consumes one " + gcb.ia.aLootItemRemoveContract[0].FriendlyName + ". ";
			}
			if (gcb.ia.aLootItemUseContract != null && gcb.ia.aLootItemUseContract.Count > 0)
			{
				TMP_Text tmp_Text4 = this.txtPreview;
				tmp_Text4.text = tmp_Text4.text + "Uses one " + gcb.ia.aLootItemUseContract[0].FriendlyName + ". ";
			}
			string reqUsedSuffix = gcb.ia.CTTestUs.GetReqUsedSuffix(GUISocialCombat2.coUs);
			if (reqUsedSuffix != string.Empty)
			{
				TMP_Text tmp_Text5 = this.txtPreview;
				tmp_Text5.text = tmp_Text5.text + "Requires " + reqUsedSuffix + ". ";
			}
			if ((double)gcb.ia.CTTestUs.fChance < 1.0 || (double)gcb.ia.CTTestThem.fChance < 1.0)
			{
				TMP_Text tmp_Text6 = this.txtPreview;
				tmp_Text6.text += "Not always available. ";
			}
			if (gcb.ia.aInverse.Length == 0 && !gcb.ia.bCloser)
			{
				TMP_Text tmp_Text7 = this.txtPreview;
				tmp_Text7.text += "Keeps control. ";
			}
			if (!string.IsNullOrEmpty(gcb.ia.strLedgerDef))
			{
				JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(gcb.ia.strLedgerDef);
				if (ledgerDef != null)
				{
					int num = this.txtPreview.text.Count((char x) => x == '\n');
					for (int i = 4; i >= num; i--)
					{
						TMP_Text tmp_Text8 = this.txtPreview;
						tmp_Text8.text += "\n";
					}
					string str = "<align=flush>" + ((ledgerDef.fAmount <= 0f) ? "color=green>" : "<color=red> -");
					TMP_Text tmp_Text9 = this.txtPreview;
					tmp_Text9.text = tmp_Text9.text + "\nFunds:$" + CrewSim.coPlayer.GetCondAmount("StatUSD").ToString("N0");
					TMP_Text tmp_Text10 = this.txtPreview;
					tmp_Text10.text = tmp_Text10.text + str + Mathf.Abs(ledgerDef.fAmount).ToString("N0") + "</color></align>";
				}
			}
			GUISocialCombat2.gssThem.SetData(GUISocialCombat2.coThem, GUISocialCombat2.coUs, false, gcb.ia);
		}
	}

	private string PreviewShort(Dictionary<string, double> dictScores, bool bThem)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		string text4 = string.Empty;
		string text5 = string.Empty;
		string text6 = GUISocialCombat2.strUpArrow;
		string text7 = GUISocialCombat2.strDownArrow;
		if (bThem)
		{
			text6 = GUISocialCombat2.strUpArrowDim;
			text7 = GUISocialCombat2.strDownArrowDim;
		}
		foreach (KeyValuePair<string, double> keyValuePair in dictScores)
		{
			Condition cond = DataHandler.GetCond(keyValuePair.Key);
			if (cond != null)
			{
				if (!bThem || cond.nDisplayOther != 0)
				{
					if (bThem || cond.nDisplaySelf != 0)
					{
						if (keyValuePair.Value > 30.0)
						{
							if (text2.Length > 0)
							{
								text2 += ", ";
							}
							string text8 = text2;
							text2 = string.Concat(new string[]
							{
								text8,
								cond.strNameFriendly,
								text6,
								text6,
								text6
							});
						}
						else if (keyValuePair.Value > 11.0)
						{
							if (text2.Length > 0)
							{
								text2 += ", ";
							}
							text2 = text2 + cond.strNameFriendly + text6 + text6;
						}
						else if (keyValuePair.Value > 5.0)
						{
							if (text3.Length > 0)
							{
								text3 += ", ";
							}
							text3 = text3 + cond.strNameFriendly + text6;
						}
						else if (keyValuePair.Value < 0.0)
						{
							if (keyValuePair.Value < -30.0)
							{
								if (text4.Length > 0)
								{
									text4 += ", ";
								}
								string text8 = text4;
								text4 = string.Concat(new string[]
								{
									text8,
									cond.strNameFriendly,
									text7,
									text7,
									text7
								});
							}
							else if (keyValuePair.Value < -11.0)
							{
								if (text4.Length > 0)
								{
									text4 += ", ";
								}
								text4 = text4 + cond.strNameFriendly + text7 + text7;
							}
							else if (keyValuePair.Value < -5.0)
							{
								if (text5.Length > 0)
								{
									text5 += ", ";
								}
								text5 = text5 + cond.strNameFriendly + text7;
							}
						}
					}
				}
			}
		}
		if (text2.Length > 0)
		{
			text += text2;
		}
		if (text3.Length > 0)
		{
			if (text.Length > 0)
			{
				text += ", ";
			}
			text += text3;
		}
		if (text4.Length > 0)
		{
			if (text.Length > 0)
			{
				text += ", ";
			}
			text += text4;
		}
		if (text5.Length > 0)
		{
			if (text.Length > 0)
			{
				text += ", ";
			}
			text += text5;
		}
		return text;
	}

	private void GetPreviewScores(Loot loot, Dictionary<string, double> dict)
	{
		if (loot != null)
		{
			if (loot.strType == "trigger")
			{
				foreach (CondTrigger condTrigger in loot.GetCTLoot(null, null))
				{
					string strCondName = condTrigger.strCondName;
					double num = (double)condTrigger.fCount;
					if (Array.IndexOf<string>(GUISocialCombat2.aPreviewStats, condTrigger.strCondName) >= 0 && condTrigger.fCount != 0f)
					{
						if (dict.ContainsKey(condTrigger.strCondName))
						{
							string strCondName2;
							dict[strCondName2 = condTrigger.strCondName] = dict[strCondName2] + (double)(-(double)condTrigger.fCount);
						}
						else
						{
							dict[condTrigger.strCondName] = (double)(-(double)condTrigger.fCount);
						}
					}
				}
			}
			if (loot.strType == "condition")
			{
				foreach (KeyValuePair<string, double> keyValuePair in loot.GetCondLoot(1f, null, null))
				{
					string key = keyValuePair.Key;
					double value = keyValuePair.Value;
					if (Array.IndexOf<string>(GUISocialCombat2.aPreviewStats, key) >= 0 && value != 0.0)
					{
						if (dict.ContainsKey(key))
						{
							string key2;
							dict[key2 = key] = dict[key2] + -value;
						}
						else
						{
							dict[key] = -value;
						}
					}
				}
			}
		}
	}

	private void ConfirmAction()
	{
		if (this.toggleGroup.AnyTogglesOn())
		{
			foreach (Toggle toggle in this.toggleGroup.ActiveToggles())
			{
				Interaction ia = toggle.GetComponent<GUIContextButton>().ia;
				ia.bManual = true;
				if (ia.objUs != null)
				{
					ia.objUs.AIIssueOrder(ia.objThem, ia, string.IsNullOrEmpty(GUISocialCombat2.strSubUI), null, 0f, 0f);
					this.ClearActions();
					CrewSim.Paused = false;
					this.bIgnoreInvoke = true;
					if (ia.bCloser)
					{
						this.EndSocialCombat();
					}
					break;
				}
			}
		}
	}

	private void ForceExit()
	{
		this.ClearActions();
		Interaction interaction = DataHandler.GetInteraction("SocialCombatExit", null, false);
		if (interaction != null && GUISocialCombat2.coUs != null && GUISocialCombat2.coThem != null)
		{
			interaction.objUs = GUISocialCombat2.coUs;
			interaction.objThem = GUISocialCombat2.coThem;
			interaction.ApplyChain(null);
		}
		else
		{
			if (GUISocialCombat2.coUs != null && GUISocialCombat2.coUs.socUs != null && GUISocialCombat2.coThem != null)
			{
				Relationship relationship = GUISocialCombat2.coUs.socUs.GetRelationship(GUISocialCombat2.coThem.strName);
				if (relationship != null)
				{
					relationship.strContext = "Default";
				}
				GUISocialCombat2.coUs.ZeroCondAmount("InSocialCombat");
			}
			if (GUISocialCombat2.coThem != null && GUISocialCombat2.coThem.socUs != null && GUISocialCombat2.coUs != null)
			{
				Relationship relationship2 = GUISocialCombat2.coThem.socUs.GetRelationship(GUISocialCombat2.coUs.strName);
				if (relationship2 != null)
				{
					relationship2.strContext = "Default";
				}
				GUISocialCombat2.coThem.ZeroCondAmount("InSocialCombat");
			}
		}
		this.EndSocialCombat();
		CrewSim.Paused = false;
	}

	public static void ResetSocialCombat(CondOwner co)
	{
		if (GUISocialCombat2.objInstance == null)
		{
			return;
		}
		if (co == null)
		{
			return;
		}
		if (GUISocialCombat2.aActions.Count > 0)
		{
			return;
		}
		int num = 0;
		if (GUISocialCombat2.coUs != null && co == GUISocialCombat2.coUs)
		{
			num++;
		}
		if (GUISocialCombat2.coThem != null && co == GUISocialCombat2.coThem)
		{
			num++;
		}
		if (num == 0)
		{
			return;
		}
		if (GUISocialCombat2.coUs == GUISocialCombat2.coThem && GUISocialCombat2.strSubUI == null)
		{
			GUISocialCombat2.objInstance.EndSocialCombat();
		}
		GUISocialCombat2.objInstance.SetData(GUISocialCombat2.coUs, GUISocialCombat2.coThem, true, null);
	}

	public void EndSocialCombat()
	{
		if (GUISocialCombat2.coUs != null)
		{
			GUISocialCombat2.coUs.LogMessage(DataHandler.GetString("GUI_SOCIAL_END_CONVERSATION", false), "Neutral", "Game");
		}
		GUISocialCombat2.coUs = null;
		GUISocialCombat2.coThem = null;
		GUISocialCombat2.strSubUI = null;
		CanvasManager.instance.CrewSimNormal();
		CrewSim.objInstance.CamCenter(CrewSim.GetSelectedCrew());
	}

	public void ThrobOn(CondOwner condOwner)
	{
		if (condOwner == GUISocialCombat2.coUs)
		{
			GUISocialCombat2.gssUs.GetComponentInChildren<GUIThrobber>().StartTurn();
			GUISocialCombat2.gssThem.GetComponentInChildren<GUIThrobber>().EndTurn();
		}
		else if (condOwner == GUISocialCombat2.coThem)
		{
			GUISocialCombat2.gssThem.GetComponentInChildren<GUIThrobber>().StartTurn();
			GUISocialCombat2.gssUs.GetComponentInChildren<GUIThrobber>().EndTurn();
		}
	}

	public static bool IsInSocialCombat(CondOwner co)
	{
		return !(co == null) && (GUISocialCombat2.coUs == co || GUISocialCombat2.coThem == co);
	}

	public static ContextUpdatedEvent OnContextUpdated = new ContextUpdatedEvent();

	public static GUISocialCombat2 objInstance;

	public static CondOwner coUs;

	public static CondOwner coThem;

	private static GUISocialStatus gssUs;

	private static GUISocialStatus gssThem;

	public static double fPauseDelay = 1.0;

	private static double fPauseDelayDefault = GUISocialCombat2.fPauseDelay;

	private static HashSet<string> aStatusFilter;

	private static HashSet<string> aStatusFilterDCsMTT;

	private static HashSet<string> aStatusFilterSocial;

	private static HashSet<string> aTraitsFilter;

	private static HashSet<string> aSkillsFilter;

	public static string strSubUI;

	private static List<GUIContextButton> aActions;

	public static string strUpArrow = "<sprite=\"FontSprites\" index=2 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(1f, 1f, 1f, 1f)) + "\">";

	public static string strDownArrow = "<sprite=\"FontSprites\" index=3 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(1f, 1f, 1f, 1f)) + "\">";

	public static string strUpArrowDim = "<sprite=\"FontSprites\" index=2 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(1f, 1f, 1f, 0.5f)) + "\">";

	public static string strDownArrowDim = "<sprite=\"FontSprites\" index=3 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(1f, 1f, 1f, 0.5f)) + "\">";

	public static string strUpArrowPre = "<sprite=\"FontSprites\" index=2 color=\"#";

	public static string strDownArrowPre = "<sprite=\"FontSprites\" index=3 color=\"#";

	public static string strArrowPost = "\">";

	public const int COND_SMALL = 5;

	public const int COND_MEDIUM = 11;

	public const int COND_BIG = 30;

	private static string[] aPreviewStats = new string[]
	{
		"StatAchievement",
		"StatAltruism",
		"StatAutonomy",
		"StatContact",
		"StatEsteem",
		"StatFamily",
		"StatIntimacy",
		"StatMeaning",
		"StatPrivacy",
		"StatSecurity",
		"StatSelfRespect"
	};

	private Transform tfActionList;

	private TMP_Text txtPreview;

	private TMP_Text txtMessageLog;

	private TMP_Text txtEncTitle;

	private TMP_Text txtEncDesc;

	private TMP_Text txtObjectiveTitle;

	private TMP_Text txtObjective;

	private ScrollRect srLog;

	private RawImage bmpEnc;

	private CanvasGroup cgEnc;

	private CanvasGroup cgObjective;

	private GUIContextButton gcbTemplate;

	private GUIContextButton gcbTemplateA;

	private GUIContextButton gcbTemplateB;

	private ToggleGroup toggleGroup;

	private bool bPadBeforeEnd;

	private bool bIgnoreInvoke;

	private StringBuilder sbDebug;
}
