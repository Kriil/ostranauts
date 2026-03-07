using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.Chargen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Character creation career/life-path screen.
// This UI walks the player through career choices, life events, and the
// resulting resume-style summary used during chargen.
public class GUIChargenCareer : GUIData
{
	// Caches the chargen widgets, lamps, scroll panels, and reusable UI prefabs.
	protected override void Awake()
	{
		base.Awake();
		this.btnReview = base.transform.Find("btnReview").GetComponent<Button>();
		this.bmpResume = base.transform.Find("bmpResume").GetComponent<GUILamp>();
		this.bmpWarn = base.transform.Find("bmpWarn").GetComponent<GUILamp>();
		this.lblTitle = base.transform.Find("lblTitle").GetComponent<TMP_Text>();
		this.lblPageName = base.transform.Find("lblPageName").GetComponent<TMP_Text>();
		this.tfMain = base.transform.Find("pnlMain/Viewport/pnlMainContent");
		this.tfSidebar = base.transform.Find("pnlSidebar/Viewport/pnlSidebarContent");
		this.srMain = base.transform.Find("pnlMain").GetComponent<ScrollRect>();
		this.srSide = base.transform.Find("pnlSidebar").GetComponent<ScrollRect>();
		this.bmpDot1 = base.transform.Find("bmpDot1/bmpOn").GetComponent<Image>();
		this.bmpDot2 = base.transform.Find("bmpDot2/bmpOn").GetComponent<Image>();
		this.bmpDot3 = base.transform.Find("bmpDot3/bmpOn").GetComponent<Image>();
		this.bmpDot4 = base.transform.Find("bmpDot4/bmpOn").GetComponent<Image>();
		this.bmpDot2.color = Color.clear;
		this.bmpDot3.color = Color.clear;
		this.bmpDot4.color = Color.clear;
		this.cgSidebar = base.transform.Find("pnlSidebar").GetComponent<CanvasGroup>();
		this.cgSidebarAlt = base.transform.Find("pnlSidebarAlt").GetComponent<CanvasGroup>();
		this.txtSidebarAlt = base.transform.Find("pnlSidebarAlt/txt").GetComponent<TMP_Text>();
		this.btnReview.onClick.AddListener(delegate()
		{
			this.Exit();
		});
		AudioManager.AddBtnAudio(this.btnReview.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskSubmit");
		this.bmpResume.State = 0;
		this.bmpWarn.State = 3;
		this.lifeEventsOccurredSoFar = new List<string>();
		this.lblCenterBold = (Resources.Load("GUIShip/GUIChargenCareer/lblCenterBold") as GameObject);
		this.bmpLine01 = (Resources.Load("GUIShip/GUIChargenCareer/bmpLine01") as GameObject);
		this.lblLeftTMP = (Resources.Load("GUIShip/GUIChargenCareer/lblLeftTMP") as GameObject);
		this.pnlColumnLists = (Resources.Load("GUIShip/GUIChargenCareer/pnlColumnLists") as GameObject);
		this.HideSidebarAlt();
	}

	// Redraws the sidebar when the current career state changes.
	private void Update()
	{
		if (GUIChargenCareer.bRedrawSidebar)
		{
			this.UpdateSidebar();
			GUIChargenCareer.bRedrawSidebar = false;
		}
	}

	// Chooses which career page to show based on the current chargen stack state.
	private void SetUI()
	{
		this.coUser = this.COSelf.GetInteractionCurrent().objThem;
		this.cgs = this.coUser.GetComponent<GUIChargenStack>();
		this.lblTitle.text = this.cgs.GetHomeworld().strATCCode + DataHandler.GetString("GUI_CAREER_LABOR_DEPT", false);
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		GUIChargenCareer.Page page;
		if (this.cgs.bCareerEnded)
		{
			page = GUIChargenCareer.Page.Resume;
		}
		else
		{
			if (latestCareer != null)
			{
				this.PageBranchChoice();
				return;
			}
			JsonHomeworld homeworld = DataHandler.GetHomeworld("OKLG");
			if (homeworld != null)
			{
				this.lblTitle.text = homeworld.strATCCode + DataHandler.GetString("GUI_CAREER_LABOR_DEPT", false);
			}
			JsonCareer career = DataHandler.GetCareer("Shipbreaker");
			if (career != null)
			{
				this.AddCareerAndHomeworld(career, homeworld, this.cgs.Strata);
				return;
			}
			page = GUIChargenCareer.Page.List;
		}
		this.HideSidebarAlt();
		if (page != GUIChargenCareer.Page.Show)
		{
			if (page != GUIChargenCareer.Page.Resume)
			{
				if (page == GUIChargenCareer.Page.List)
				{
					this.PageListCareers();
				}
			}
			else
			{
				this.PageResume();
			}
		}
		else
		{
			this.PageCareerTermSummary();
		}
		this.UpdateSidebar();
	}

	// Finishes or warns depending on whether the resume requirements are met.
	private void Exit()
	{
		if (this.bmpResume.State == 3)
		{
			CrewSim.bUILock = false;
			CrewSim.bPauseLock = false;
			CrewSim.LowerUI(false);
			this.coUser.ZeroCondAmount("IsInChargen");
			this.coUser.ZeroCondAmount("TutorialCareerWaiting");
			BeatManager.ResetTensionTimer();
			BeatManager.ResetReleaseTimer();
		}
		else
		{
			this.bmpWarn.State = 1;
			this.QueueLamps(this.fBlinkTime);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnJobsKioskWarn", false, false);
		}
	}

	// Lamp blink coroutine used for the "cannot finish yet" warning state.
	private IEnumerator SetLamps(float time)
	{
		yield return new WaitForSeconds(time);
		if (this.bmpResume.State == 3)
		{
			this.bmpWarn.State = 0;
		}
		else
		{
			this.bmpWarn.State = 3;
			base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srMain));
		}
		yield break;
	}

	// Schedules the warning-lamp coroutine.
	private void QueueLamps(float time)
	{
		base.StartCoroutine(this.SetLamps(time));
	}

	// Shows the branch-choice page after a completed career term.
	private void PageBranchChoice()
	{
		CareerChosen jcc = this.cgs.GetLatestCareer();
		this.ClearMain();
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblCenterBold, this.tfMain);
		gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_TITLE", false);
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_DESC", false);
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
		GameObject gameObject3;
		Button component;
		GUIEnterExitHandler guienterExitHandler;
		if (this.cgs.fShipChance < 1f)
		{
			gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject3.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_LIVE_FAST", false);
			component = gameObject3.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			gameObject3.transform.GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.AddCareer(jcc.GetJC(), GUIChargenCareer.EventType.Default);
			});
			guienterExitHandler = component.gameObject.AddComponent<GUIEnterExitHandler>();
			guienterExitHandler.fnOnEnter = delegate()
			{
				this.ShowSidebarAltText(DataHandler.GetString("GUI_CAREER_SIDEBAR_LIVE_FAST", false));
			};
			guienterExitHandler.fnOnExit = delegate()
			{
				this.HideSidebarAlt();
			};
			gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject3.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_BIDE_TIME_SKILLS", false);
			component = gameObject3.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			gameObject3.transform.GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.PageWorkOnSelfSkills();
			});
			guienterExitHandler = component.gameObject.AddComponent<GUIEnterExitHandler>();
			guienterExitHandler.fnOnEnter = delegate()
			{
				this.ShowSidebarAltText(DataHandler.GetString("GUI_CAREER_SIDEBAR_WORK_ON_SKILLS", false));
			};
			guienterExitHandler.fnOnExit = delegate()
			{
				this.HideSidebarAlt();
			};
			gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject3.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_BIDE_TIME_MONEY", false);
			component = gameObject3.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			gameObject3.transform.GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.AddCareer(jcc.GetJC(), GUIChargenCareer.EventType.Money);
			});
			guienterExitHandler = component.gameObject.AddComponent<GUIEnterExitHandler>();
			guienterExitHandler.fnOnEnter = delegate()
			{
				this.ShowSidebarAltText(DataHandler.GetString("GUI_CAREER_SIDEBAR_SAVE_MONEY", false));
			};
			guienterExitHandler.fnOnExit = delegate()
			{
				this.HideSidebarAlt();
			};
		}
		gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
		gameObject3.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_SEEK_SHIP", false);
		component = gameObject3.transform.GetComponent<Button>();
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		gameObject3.transform.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.GetRandomEvent(GUIChargenCareer.EventType.Ship);
		});
		guienterExitHandler = component.gameObject.AddComponent<GUIEnterExitHandler>();
		guienterExitHandler.fnOnEnter = delegate()
		{
			this.ShowSidebarAltText(DataHandler.GetString("GUI_CAREER_SIDEBAR_SEEK_SHIP", false));
		};
		guienterExitHandler.fnOnExit = delegate()
		{
			this.HideSidebarAlt();
		};
		gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
		gameObject3.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_BIDE_TIME_RESUME", false);
		component = gameObject3.transform.GetComponent<Button>();
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		gameObject3.transform.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.PageResume();
		});
		guienterExitHandler = component.gameObject.AddComponent<GUIEnterExitHandler>();
		guienterExitHandler.fnOnEnter = delegate()
		{
			this.ShowSidebarAltText(DataHandler.GetString("GUI_CAREER_SIDEBAR_REVIEW_RESUME", false));
		};
		guienterExitHandler.fnOnExit = delegate()
		{
			this.HideSidebarAlt();
		};
		gameObject2.transform.SetAsLastSibling();
		this.UpdateSidebar();
		this.UpdateTitleStats();
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srMain));
	}

	private void GetRandomEvent(GUIChargenCareer.EventType evt)
	{
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.HideSidebarAlt();
		if (latestCareer == null || !latestCareer.bConfirmed)
		{
			this.PageCareerTermSummary();
			return;
		}
		string text = null;
		this.dictPropMap.TryGetValue(latestCareer.strJC, out text);
		string strValue = null;
		this.dictPropMap.TryGetValue(latestCareer.strJC + "Type", out strValue);
		if (text != null && text != string.Empty)
		{
			this.lifeEventsOccurredSoFar = new List<string>(text.Split(new char[]
			{
				','
			}));
		}
		string[] array = latestCareer.GetJC().aEvents;
		strValue = "Event";
		if (evt == GUIChargenCareer.EventType.Ship)
		{
			array = latestCareer.GetJC().aEventsShip;
			strValue = "Ship";
		}
		else if (evt == GUIChargenCareer.EventType.Money)
		{
			array = latestCareer.GetJC().aEventsMoney;
			strValue = "Money";
		}
		JsonLifeEvent jsonLifeEvent = null;
		int num = 0;
		int num2 = 100;
		while (array.Length > 0 && num2 > 0)
		{
			num2--;
			string text2 = array[0];
			bool flag = false;
			List<string> lootNames = DataHandler.GetLoot(text2).GetLootNames(text2, false, null);
			foreach (string text3 in lootNames)
			{
				jsonLifeEvent = DataHandler.GetLifeEvent(text3);
				if (jsonLifeEvent != null)
				{
					if (this.lifeEventsOccurredSoFar.Contains(text3))
					{
						jsonLifeEvent = null;
						num++;
						Debug.Log("skipped");
						if (num > 4)
						{
							this.lifeEventsOccurredSoFar.RemoveAt(0);
							num = 0;
							text = string.Join(",", new List<string>(this.lifeEventsOccurredSoFar).ToArray());
							base.SetPropMapData(latestCareer.strJC, text);
							break;
						}
					}
					else
					{
						Interaction interaction = DataHandler.GetInteraction(jsonLifeEvent.strInteraction, null, false);
						if (interaction != null && interaction.Triggered(this.coUser, this.coUser, false, false, false, true, null))
						{
							this.lifeEventsOccurredSoFar.Add(text3);
							text = string.Join(",", new List<string>(this.lifeEventsOccurredSoFar).ToArray());
							base.SetPropMapData(latestCareer.strJC, text);
							base.SetPropMapData(latestCareer.strJC + "Type", strValue);
							flag = true;
							break;
						}
						jsonLifeEvent = null;
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (jsonLifeEvent == null)
		{
			this.PageCareerTermSummary();
			return;
		}
		this.ClearMain();
		this.UpdateTitleStats();
		this.PageEvent(jsonLifeEvent);
	}

	public override void UpdateUI()
	{
		this.UpdateTitleStats();
	}

	private void UpdateTitleStats()
	{
		this.lblPageName.text = DataHandler.GetString("GUI_CAREER_SPECIAL_EVENTS_TITLE", false) + this.coUser.strNameFriendly;
		TMP_Text tmp_Text = this.lblPageName;
		tmp_Text.text = tmp_Text.text + DataHandler.GetString("GUI_CAREER_SPECIAL_EVENTS_TITLE_AGE", false) + Convert.ToInt32(this.coUser.GetCondAmount("StatAge"));
		TMP_Text tmp_Text2 = this.lblPageName;
		tmp_Text2.text = tmp_Text2.text + DataHandler.GetString("GUI_CAREER_SPECIAL_EVENTS_TITLE_FUNDS", false) + this.coUser.GetCondAmount("StatUSD").ToString("#.00");
	}

	private void PageEvent(JsonLifeEvent jle)
	{
		if (jle == null)
		{
			this.PageCareerTermSummary();
		}
		Interaction interaction = DataHandler.GetInteraction(jle.strInteraction, null, false);
		if (interaction == null || !interaction.Triggered(this.coUser, this.coUser, false, false, false, true, null))
		{
			this.PageCareerTermSummary();
			return;
		}
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		int count = latestCareer.aEvents.Count;
		interaction.objUs = this.coUser;
		interaction.objThem = this.coUser;
		List<string> list = new List<string>(this.coUser.mapConds.Keys);
		interaction.ApplyEffects(latestCareer.aEvents, false);
		foreach (Condition condition in this.coUser.mapConds.Values)
		{
			if (condition.nDisplaySelf == 2 && list.IndexOf(condition.strName) < 0 && latestCareer.aSkillsChosen.IndexOf(condition.strName) < 0 && condition.strName.IndexOf("Dc") != 0)
			{
				latestCareer.aSkillsChosen.Add(condition.strName);
			}
		}
		if (interaction.aSocialChangelog != null)
		{
			latestCareer.aSocials.AddRange(interaction.aSocialChangelog);
		}
		if (jle.fCashRewardMax != 0f)
		{
			latestCareer.fCashReward = MathUtils.Rand(jle.fCashRewardMin, jle.fCashRewardMax, MathUtils.RandType.Flat, null);
		}
		else
		{
			latestCareer.fCashReward = jle.fCashRewardMin;
		}
		if (latestCareer.fCashReward != 0f)
		{
			LedgerLI li = new LedgerLI(this.coUser.strID, "Career Event", latestCareer.fCashReward, interaction.strTitle, GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
			this.coUser.AddCondAmount(GUIFinance.strCondCurr, (double)latestCareer.fCashReward, 0.0, 0f);
			Ledger.AddLI(li);
		}
		latestCareer.aShips = DataHandler.GetLoot(jle.strShipRewards).GetLootNames(null, false, null);
		latestCareer.strStartATC = jle.strStartATC;
		latestCareer.fStartATCRange = jle.fStartATCRange;
		latestCareer.fShipDmgMax = jle.fShipDmgMax;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblCenterBold, this.tfMain);
		gameObject.GetComponent<Text>().text = interaction.strTitle;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		string text = string.Empty;
		for (int i = count; i < latestCareer.aEvents.Count; i++)
		{
			if (text.Length > 0)
			{
				text += "\n";
			}
			text += latestCareer.aEvents[i];
		}
		gameObject.GetComponent<Text>().text = text;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
		JsonLifeEvent jsonLifeEvent = null;
		if (interaction.aInverse.Length == 0)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_OK", false);
			JsonShip jShip = null;
			if (latestCareer.aShips.Count > 0)
			{
				jShip = DataHandler.GetShip(latestCareer.aShips[0]);
			}
			Button component = gameObject.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			if (jShip != null)
			{
				component.onClick.AddListener(delegate()
				{
					this.SelectShip(jShip);
				});
			}
			else
			{
				latestCareer.bTermEnded = true;
				component.onClick.AddListener(delegate()
				{
					this.PageBranchChoice();
				});
			}
		}
		else
		{
			List<JsonLifeEvent> list2 = new List<JsonLifeEvent>();
			List<Interaction> list3 = new List<Interaction>();
			bool flag = false;
			foreach (string strName in interaction.aInverse)
			{
				JsonLifeEvent lifeEvent = DataHandler.GetLifeEvent(strName);
				if (lifeEvent != null)
				{
					Interaction interaction2 = DataHandler.GetInteraction(lifeEvent.strInteraction, null, false);
					if (interaction2.Triggered(this.coUser, this.coUser, false, false, false, true, null))
					{
						list2.Add(lifeEvent);
						list3.Add(interaction2);
						if (lifeEvent.strShipRewards != string.Empty && this.cgs.fShipChance >= 1f)
						{
							flag = true;
						}
					}
				}
			}
			for (int k = 0; k < list2.Count; k++)
			{
				JsonLifeEvent jleNext = list2[k];
				if (!flag || !(jleNext.strShipRewards == string.Empty))
				{
					if (jleNext.strShipRewards != string.Empty)
					{
						jsonLifeEvent = jleNext;
					}
					GameObject rowBtn = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
					rowBtn.transform.Find("Text").GetComponent<TMP_Text>().text = list3[k].strTitle;
					Button component2 = rowBtn.transform.GetComponent<Button>();
					AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
					rowBtn.transform.GetComponent<Button>().onClick.AddListener(delegate()
					{
						this.ClickEvent(rowBtn, jleNext);
					});
					if (rowBtn.transform.GetComponentInChildren<TMP_Text>().text == "Take Ship")
					{
						rowBtn.transform.SetSiblingIndex(0);
					}
				}
			}
		}
		latestCareer.fShipMortgage = jle.fShipMortgage;
		latestCareer.bShipOwned = jle.bShipOwned;
		if (jsonLifeEvent != null)
		{
			Loot loot = DataHandler.GetLoot(jsonLifeEvent.strShipRewards);
			List<string> list4 = new List<string>();
			if (loot != null)
			{
				list4 = loot.GetLootNames(null, false, null);
			}
			JsonShip jsonShip = null;
			if (list4.Count > 0)
			{
				jsonShip = DataHandler.GetShip(list4[0]);
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = "missing";
			if (jsonShip != null)
			{
				stringBuilder.Append("Make: ");
				stringBuilder.AppendLine(jsonShip.make);
				stringBuilder.Append("Model: ");
				stringBuilder.AppendLine(jsonShip.model);
				stringBuilder.Append("Year: ");
				stringBuilder.AppendLine(jsonShip.year);
				stringBuilder.Append("Designation: ");
				stringBuilder.AppendLine(jsonShip.designation);
				stringBuilder.Append("Dimensions: ");
				stringBuilder.AppendLine(jsonShip.dimensions);
				stringBuilder.Append("Mass: ");
				stringBuilder.Append(jsonShip.fShallowMass);
				stringBuilder.AppendLine(" (kg)");
				stringBuilder.Append("RCS Count: ");
				stringBuilder.AppendLine(jsonShip.nRCSCount.ToString());
				stringBuilder.Append("Torch Drive: ");
				if (jsonShip.bFusionTorch)
				{
					stringBuilder.AppendLine("Yes");
				}
				else
				{
					stringBuilder.AppendLine("No");
				}
				stringBuilder.Append("Location: ");
				stringBuilder.AppendLine(jsonLifeEvent.strStartATC);
				stringBuilder.Append("Docked: ");
				if (jsonLifeEvent.fStartATCRange > 0f)
				{
					stringBuilder.AppendLine("No");
				}
				else
				{
					stringBuilder.AppendLine("Yes");
				}
				stringBuilder.AppendLine();
				stringBuilder.Append("Mortgage: ");
				if (jsonLifeEvent.fShipMortgage > 0f)
				{
					stringBuilder.AppendLine(jsonLifeEvent.fShipMortgage.ToString("n"));
					float num = MathUtils.MortgagePaymentPerShift(jsonLifeEvent.fShipMortgage);
					stringBuilder.Append("Payment per shift: ");
					stringBuilder.AppendLine(num.ToString("n"));
				}
				else
				{
					stringBuilder.AppendLine("N/A");
				}
				text2 = list4[0];
			}
			else
			{
				stringBuilder.AppendLine("NO DATA");
			}
			original = (Resources.Load("GUIShip/GUIChargenCareer/pnlShipInfo") as GameObject);
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			gameObject.GetComponentInChildren<TMP_Text>().text = stringBuilder.ToString();
			gameObject.GetComponentInChildren<RawImage>().texture = DataHandler.LoadPNG(string.Concat(new string[]
			{
				"/ships/",
				text2,
				"/",
				text2,
				".png"
			}), false, false);
		}
		gameObject2.transform.SetAsLastSibling();
		this.UpdateSidebar();
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srMain));
	}

	private void ClickEvent(GameObject goBtn, JsonLifeEvent jleNext)
	{
		this.PageEvent(jleNext);
		IEnumerator enumerator = goBtn.transform.parent.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				Button component = transform.GetComponent<Button>();
				AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
				if (!(component == null))
				{
					component.onClick.RemoveAllListeners();
					component.interactable = false;
				}
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
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srMain));
	}

	private void PageResume()
	{
		this.bmpResume.State = 0;
		if (this.cgs.bCareerEnded)
		{
			this.bmpDot4.color = Color.white;
		}
		if (this.cgs.strRegIDChosen != null)
		{
			this.bmpDot3.color = Color.white;
		}
		this.ClearMain();
		this.HideSidebarAlt();
		this.RemoveCareer();
		this.lblPageName.text = DataHandler.GetString("GUI_CAREER_RESUME_TITLE", false);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfMain);
		gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_NAME", false) + this.coUser.strName;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		GameObject gameObject2 = Resources.Load("GUIShip/GUIChargenCareer/pnlCareerContact") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfMain);
		JsonHomeworld homeworld = this.cgs.GetHomeworld();
		int num = Mathf.FloorToInt(Convert.ToSingle(this.coUser.GetCondAmount("StatAge")));
		string text = string.Concat(new object[]
		{
			DataHandler.GetString("GUI_CAREER_SIDEBAR_AGE", false),
			num,
			"\n",
			DataHandler.GetString("GUI_CAREER_SIDEBAR_HOMEWORLD", false),
			homeworld.strColonyName,
			"\n",
			DataHandler.GetString("GUI_CAREER_SIDEBAR_NET_WORTH", false),
			this.coUser.GetCondAmount("StatUSD").ToString("#.00"),
			"\n"
		});
		gameObject.GetComponent<TMP_Text>().text = text;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		List<string> list = new List<string>();
		List<string> allLootNames = DataHandler.GetLoot("CONDSocialGUIFilter").GetAllLootNames();
		foreach (string text2 in allLootNames)
		{
			if (this.coUser.HasCond(text2))
			{
				list.Add(text2);
			}
		}
		List<string> list2 = new List<string>();
		List<string> allLootNames2 = DataHandler.GetLoot("CONDSocialGUIFilterSkills").GetAllLootNames();
		foreach (string text3 in allLootNames2)
		{
			if (this.coUser.HasCond(text3))
			{
				list2.Add(text3);
			}
		}
		gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfMain);
		GameObject original;
		if (list.Count > 0)
		{
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_RESUME_TRAITS", false);
			original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
			GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			original = (Resources.Load("GUIShip/GUIChargenCareer/lblCenter") as GameObject);
			foreach (string strName in list)
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject3.transform);
				gameObject.GetComponent<Text>().text = DataHandler.GetCond(strName).strNameFriendly;
			}
		}
		else
		{
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_RESUME_TRAITS", false) + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS_NONE", false);
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfMain);
		if (list2.Count > 0)
		{
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_RESUME_SKILLS", false);
			original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
			GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			original = (Resources.Load("GUIShip/GUIChargenCareer/lblCenter") as GameObject);
			foreach (string strName2 in list2)
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject4.transform);
				gameObject.GetComponent<Text>().text = DataHandler.GetCond(strName2).strNameFriendly;
			}
		}
		else
		{
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_RESUME_SKILLS", false) + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS_NONE", false);
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		if (!this.cgs.bCareerEnded)
		{
			original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
			GameObject gameObject5 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject5.transform);
			gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_OK", false);
			Button component = gameObject.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			component.onClick.AddListener(delegate()
			{
				this.PageBranchChoice();
			});
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_EVENTS", false);
		bool flag = true;
		foreach (CareerChosen careerChosen in this.cgs.aCareers)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			if (flag)
			{
				flag = false;
				gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_RESUME_EVENT_1", false) + DataHandler.GetString("GUI_CAREER_RESUME_EVENT_EARLY", false);
			}
			else
			{
				gameObject.GetComponent<Text>().text = string.Concat(new object[]
				{
					DataHandler.GetString("GUI_CAREER_RESUME_EVENT_1", false),
					careerChosen.nAge,
					DataHandler.GetString("GUI_CAREER_RESUME_EVENT_2", false),
					careerChosen.GetJC().strNameFriendly
				});
			}
			foreach (string text4 in careerChosen.aEvents)
			{
				string text5 = text4;
				bool flag2 = false;
				if (text5.IndexOf("-") == 0)
				{
					flag2 = true;
					text5 = text5.Substring(1);
				}
				if (allLootNames.IndexOf(text5) >= 0 || allLootNames2.IndexOf(text5) >= 0)
				{
					Condition cond = DataHandler.GetCond(text5);
					if (cond != null)
					{
						text5 = cond.strNameFriendly;
					}
					if (flag2)
					{
						text5 = DataHandler.GetString("GUI_CAREER_RESUME_WORK_SELF", false) + DataHandler.GetString("GUI_CAREER_RESUME_WORK_SELF_REMOVE", false) + text5;
					}
					else
					{
						text5 = DataHandler.GetString("GUI_CAREER_RESUME_WORK_SELF", false) + text5;
					}
				}
				gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
				gameObject.GetComponent<Text>().text = text5;
			}
		}
		if (this.cgs.bCareerEnded)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			string str = string.Empty;
			Ship ship = null;
			if (CrewSim.system.dictShips.TryGetValue(this.cgs.strRegIDChosen, out ship))
			{
				str = string.Concat(new string[]
				{
					" (\"",
					ship.json.make,
					" ",
					ship.json.model,
					"\") "
				});
			}
			gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_SHIP_ACQUIRED_1", false) + this.cgs.strRegIDChosen + str + DataHandler.GetString("GUI_CAREER_SIDEBAR_SHIP_ACQUIRED_2", false);
			ship = null;
			original = (Resources.Load("GUIShip/GUIChargenCareer/lblCenter") as GameObject);
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_PROCEED_LAUNCH", false);
			this.bmpResume.State = 3;
			this.bmpWarn.State = 0;
			AudioManager.am.PlayAudioEmitter("ShipUIBtnJobsKioskReady", false, false);
		}
		else
		{
			original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
			GameObject gameObject6 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject6.transform);
			gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_OK", false);
			Button component2 = gameObject.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			component2.onClick.AddListener(delegate()
			{
				this.PageBranchChoice();
			});
		}
	}

	private void PageListCareers()
	{
		this.bmpResume.State = 0;
		this.ClearMain();
		this.RemoveCareer();
		if (this.cgs.bCareerEnded)
		{
			this.PageResume();
			return;
		}
		this.lblPageName.text = "Choose Another Qualification";
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/GUIChargenCareerRow") as GameObject;
		using (Dictionary<string, JsonCareer>.ValueCollection.Enumerator enumerator = DataHandler.dictCareers.Values.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				JsonCareer jc = enumerator.Current;
				GUIChargenCareer $this = this;
				if (!jc.bHide)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
					Button component = gameObject.transform.Find("btnCareer").GetComponent<Button>();
					AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
					TMP_Text component2 = component.transform.Find("Text").GetComponent<TMP_Text>();
					component2.text = jc.strNameFriendly;
					component2 = gameObject.transform.Find("lblReqs").GetComponent<TMP_Text>();
					if (jc.bComingSoon)
					{
						component.interactable = false;
						string strNameFriendly = jc.strNameFriendly;
						if (strNameFriendly == null)
						{
							goto IL_20B;
						}
						if (!(strNameFriendly == "Prisoner"))
						{
							if (!(strNameFriendly == "Bartender"))
							{
								if (!(strNameFriendly == "Criminal"))
								{
									if (!(strNameFriendly == "Law Enforcement Officer"))
									{
										if (!(strNameFriendly == "Manager"))
										{
											if (!(strNameFriendly == "Pirate"))
											{
												goto IL_20B;
											}
											component2.text = "No Ship";
										}
										else
										{
											component2.text = "No Rich Friends";
										}
									}
									else
									{
										component2.text = "Too Dirty";
									}
								}
								else
								{
									component2.text = "Too Clean";
								}
							}
							else
							{
								component2.text = "Unlicensed";
							}
						}
						else
						{
							component2.text = "At Large";
						}
						continue;
						IL_20B:
						component2.text = "Coming Soon";
					}
					else if (DataHandler.GetCondTrigger(jc.strCTPrereqs).Triggered(this.coUser, null, true))
					{
						component.onClick.AddListener(delegate()
						{
							$this.AddCareer(jc, GUIChargenCareer.EventType.Default);
						});
						component2.text = "<#3E8703>Available</color>";
					}
				}
			}
		}
		base.transform.Find("pnlMain").GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
	}

	private void MakeSkillBtnGrid(List<List<Tuple<Condition, int>>> skillsAvailable)
	{
		this._dictCheckmarks.Clear();
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		if (latestCareer != null)
		{
			JsonCareer jc = latestCareer.GetJC();
		}
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject;
		GameObject original2 = Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject;
		GameObject original3 = Resources.Load("GUIShip/GUIChargenCareer/chkBlueMulti") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		gameObject.GetComponent<Text>().text = DataHandler.GetString("GUI_CAREER_NEXT_STEPS_BIDE_TIME_SKILLS_DESC", false);
		for (int i = 0; i < skillsAvailable.Count; i++)
		{
			List<Tuple<Condition, int>> list = skillsAvailable[i];
			if (list.Count != 0)
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
				string text = string.Empty;
				if (i == 0)
				{
					text = "Skills:";
				}
				else if (i == 1)
				{
					text = "Hobbies:";
				}
				else
				{
					text = "Traits:";
				}
				Text component = gameObject.GetComponent<Text>();
				component.text = text;
				component.fontStyle = FontStyle.Bold;
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, this.tfMain);
				GridLayoutGroup component2 = gameObject2.GetComponent<GridLayoutGroup>();
				component2.cellSize = new Vector2(212f, 28f);
				foreach (Tuple<Condition, int> tuple in list)
				{
					Condition cond = tuple.Item1;
					int item = tuple.Item2;
					if (cond == null)
					{
						gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject2.transform);
						gameObject.GetComponent<TMP_Text>().text = string.Empty;
					}
					else
					{
						string text2 = cond.strNameFriendly;
						text2 = text2.Replace("Skilled in ", string.Empty);
						text2 = text2.Replace("Skilled ", string.Empty);
						gameObject = UnityEngine.Object.Instantiate<GameObject>(original3, gameObject2.transform);
						GUISkillToggle component3 = gameObject.GetComponent<GUISkillToggle>();
						component3.SetupToggle(text2, this.coUser, item, delegate(bool on)
						{
							this.OnCondToggled(on, cond);
						}, cond);
						this._dictCheckmarks[cond.strName] = component3;
					}
				}
			}
		}
		this.AddCancelButton();
	}

	private double GetPreviousToggleValue(int currentIndex, string cond, double currentChange)
	{
		double num = this.coUser.GetCondAmount(cond);
		float fClampMax = DataHandler.GetCond(cond).fClampMax;
		for (int i = 0; i < currentIndex; i++)
		{
			SkillSelectionDTO skillSelectionDTO = this._selectedSkills[i];
			if (skillSelectionDTO.CondName == cond)
			{
				num += (double)skillSelectionDTO.Change;
				if (num > (double)fClampMax)
				{
					num = (double)fClampMax;
				}
				else if (num < 0.0)
				{
					num = 0.0;
				}
			}
			if (skillSelectionDTO.AgeConds != null)
			{
				foreach (KeyValuePair<string, double> keyValuePair in skillSelectionDTO.AgeConds)
				{
					if (keyValuePair.Key == cond)
					{
						num += keyValuePair.Value;
						if (num > (double)fClampMax)
						{
							num = (double)fClampMax;
						}
						else if (num < 0.0)
						{
							num = 0.0;
						}
					}
				}
			}
		}
		num += currentChange;
		if (num > (double)fClampMax)
		{
			num = (double)fClampMax;
		}
		return num;
	}

	private void OnCondToggled(bool isOn, Condition cond)
	{
		if (cond == null)
		{
			return;
		}
		bool flag = false;
		SkillSelectionDTO skillSelectionDTO = this._selectedSkills.LastOrDefault<SkillSelectionDTO>();
		if (skillSelectionDTO != null && skillSelectionDTO.CondName == cond.strName)
		{
			if (skillSelectionDTO.AgeConds != null)
			{
				foreach (KeyValuePair<string, double> keyValuePair in skillSelectionDTO.AgeConds)
				{
					this.UpdateToggle(DataHandler.GetCond(keyValuePair.Key), this.GetPreviousToggleValue(this._selectedSkills.Count - 1, keyValuePair.Key, -keyValuePair.Value));
				}
			}
			this._selectedSkills.RemoveAt(this._selectedSkills.Count - 1);
			flag = true;
		}
		if (!flag)
		{
			this._selectedSkills.Add(new SkillSelectionDTO(cond, (!isOn) ? -1 : 1));
		}
		GUISkillToggle guiskillToggle;
		if (cond.strAnti != null && this._dictCheckmarks.TryGetValue(cond.strAnti, out guiskillToggle) && guiskillToggle != null)
		{
			guiskillToggle.ToggleLockIcon(isOn);
		}
		GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		this.RebuildMultiSelectSidebar();
	}

	private void AddCancelButton()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, gameObject.transform);
		TMP_Text component = gameObject2.transform.Find("Text").GetComponent<TMP_Text>();
		component.enableAutoSizing = false;
		component.fontSize = 15f;
		component.text = DataHandler.GetString("GUI_CAREER_CANCEL", false);
		Button component2 = gameObject2.transform.GetComponent<Button>();
		AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		component2.onClick.AddListener(delegate()
		{
			this.ClearMain();
			this.HideSidebarAlt();
			this._selectedSkills.Clear();
			this.PageBranchChoice();
		});
	}

	private void RebuildMultiSelectSidebar()
	{
		if (this._selectedSkills.Count == 0)
		{
			this.UpdateSidebar();
			return;
		}
		IEnumerator enumerator = this.tfSidebar.GetEnumerator();
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
		VerticalLayoutGroup component = this.tfSidebar.GetComponent<VerticalLayoutGroup>();
		component.spacing = 2f;
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.tfSidebar.GetComponent<RectTransform>());
		string @string = DataHandler.GetString("GUI_CAREER_SIDEBAR_COST_2", false);
		int num = 0;
		this.SpawnSideBarHeader();
		foreach (SkillSelectionDTO skillSelectionDTO in this._selectedSkills)
		{
			int num2 = this.GetTraitYears(skillSelectionDTO.CondName) * skillSelectionDTO.Change;
			skillSelectionDTO.AgeConds = this.GetAgeRelatedConds(num, num2);
			this.SpawnAgeRelatedConds(skillSelectionDTO.AgeConds);
			GUISkillToggle guiskillToggle;
			if (this._dictCheckmarks.TryGetValue(skillSelectionDTO.CondName, out guiskillToggle))
			{
				this.UpdateToggle(skillSelectionDTO.Condition, (double)skillSelectionDTO.Change);
			}
			num += num2;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.pnlColumnLists, this.tfSidebar);
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject.transform);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<b>");
			if (skillSelectionDTO.Change < 0)
			{
				stringBuilder.Append("- ");
			}
			else
			{
				stringBuilder.Append("+ ");
			}
			stringBuilder.Append(skillSelectionDTO.Condition.strNameFriendly);
			stringBuilder.AppendLine("</b> ");
			gameObject2.GetComponent<TMP_Text>().text = stringBuilder.ToString();
			GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject.transform);
			stringBuilder = new StringBuilder();
			stringBuilder.Append(num2);
			stringBuilder.AppendLine(@string);
			TMP_Text component2 = gameObject3.GetComponent<TMP_Text>();
			component2.alignment = TextAlignmentOptions.Right;
			component2.text = stringBuilder.ToString();
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
		GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
		TMP_Text component3 = gameObject4.GetComponent<TMP_Text>();
		component3.alignment = TextAlignmentOptions.Right;
		component3.text = "<b>Total Cost: </b>" + num.ToString() + @string;
		this.AddApplyClearButtonSection(num);
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srSide));
	}

	private void SpawnSideBarHeader()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
		gameObject.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
		gameObject.GetComponent<TMP_Text>().text = "Summary";
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject;
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfSidebar);
		GridLayoutGroup component = gameObject2.GetComponent<GridLayoutGroup>();
		component.cellSize = new Vector2(100f, 7.5f);
		GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject2.transform);
		TMP_Text component2 = gameObject3.GetComponent<TMP_Text>();
		component2.text = "Selected Skills";
		component2.alignment = TextAlignmentOptions.Center;
		component2.fontStyle = FontStyles.Bold;
		component2.fontSize = 12f;
		GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject2.transform);
		TMP_Text component3 = gameObject4.GetComponent<TMP_Text>();
		component3.text = string.Empty;
		GameObject gameObject5 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject2.transform);
		TMP_Text component4 = gameObject5.GetComponent<TMP_Text>();
		component4.text = "Costs";
		component4.alignment = TextAlignmentOptions.Center;
		component4.fontStyle = FontStyles.Bold;
		component4.fontSize = 12f;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
	}

	private void SpawnAgeRelatedConds(Dictionary<string, double> ageConds)
	{
		if (ageConds == null || ageConds.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, double> keyValuePair in ageConds)
		{
			Condition cond = DataHandler.GetCond(keyValuePair.Key);
			if (cond != null && !string.IsNullOrEmpty(cond.strNameFriendly) && cond.nDisplaySelf != 0)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
				StringBuilder stringBuilder = new StringBuilder();
				if (keyValuePair.Key.StartsWith("Dc"))
				{
					stringBuilder.AppendLine("<color=red>" + GrammarUtils.GetInflectedString(cond.strDesc, cond, this.coUser) + "</color>");
				}
				else
				{
					string text = (keyValuePair.Value <= 0.0) ? string.Empty : "+";
					stringBuilder.AppendLine(string.Concat(new string[]
					{
						"<color=red>",
						cond.strNameFriendly,
						" ",
						text,
						keyValuePair.Value.ToString("F2"),
						"</color>"
					}));
					this.UpdateToggle(cond, keyValuePair.Value);
				}
				TMP_Text component = gameObject.GetComponent<TMP_Text>();
				component.alignment = TextAlignmentOptions.Right;
				component.text = stringBuilder.ToString();
			}
		}
	}

	private CondRuleThresh GetChangedCRThresh(int yearsAddedPreviously, int yearsAddedNew)
	{
		CondRule condRule = this.coUser.GetCondRule("StatAge");
		int num = Mathf.FloorToInt(Convert.ToSingle(this.coUser.GetCondAmount("StatAge")));
		CondRuleThresh currentThresh = condRule.GetCurrentThresh(this.coUser, (double)(num + yearsAddedPreviously));
		CondRuleThresh currentThresh2 = condRule.GetCurrentThresh(this.coUser, (double)(num + yearsAddedPreviously + yearsAddedNew));
		return (currentThresh != currentThresh2) ? currentThresh2 : null;
	}

	private Dictionary<string, double> GetAgeRelatedConds(int yearsAddedPreviously, int yearsAddedNew)
	{
		CondRuleThresh changedCRThresh = this.GetChangedCRThresh(yearsAddedPreviously, yearsAddedNew);
		if (changedCRThresh == null)
		{
			return null;
		}
		Dictionary<string, double> condLoot;
		if (!this._promisedAgeLoot.TryGetValue(changedCRThresh.strLootNew, out condLoot))
		{
			Loot loot = DataHandler.GetLoot(changedCRThresh.strLootNew);
			condLoot = loot.GetCondLoot(1f, null, null);
			this._promisedAgeLoot[changedCRThresh.strLootNew] = condLoot;
		}
		return condLoot;
	}

	private void UpdateToggle(Condition cond, double change)
	{
		GUISkillToggle guiskillToggle;
		this._dictCheckmarks.TryGetValue(cond.strName, out guiskillToggle);
		if (guiskillToggle == null)
		{
			return;
		}
		GUISkillToggle guiskillToggle2;
		if (cond.strAnti != null && this._dictCheckmarks.TryGetValue(cond.strAnti, out guiskillToggle2) && guiskillToggle2 != null)
		{
			if (guiskillToggle2.IsOn)
			{
				guiskillToggle2.ToggleChkSilently(false);
				guiskillToggle.ToggleLockIcon(false);
				return;
			}
			guiskillToggle2.ToggleLockIcon(change > 0.0);
		}
		if (guiskillToggle != null)
		{
			if ((change <= 0.0 || !guiskillToggle.IsOn) && (change >= 0.0 || guiskillToggle.IsOn))
			{
				if ((change > 0.0 && !guiskillToggle.IsOn) || (change < 0.0 && guiskillToggle.IsOn))
				{
					guiskillToggle.ToggleChkSilently(change > 0.0);
				}
			}
		}
	}

	private void AddApplyClearButtonSection(int currentTotal)
	{
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfSidebar);
		GridLayoutGroup component = gameObject.GetComponent<GridLayoutGroup>();
		component.cellSize = new Vector2(100f, 25f);
		GameObject original2 = Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject;
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, gameObject.transform);
		TMP_Text component2 = gameObject2.transform.Find("Text").GetComponent<TMP_Text>();
		component2.text = "Apply";
		component2.enableAutoSizing = false;
		component2.fontSize = 15f;
		Button component3 = gameObject2.transform.GetComponent<Button>();
		component3.interactable = (currentTotal >= 0);
		AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		JsonCareer jc = null;
		if (latestCareer != null)
		{
			jc = latestCareer.GetJC();
		}
		component3.onClick.AddListener(delegate()
		{
			foreach (SkillSelectionDTO skillSelectionDTO in this._selectedSkills)
			{
				string strChosen = ((skillSelectionDTO.Change >= 0) ? string.Empty : "-") + skillSelectionDTO.CondName;
				this.AddSkillTrait(jc, strChosen);
			}
			this._selectedSkills.Clear();
			this.ClearMain();
			this.HideSidebarAlt();
			this.PageBranchChoice();
		});
		gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, gameObject.transform);
		component2 = gameObject2.transform.Find("Text").GetComponent<TMP_Text>();
		component2.text = "Undo Last";
		component2.enableAutoSizing = false;
		component2.fontSize = 15f;
		component3 = gameObject2.transform.GetComponent<Button>();
		AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		component3.onClick.AddListener(delegate()
		{
			SkillSelectionDTO skillSelectionDTO = this._selectedSkills.LastOrDefault<SkillSelectionDTO>();
			if (skillSelectionDTO != null)
			{
				GUISkillToggle guiskillToggle = null;
				if (this._dictCheckmarks.TryGetValue(skillSelectionDTO.CondName, out guiskillToggle))
				{
					guiskillToggle.IsOn = !guiskillToggle.IsOn;
				}
			}
		});
		gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, gameObject.transform);
		component2 = gameObject2.transform.Find("Text").GetComponent<TMP_Text>();
		component2.text = "Clear";
		component2.enableAutoSizing = false;
		component2.fontSize = 15f;
		component3 = gameObject2.transform.GetComponent<Button>();
		AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		component3.onClick.AddListener(delegate()
		{
			this._selectedSkills.Clear();
			this.RebuildMultiSelectSidebar();
			this.PageWorkOnSelfSkills();
		});
		if (currentTotal < 0)
		{
			gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
			component2 = gameObject2.GetComponent<TMP_Text>();
			component2.alignment = TextAlignmentOptions.Center;
			component2.text = "Total cost cannot be negative";
			component2.enableAutoSizing = false;
			component2.fontSize = 10f;
		}
	}

	private void AddSkillTrait(JsonCareer jc, string strChosen)
	{
		this.bmpDot2.color = Color.white;
		List<string> list = new List<string>(this.coUser.mapConds.Keys);
		this.cgs.AddCareer(jc);
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.cgs.ApplyCareer(this.coUser, latestCareer, true);
		latestCareer.bTermEnded = true;
		latestCareer.aEvents.Add(strChosen);
		double num = 1.0;
		if (strChosen != null && strChosen.IndexOf("-") == 0)
		{
			strChosen = strChosen.Substring(1);
			num = -1.0;
		}
		int num2 = this.GetTraitYears(strChosen) * (int)num;
		CondRuleThresh changedCRThresh = this.GetChangedCRThresh(0, num2);
		Dictionary<string, double> dictionary;
		if (changedCRThresh != null && this._promisedAgeLoot.TryGetValue(changedCRThresh.strLootNew, out dictionary) && dictionary != null)
		{
			foreach (KeyValuePair<string, double> keyValuePair in dictionary)
			{
				this.coUser.AddCondAmount(keyValuePair.Key, keyValuePair.Value, 0.0, 0f);
			}
		}
		this.coUser.bFreezeCondRules = true;
		this.coUser.AddCondAmount("StatAge", (double)num2, 0.0, 0f);
		this.coUser.bFreezeCondRules = false;
		this.coUser.AddCondAmount(strChosen, num, 0.0, 0f);
		foreach (Condition condition in this.coUser.mapConds.Values)
		{
			if (list.IndexOf(condition.strName) < 0 && latestCareer.aSkillsChosen.IndexOf(condition.strName) < 0 && condition.nDisplaySelf == 2 && condition.strName.IndexOf("Dc") != 0)
			{
				latestCareer.aSkillsChosen.Add(condition.strName);
			}
		}
	}

	private void ShowSidebarAltText(string str)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(str);
		this.txtSidebarAlt.text = stringBuilder.ToString();
		CanvasManager.ShowCanvasGroup(this.cgSidebarAlt);
		CanvasManager.HideCanvasGroup(this.cgSidebar);
	}

	private void HideSidebarAlt()
	{
		CanvasManager.ShowCanvasGroup(this.cgSidebar);
		CanvasManager.HideCanvasGroup(this.cgSidebarAlt);
	}

	private void UpdateSidebar()
	{
		IEnumerator enumerator = this.tfSidebar.GetEnumerator();
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
		VerticalLayoutGroup component = this.tfSidebar.GetComponent<VerticalLayoutGroup>();
		component.spacing = 4f;
		int year = CrewSim.system.GetYear();
		int num = Mathf.FloorToInt(Convert.ToSingle(this.coUser.GetCondAmount("StatAge")));
		int num2 = year - num;
		int nFoundingYear = this.cgs.GetHomeworld().nFoundingYear;
		int num3 = year - nFoundingYear;
		if (this.curveShipChances == null)
		{
			this.curveShipChances = Resources.Load<AnimationCurveAsset>("Curves/ShipEventChances");
		}
		this.cgs.fShipChance = this.curveShipChances.curve.Evaluate(1f * (float)(num - 18) / (float)(num3 - 18));
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
		gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_NAME", false) + this.coUser.strName;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/pnlCareerContact") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
		JsonHomeworld homeworld = this.cgs.GetHomeworld();
		string text = string.Concat(new object[]
		{
			DataHandler.GetString("GUI_CAREER_SIDEBAR_AGE", false),
			num,
			"\n",
			DataHandler.GetString("GUI_CAREER_SIDEBAR_HOMEWORLD", false),
			homeworld.strColonyName,
			"\n",
			DataHandler.GetString("GUI_CAREER_SIDEBAR_NET_WORTH", false),
			this.coUser.GetCondAmount("StatUSD").ToString("#.00"),
			"\n"
		});
		if (this.cgs.aHomeworldTraits.Count > 0)
		{
			text += DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS", false);
		}
		else
		{
			text = text + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS", false) + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS_NONE", false);
		}
		for (int i = 0; i < this.cgs.aHomeworldTraits.Count; i++)
		{
			if (i > 0)
			{
				text += ", ";
			}
			Condition cond = DataHandler.GetCond(this.cgs.aHomeworldTraits[i]);
			if (cond != null)
			{
				text += cond.strNameFriendly;
				if (i == this.cgs.aHomeworldTraits.Count - 1)
				{
					text += ".\n";
				}
			}
		}
		gameObject.GetComponent<TMP_Text>().text = text;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
		foreach (CareerChosen careerChosen in this.cgs.aCareers)
		{
			if (careerChosen.bConfirmed && careerChosen.bTermEnded && careerChosen.aEvents.Count != 0)
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
				text = string.Empty;
				if (careerChosen.bFirst)
				{
					text = text + DataHandler.GetString("GUI_CAREER_SIDEBAR_CAREER_CHANGE", false) + careerChosen.GetJC().strNameFriendly + "\n";
				}
				else
				{
					text = text + DataHandler.GetString("GUI_CAREER_SIDEBAR_CAREER_CONT", false) + careerChosen.GetJC().strNameFriendly + "\n";
				}
				if (careerChosen.aSkillsChosen.Count > 0)
				{
					text += DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS", false);
				}
				else
				{
					text = text + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS", false) + DataHandler.GetString("GUI_CAREER_SIDEBAR_SKILLS_TRAITS_NONE", false);
				}
				for (int j = 0; j < careerChosen.aSkillsChosen.Count; j++)
				{
					if (j > 0)
					{
						text += ", ";
					}
					Condition cond2 = DataHandler.GetCond(careerChosen.aSkillsChosen[j]);
					text += cond2.strNameFriendly;
					if (j == careerChosen.aSkillsChosen.Count - 1)
					{
						text += ".\n";
					}
				}
				if (careerChosen.fCashReward != 0f)
				{
					text = text + DataHandler.GetString("GUI_CAREER_SIDEBAR_REWARDS", false) + careerChosen.fCashReward.ToString("#.00") + "\n";
				}
				if (careerChosen.aSocials.Count > 0)
				{
					text += DataHandler.GetString("GUI_CAREER_SIDEBAR_NOTABLE_CONTACTS", false);
				}
				gameObject.GetComponent<TMP_Text>().text = text;
				if (careerChosen.aSocials.Count > 0)
				{
					for (int k = 0; k < careerChosen.aSocials.Count - 2; k += 3)
					{
						gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfSidebar);
						string text2 = careerChosen.aSocials[k];
						string text3 = careerChosen.aSocials[k + 1];
						string text4 = careerChosen.aSocials[k + 2];
						CondOwner condOwner = null;
						if (text2 != null && text3 != null && DataHandler.mapCOs.TryGetValue(text2, out condOwner))
						{
							GUIChargenStack component2 = condOwner.GetComponent<GUIChargenStack>();
							string text5 = DataHandler.GetString("GUI_CAREER_SIDEBAR_NOTABLE_CONTACTS_NEW", false) + DataHandler.GetCondFriendlyName(text3);
							text5 = text5 + ": " + text2;
							if (component2.GetLatestCareer() != null && component2.GetLatestCareer().GetJC() != null)
							{
								text5 = text5 + ", " + component2.GetLatestCareer().GetJC().strNameFriendly;
							}
							text5 = text5 + DataHandler.GetString("GUI_CAREER_SIDEBAR_NOTABLE_CONTACTS_FROM", false) + component2.GetHomeworld().strColonyName + ".";
							gameObject.GetComponentInChildren<TMP_Text>().text = text5;
							RawImage componentInChildren = gameObject.GetComponentInChildren<RawImage>();
							componentInChildren.texture = FaceAnim2.GetPNG(condOwner);
						}
					}
				}
				UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
			}
		}
		if (this.cgs.bCareerEnded)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
			string str = string.Empty;
			Ship ship = null;
			if (CrewSim.system.dictShips.TryGetValue(this.cgs.strRegIDChosen, out ship))
			{
				str = string.Concat(new string[]
				{
					" (\"",
					ship.json.make,
					" ",
					ship.json.model,
					"\") "
				});
			}
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_SHIP_ACQUIRED_1", false) + this.cgs.strRegIDChosen + str + DataHandler.GetString("GUI_CAREER_SIDEBAR_SHIP_ACQUIRED_2", false);
			ship = null;
			gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
			gameObject.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_CAREER_SIDEBAR_PROCEED_LAUNCH", false);
		}
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srSide));
	}

	private void SelectShip(JsonShip js)
	{
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		string text = this.coUser.strID;
		CondOwner condOwner = null;
		if (!latestCareer.bShipOwned)
		{
			if (latestCareer.aSocials != null)
			{
				for (int i = 0; i < latestCareer.aSocials.Count - 2; i += 3)
				{
					string text2 = latestCareer.aSocials[i];
					if (text2 != null && DataHandler.mapCOs.TryGetValue(text2, out condOwner))
					{
						text = text2;
						break;
					}
				}
			}
			if (text == this.coUser.strID)
			{
				text = "UNREGISTERED";
			}
		}
		Ship ship = CrewSim.system.SpawnShip(js.strName, this.cgs.strRegIDChosen, Ship.Loaded.Full, Ship.Damage.New, text, 100, false);
		this.cgs.strRegIDChosen = ship.strRegID;
		if (this.coUser.Company != null)
		{
			this.coUser.Company.strRegID = ship.strRegID;
		}
		this.coUser.ClaimShip(ship.strRegID);
		if (condOwner != null)
		{
			condOwner.ClaimShip(ship.strRegID);
		}
		if (latestCareer.fShipDmgMax > 0f)
		{
			ship.DamageAllCOs(latestCareer.fShipDmgMax, true, null);
		}
		CrewSim.DockShip(CrewSim.shipCurrentLoaded, ship.strRegID);
		MonoSingleton<ObjectiveTracker>.Instance.RemoveShipSubscription(CrewSim.shipCurrentLoaded.strRegID);
		MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(ship.strRegID);
		this.cgs.bCareerEnded = true;
		this.PageResume();
		this.UpdateSidebar();
		if (this.cgs.GetLatestCareer().fShipMortgage > 0f)
		{
			LedgerLI li = new LedgerLI("Ogiso's Bank", this.coUser.strID, this.cgs.GetLatestCareer().fShipMortgage, DataHandler.GetString("GUI_FINANCE_MORTGAGE01", false) + this.cgs.strRegIDChosen, "$", StarSystem.fEpoch, false, LedgerLI.Frequency.Mortgage);
			Ledger.AddLI(li);
		}
	}

	private void ClearMain()
	{
		IEnumerator enumerator = this.tfMain.GetEnumerator();
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

	private void AddCareer(JsonCareer jc, GUIChargenCareer.EventType evt)
	{
		this.HideSidebarAlt();
		this.bmpDot2.color = Color.white;
		this.cgs.AddCareer(jc);
		List<string> list = new List<string>(this.coUser.mapConds.Keys);
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.cgs.ApplyCareer(this.coUser, latestCareer, true);
		foreach (Condition condition in this.coUser.mapConds.Values)
		{
			if (list.IndexOf(condition.strName) < 0 && latestCareer.aSkillsChosen.IndexOf(condition.strName) < 0 && condition.nDisplaySelf == 2 && condition.strName.IndexOf("Dc") != 0)
			{
				latestCareer.aSkillsChosen.Add(condition.strName);
			}
		}
		this.UpdateSidebar();
		this.GetRandomEvent(evt);
	}

	private void AddCareerAndHomeworld(JsonCareer jc, JsonHomeworld jhw, int nStrata)
	{
		this.bmpDot2.color = Color.white;
		List<string> list = new List<string>(this.coUser.mapConds.Keys);
		this.cgs.ChangeHomeworld(jhw, nStrata);
		foreach (Condition condition in this.coUser.mapConds.Values)
		{
			if (list.IndexOf(condition.strName) < 0 && this.cgs.aHomeworldTraits.IndexOf(condition.strName) < 0 && condition.nDisplaySelf == 2 && condition.strName.IndexOf("Dc") != 0)
			{
				this.cgs.aHomeworldTraits.Add(condition.strName);
			}
		}
		list = new List<string>(this.coUser.mapConds.Keys);
		this.cgs.AddCareer(jc);
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.cgs.ApplyCareer(this.coUser, latestCareer, true);
		foreach (Condition condition2 in this.coUser.mapConds.Values)
		{
			if (list.IndexOf(condition2.strName) < 0 && latestCareer.aSkillsChosen.IndexOf(condition2.strName) < 0 && condition2.nDisplaySelf == 2 && condition2.strName.IndexOf("Dc") != 0)
			{
				latestCareer.aSkillsChosen.Add(condition2.strName);
			}
		}
		this.UpdateSidebar();
		this.ClearMain();
		JsonLifeEvent lifeEvent = DataHandler.GetLifeEvent("CGEncHomeworldOKLGIntro");
		this.UpdateTitleStats();
		this.PageEvent(lifeEvent);
	}

	private int GetTraitYears(string str)
	{
		int[] array = new int[]
		{
			1,
			1
		};
		if (str != null && DataHandler.dictTraitScores.TryGetValue(str, out array))
		{
			return array[0];
		}
		return 1;
	}

	private void RemoveCareer()
	{
		this.cgs.RemoveCareer();
	}

	private void PageWorkOnSelfSkills()
	{
		this.ClearMain();
		this.HideSidebarAlt();
		this.bmpResume.State = 0;
		this.bmpDot2.color = Color.white;
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		if (this.cgs.bCareerEnded)
		{
			this.PageResume();
			return;
		}
		this.UpdateTitleStats();
		this.aCondsOld = new List<Condition>(this.coUser.mapConds.Values);
		List<Tuple<Condition, int>> list = new List<Tuple<Condition, int>>();
		List<string> lootNames = DataHandler.GetLoot("CONDChargenSkills").GetLootNames(null, false, null);
		using (List<string>.Enumerator enumerator = lootNames.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				string strSkill = enumerator.Current;
				if (!list.Any((Tuple<Condition, int> x) => x.Item1.strName == strSkill) && strSkill.IndexOf("Skill") >= 0)
				{
					list.Add(new Tuple<Condition, int>(DataHandler.GetCond(strSkill), this.GetTraitYears(strSkill)));
				}
			}
		}
		list = this.OrderListByPairs(list);
		List<Tuple<Condition, int>> list2 = new List<Tuple<Condition, int>>();
		lootNames = DataHandler.GetLoot("CONDChargenHobbies").GetLootNames(null, false, null);
		using (List<string>.Enumerator enumerator2 = lootNames.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				string strSkill = enumerator2.Current;
				if (!list.Any((Tuple<Condition, int> x) => x.Item1.strName == strSkill) && !list2.Any((Tuple<Condition, int> x) => x.Item1.strName == strSkill) && strSkill.IndexOf("Skill") >= 0)
				{
					list2.Add(new Tuple<Condition, int>(DataHandler.GetCond(strSkill), this.GetTraitYears(strSkill)));
				}
			}
		}
		list2 = this.OrderListByPairs(list2);
		List<Tuple<Condition, int>> list3 = new List<Tuple<Condition, int>>();
		foreach (KeyValuePair<string, int[]> keyValuePair in DataHandler.dictTraitScores)
		{
			if (keyValuePair.Value[1] != 0)
			{
				list3.Add(new Tuple<Condition, int>(DataHandler.GetCond(keyValuePair.Key), keyValuePair.Value[0]));
			}
		}
		list3 = this.OrderListByPairs(list3);
		List<List<Tuple<Condition, int>>> skillsAvailable = new List<List<Tuple<Condition, int>>>
		{
			list,
			list2,
			list3
		};
		this.MakeSkillBtnGrid(skillsAvailable);
	}

	private List<Tuple<Condition, int>> OrderListByPairs(List<Tuple<Condition, int>> unOrderedList)
	{
		List<Tuple<Condition, int>> list = new List<Tuple<Condition, int>>();
		unOrderedList = (from x in unOrderedList
		orderby x.Item1.strNameFriendly
		select x).ToList<Tuple<Condition, int>>();
		foreach (Tuple<Condition, int> tuple in unOrderedList)
		{
			if (!list.Contains(tuple))
			{
				Condition cond = tuple.Item1;
				if (string.IsNullOrEmpty(cond.strAnti))
				{
					list.Add(tuple);
				}
				else
				{
					if (list.Count % 2 != 0)
					{
						list.Add(new Tuple<Condition, int>());
					}
					Tuple<Condition, int> tuple2 = unOrderedList.FirstOrDefault((Tuple<Condition, int> x) => x.Item1.strName == cond.strAnti);
					if (tuple2 != null)
					{
						if (tuple.Item2 >= 0 || tuple2.Item2 <= 0)
						{
							list.Add(tuple);
							list.Add(tuple2);
						}
					}
					else
					{
						list.Add(tuple);
					}
				}
			}
		}
		return list;
	}

	private void PageCareerTermSummary()
	{
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.bmpResume.State = 0;
		this.ClearMain();
		this.RemoveCareer();
		if (latestCareer == null || !latestCareer.bConfirmed)
		{
			this.PageListCareers();
			return;
		}
		this.lblPageName.text = "Career Details";
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.lblCenterBold, this.tfMain);
		gameObject.GetComponent<Text>().text = latestCareer.GetJC().strNameFriendly;
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		gameObject.GetComponent<Text>().text = "Credentials";
		original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		List<string> lootNames = DataHandler.GetLoot(latestCareer.GetJC().strLootConds).GetLootNames(null, false, null);
		original = (Resources.Load("GUIShip/GUIChargenCareer/lblCenter") as GameObject);
		foreach (string strName in lootNames)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject.GetComponent<Text>().text = DataHandler.GetCond(strName).strNameFriendly;
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		gameObject.GetComponent<Text>().text = "Special Events";
		foreach (string text in latestCareer.aEvents)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			gameObject.GetComponent<Text>().text = text;
		}
		UnityEngine.Object.Instantiate<GameObject>(this.bmpLine01, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/pnlGridList") as GameObject);
		gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/btnBlue") as GameObject);
		if (this.cgs.bCareerEnded)
		{
			this.bmpResume.State = 3;
			this.bmpWarn.State = 0;
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = "Return to Career";
			Button component = gameObject.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			component.onClick.AddListener(delegate()
			{
				this.PageListCareers();
			});
		}
		else
		{
			JsonCareer career = DataHandler.GetCareer(latestCareer.strJC);
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
			gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = "Continue Career";
			Button component2 = gameObject.transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
			component2.onClick.AddListener(delegate()
			{
				this.PageBranchChoice();
			});
			if (latestCareer.aShips.Count > 0)
			{
				foreach (string strName2 in latestCareer.aShips)
				{
					JsonShip js = DataHandler.GetShip(strName2);
					if (js != null)
					{
						gameObject = UnityEngine.Object.Instantiate<GameObject>(original, gameObject2.transform);
						gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = "Take Ship";
						Button component3 = gameObject.transform.GetComponent<Button>();
						AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
						component3.onClick.AddListener(delegate()
						{
							this.SelectShip(js);
						});
						break;
					}
				}
			}
		}
		this.UpdateTitleStats();
		base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srMain));
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}

	public static int nTermYears = 4;

	public static bool bRedrawSidebar;

	private Button btnReview;

	private GUILamp bmpResume;

	private GUILamp bmpWarn;

	private Image bmpDot1;

	private Image bmpDot2;

	private Image bmpDot3;

	private Image bmpDot4;

	private TMP_Text lblTitle;

	private TMP_Text lblPageName;

	private TMP_Text txtSidebarAlt;

	private Transform tfSidebar;

	private Transform tfMain;

	private CanvasGroup cgSidebar;

	private CanvasGroup cgSidebarAlt;

	private ScrollRect srMain;

	private ScrollRect srSide;

	private CondOwner coUser;

	private GUIChargenStack cgs;

	private AnimationCurveAsset curveShipChances;

	private string strInvalidMessage = "lblInvalidMessage";

	private float fBlinkTime = 1.5f;

	private List<string> lifeEventsOccurredSoFar;

	private List<Condition> aCondsOld;

	private readonly List<SkillSelectionDTO> _selectedSkills = new List<SkillSelectionDTO>();

	private readonly Dictionary<string, Dictionary<string, double>> _promisedAgeLoot = new Dictionary<string, Dictionary<string, double>>();

	private readonly Dictionary<string, GUISkillToggle> _dictCheckmarks = new Dictionary<string, GUISkillToggle>();

	private GameObject lblCenterBold;

	private GameObject bmpLine01;

	private GameObject lblLeftTMP;

	private GameObject pnlColumnLists;

	public enum Page
	{
		List,
		Show,
		Resume
	}

	public enum EventType
	{
		Default,
		Ship,
		Money
	}
}
