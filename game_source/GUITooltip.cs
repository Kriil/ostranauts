using System;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUITooltip : MonoBehaviour
{
	private void Awake()
	{
		this._defaultFrameClr = this.bgFrame.color;
	}

	private void Start()
	{
		CrewSim.RefreshTooltipEvent.AddListener(new UnityAction(this.CloseTooltip));
	}

	private void OnDestroy()
	{
		CrewSim.RefreshTooltipEvent.RemoveListener(new UnityAction(this.CloseTooltip));
	}

	public void SetWindow(GUITooltip.TooltipWindow window)
	{
		this.window = window;
	}

	private void CloseTooltip()
	{
		this.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
	}

	public void SetCO(CondOwner co)
	{
		this.tooltipCO = co;
	}

	public void SetTooltip(CondOwner co, GUITooltip.TooltipWindow window)
	{
		if (co == null || co.strID == null)
		{
			this.tooltipCO = null;
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (window == GUITooltip.TooltipWindow.Hide)
		{
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (co == this.tooltipCO && this.window == window)
		{
			return;
		}
		this.tooltipCO = co;
		this.window = window;
		if (window == GUITooltip.TooltipWindow.Install || window == GUITooltip.TooltipWindow.Repair || window == GUITooltip.TooltipWindow.Task || window == GUITooltip.TooltipWindow.Uninstall)
		{
			if (!CrewSim.objInstance.workManager.COIDHasTasks(co.strID))
			{
				this.tooltipCO = null;
				this.window = GUITooltip.TooltipWindow.Hide;
				return;
			}
			foreach (Task2 task in CrewSim.objInstance.workManager.GetAllTasksForCOID(co.strID))
			{
				TextMeshProUGUI textMeshProUGUI = this.tooltipText;
				textMeshProUGUI.text += this.TooltipTextFormat3(co, task);
			}
			this.TooltipResize2();
		}
	}

	public void SetTooltipIA(Interaction ia, GUITooltip.TooltipWindow window)
	{
		if (ia == null || ia.strName == string.Empty)
		{
			this.tooltipCO = null;
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (window == GUITooltip.TooltipWindow.Hide)
		{
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (ia == this.tooltipIA && this.window == window)
		{
			return;
		}
		this.tooltipCO = null;
		this.tooltipIA = ia;
		this.window = window;
		if (window == GUITooltip.TooltipWindow.QAB || window == GUITooltip.TooltipWindow.MTT)
		{
			this.tooltipText.text = GUITooltip.TooltipTextFormat4(ia);
			this.TooltipResize2();
		}
	}

	public void SetTooltipMulti(List<CondOwner> aCOs, GUITooltip.TooltipWindow window)
	{
		if (aCOs == null || aCOs.Count == 0 || aCOs[0] == null || aCOs[0].strID == null)
		{
			this.tooltipCO = null;
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (window == GUITooltip.TooltipWindow.Hide)
		{
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (aCOs[0] == this.tooltipCO && this.window == window)
		{
			return;
		}
		this.tooltipCO = aCOs[0];
		this.window = window;
		this.tooltipText.text = string.Empty;
		if (window == GUITooltip.TooltipWindow.Install || window == GUITooltip.TooltipWindow.Repair || window == GUITooltip.TooltipWindow.Task || window == GUITooltip.TooltipWindow.Uninstall)
		{
			bool flag = false;
			foreach (CondOwner condOwner in aCOs)
			{
				if (CrewSim.objInstance.workManager.COIDHasTasks(condOwner.strID))
				{
					foreach (Task2 task in CrewSim.objInstance.workManager.GetAllTasksForCOID(condOwner.strID))
					{
						TextMeshProUGUI textMeshProUGUI = this.tooltipText;
						textMeshProUGUI.text = textMeshProUGUI.text + this.TooltipTextFormat3(condOwner, task) + "\n--------\n";
						flag = true;
					}
				}
			}
			if (flag)
			{
				this.TooltipResize2();
			}
			else
			{
				this.tooltipCO = null;
				this.window = GUITooltip.TooltipWindow.Hide;
			}
		}
	}

	public void SetTooltipCrew(CondOwner coCrew, GUITooltip.TooltipWindow wndw)
	{
		if (coCrew == null)
		{
			this.tooltipCO = null;
			this.window = GUITooltip.TooltipWindow.Hide;
			return;
		}
		if (wndw != GUITooltip.TooltipWindow.Crew)
		{
			this.window = wndw;
			return;
		}
		if (coCrew == this.tooltipCO && this.window == this.window)
		{
			return;
		}
		this.tooltipCO = coCrew;
		this.window = wndw;
		if (coCrew.aQueue == null)
		{
			return;
		}
		string str = string.Empty;
		bool flag = false;
		if (coCrew == CrewSim.coPlayer)
		{
			str = ", <b>Captain</b>";
			flag = true;
		}
		else if (coCrew.HasCond("IsPlayerCrew"))
		{
			str = ", <b>Crew</b>";
			flag = true;
		}
		if (!flag)
		{
			float factionScore = coCrew.GetFactionScore(CrewSim.coPlayer.GetAllFactions());
			if (JsonFaction.GetReputation(factionScore) == JsonFaction.Reputation.Dislikes)
			{
				this.bgFrame.color = this._dislikedFrameClr;
			}
		}
		else
		{
			this.bgFrame.color = this._crewFrameClr;
		}
		string text = coCrew.strName + str;
		if (coCrew.jsShiftLast != null && coCrew.jsShiftLast.nID >= 0)
		{
			text = text + ", <b>Active Shift:</b> " + coCrew.jsShiftLast.strName;
		}
		text += "\n\n<b>Current:</b>";
		if (coCrew.aQueue.Count > 0 && coCrew.aQueue[0] != null)
		{
			text = text + "\n" + coCrew.aQueue[0].strTitle;
		}
		else
		{
			text += "\nnone";
		}
		text += "\n\n";
		text += "<b>Log:</b>";
		int num = 3;
		if (coCrew.aMessages != null && coCrew.aMessages.Count > 0)
		{
			int num2 = coCrew.aMessages.Count - 1;
			while (num2 >= 0 && num > 0)
			{
				if (!(coCrew.aMessages[num2].strOwner != coCrew.strName))
				{
					num--;
					text = text + "\n" + coCrew.aMessages[num2].strMessage;
				}
				num2--;
			}
		}
		if (num == 3)
		{
			text += "\nnone";
		}
		if (coCrew.aQueue.Count > 1)
		{
			text += "\n\n<b>Planned:</b>";
			for (int i = 1; i < coCrew.aQueue.Count; i++)
			{
				Interaction interaction = coCrew.aQueue[i];
				if (interaction != null)
				{
					string str2 = "\n" + interaction.strTitle;
					text += str2;
					break;
				}
			}
		}
		if (flag && coCrew.RecentWorkHistory != null)
		{
			List<Tuple<double, string>> lastFailedWorkAttempts = coCrew.RecentWorkHistory.GetLastFailedWorkAttempts();
			if (lastFailedWorkAttempts != null && lastFailedWorkAttempts.Count > 0)
			{
				text += "\n\n<b>Last Failed Work Attempts:</b>";
				foreach (Tuple<double, string> tuple in lastFailedWorkAttempts)
				{
					string text2 = text;
					text = string.Concat(new string[]
					{
						text2,
						"\n",
						tuple.Item2,
						", ",
						(StarSystem.fEpoch - tuple.Item1).ToString("N0"),
						"s ago"
					});
				}
			}
			if (coCrew.RecentWorkHistory.LastPledge != null && StarSystem.fEpoch - coCrew.RecentWorkHistory.LastPledge.Item1 < 30.0)
			{
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					"\n\n<b>Last Active Pledge:</b>\n",
					coCrew.RecentWorkHistory.LastPledge.Item2,
					", ",
					(StarSystem.fEpoch - coCrew.RecentWorkHistory.LastPledge.Item1).ToString("N0"),
					"s ago"
				});
			}
		}
		text = GUITooltip.RemoveTrailingNewlines(text);
		this.tooltipText.text = text;
		this.TooltipResize2();
	}

	public static List<string> GetItemQualityList(List<CondTrigger> aCTs)
	{
		List<string> list = new List<string>();
		if (aCTs != null)
		{
			Dictionary<CondTrigger, float> dictionary = new Dictionary<CondTrigger, float>();
			foreach (CondTrigger condTrigger in aCTs)
			{
				if (dictionary.ContainsKey(condTrigger))
				{
					Dictionary<CondTrigger, float> dictionary2;
					CondTrigger key;
					(dictionary2 = dictionary)[key = condTrigger] = dictionary2[key] + condTrigger.fCount;
				}
				else
				{
					dictionary[condTrigger] = condTrigger.fCount;
				}
			}
			foreach (CondTrigger condTrigger2 in dictionary.Keys)
			{
				string text = "{";
				text += condTrigger2.RulesInfo;
				text += "}";
				if (dictionary[condTrigger2] != 1f)
				{
					text = text + " x" + dictionary[condTrigger2];
				}
				list.Add(text);
			}
		}
		return list;
	}

	public static List<string> GetItemQualityList(string strCTLoot)
	{
		if (strCTLoot != null)
		{
			List<CondTrigger> ctloot = DataHandler.GetLoot(strCTLoot).GetCTLoot(null, null);
			return GUITooltip.GetItemQualityList(ctloot);
		}
		return new List<string>();
	}

	private void Update()
	{
		if (this.window == GUITooltip.TooltipWindow.Hide)
		{
			this.tooltipCG.alpha = 0f;
			this.bgFrame.color = this._defaultFrameClr;
			return;
		}
		this.tooltipCG.alpha = 1f;
		if (this.window == GUITooltip.TooltipWindow.Inventory || this.window == GUITooltip.TooltipWindow.Trade)
		{
			if (this.tooltipCO != null)
			{
				if (this.window == GUITooltip.TooltipWindow.Inventory)
				{
					this.fTooltipHideTimer = 0.05f;
				}
				else if (this.window == GUITooltip.TooltipWindow.Trade)
				{
					this.fTooltipHideTimer = 0.25f;
				}
				this.tooltipText.text = this.TooltipTextFormat1(this.tooltipCO);
				this.TooltipResize();
			}
			else if (this.fTooltipHideTimer > 0f)
			{
				this.fTooltipHideTimer -= Time.deltaTime;
			}
			else
			{
				this.tooltipText.text = string.Empty;
			}
		}
		else if (this.window == GUITooltip.TooltipWindow.QAB || this.window == GUITooltip.TooltipWindow.MTT)
		{
			if (this.tooltipIA != null)
			{
				if (this.window == GUITooltip.TooltipWindow.QAB)
				{
					this.fTooltipHideTimer = 0.05f;
				}
				if (this.window == GUITooltip.TooltipWindow.MTT)
				{
					this.fTooltipHideTimer = 0.25f;
				}
				this.tooltipText.text = GUITooltip.TooltipTextFormat4(this.tooltipIA);
				this.TooltipResize();
			}
			else if (this.fTooltipHideTimer > 0f)
			{
				this.fTooltipHideTimer -= Time.deltaTime;
			}
			else
			{
				this.tooltipText.text = string.Empty;
			}
		}
		float num = 1280f / (float)Screen.width * CrewSim.objInstance.AspectRatioMod();
		float num2 = 720f / (float)Screen.height;
		float num3 = (Input.mousePosition.x - (float)(Screen.width / 2)) * num;
		float num4 = (Input.mousePosition.y - (float)(Screen.height / 2)) * num2;
		float z = 25600f / (float)Screen.width;
		float num5 = this.tooltipRect.rect.width / 2f + 10f;
		float num6 = this.tooltipRect.rect.height / 2f + 10f;
		if ((double)(num3 + num5 * 2f) > (double)((float)Screen.width * num) * 0.45)
		{
			num5 = -num5;
		}
		if ((double)(num4 + num6 * 2f) > (double)((float)Screen.height * num2) * 0.5)
		{
			num4 = (float)Screen.height * num2 * 0.5f - num6 * 2f;
		}
		this.tooltipRect.localPosition = new Vector3(num3 + num5, num4 + num6, z);
	}

	public void TooltipResize2()
	{
		float x = this.tooltipText.preferredWidth + 1f;
		if (this.tooltipText.preferredWidth + 1f >= 420f)
		{
			x = 420f;
		}
		this.tooltipRect.sizeDelta = new Vector2(x, this.tooltipText.preferredHeight + 5f);
	}

	public void TooltipResize()
	{
		float x = 420f;
		if (this.tooltipText.preferredHeight + 10f >= this.tooltipRect.rect.height)
		{
			this.tooltipRect.sizeDelta = new Vector2(x, this.tooltipText.preferredHeight + 10f);
		}
		else
		{
			this.tooltipRect.sizeDelta = new Vector2(x, 150f);
		}
	}

	public string TooltipTextFormat1(CondOwner condOwner)
	{
		string text = string.Empty;
		string str = this.tooltipCO.strName;
		if (this.tooltipCO.FriendlyName != null)
		{
			str = this.tooltipCO.FriendlyName;
		}
		text = str + "\n";
		if (condOwner.strDesc != null)
		{
			text = text + "\n" + this.tooltipCO.strDesc + "\n";
		}
		double totalMass = condOwner.GetTotalMass();
		double condAmount = condOwner.GetCondAmount("StatMass");
		text = text + "\nMass: " + condAmount.ToString("F2") + "kg";
		if (this.window == GUITooltip.TooltipWindow.Inventory && condAmount != totalMass)
		{
			text = text + "\nMass of stack: " + totalMass.ToString("F2") + "kg";
		}
		double condAmount2 = condOwner.GetCondAmount("StatDamageMax");
		if (condAmount2 > 0.0)
		{
			double condAmount3 = condOwner.GetCondAmount("StatDamage");
			text = text + "\nCondition: " + ((condAmount2 - condAmount3) / condAmount2 * 100.0).ToString("F2") + "%";
		}
		Powered component = condOwner.GetComponent<Powered>();
		if (condOwner.HasCond("IsPowerObservable"))
		{
			double num = 0.0;
			double num2 = 0.0;
			double num3 = 0.0;
			num2 += condOwner.GetCondAmount("StatPowerMax") * condOwner.GetDamageState();
			num += condOwner.GetCondAmount("StatPower");
			if (condOwner.GetComponent<Container>() != null)
			{
				List<CondOwner> cos = condOwner.GetComponent<Container>().GetCOs(true, null);
				if (cos != null && cos.Count > 0)
				{
					foreach (CondOwner condOwner2 in cos)
					{
						if (condOwner2 != null)
						{
							num2 += condOwner2.GetCondAmount("StatPowerMax") * condOwner2.GetDamageState();
							num += condOwner2.GetCondAmount("StatPower");
						}
					}
				}
			}
			if (num2 != 0.0)
			{
				num3 = num / num2;
			}
			if (num3 > 1.0)
			{
				num3 = 1.0;
			}
			string text2 = (num3 * 100.0).ToString("F2");
			text = text + "\nCharge: " + text2 + "%";
			condOwner.mapInfo["Charge"] = text2 + "%";
		}
		else if (component != null)
		{
			string text3 = (component.PowerStoredPercent * 100.0).ToString("F2");
			text = text + "\nCharge: " + text3 + "%";
			condOwner.mapInfo["Charge"] = text3 + "%";
		}
		else
		{
			condOwner.mapInfo.Remove("Charge");
		}
		if (DataHandler.GetCondTrigger("TIsVessel").Triggered(condOwner, null, true))
		{
			string str2;
			if (condOwner.mapInfo.TryGetValue("Pressure", out str2))
			{
				text = text + "\nPressure: " + str2;
			}
			Condition condition;
			if (condOwner.mapConds.TryGetValue("StatLiqD2O", out condition) && condition != null)
			{
				string text4 = text;
				text = string.Concat(new string[]
				{
					text4,
					"\n",
					condition.strNameFriendly,
					": ",
					condition.fCount.ToString("N3"),
					"kg"
				});
			}
			if (condOwner.mapConds.TryGetValue("StatSolidHe3", out condition) && condition != null)
			{
				string text4 = text;
				text = string.Concat(new string[]
				{
					text4,
					"\n",
					condition.strNameFriendly,
					": ",
					condition.fCount.ToString("N3"),
					"kg"
				});
			}
		}
		text = GUITooltip.RemoveTrailingNewlines(text);
		return text;
	}

	public string TooltipTextFormat2(CondOwner condOwner)
	{
		string text = string.Empty;
		string cofriendlyName = DataHandler.GetCOFriendlyName(condOwner.strPlaceholderInstallReq);
		text = "Install job requires " + cofriendlyName + "\n\nParts in lot: ";
		if (condOwner.aLot.Count > 0)
		{
			cofriendlyName = DataHandler.GetCOFriendlyName(condOwner.aLot[0].strName);
			text += cofriendlyName;
		}
		else
		{
			text += "None";
		}
		if (condOwner.HasCond("StatInstallProgress") && condOwner.HasCond("StatInstallProgressMax"))
		{
			double num = condOwner.GetCondAmount("StatInstallProgress") / condOwner.GetCondAmount("StatInstallProgressMax") * 100.0;
			string text2 = text;
			text = string.Concat(new object[]
			{
				text2,
				"\n\nInstall Progress: ",
				num,
				"%"
			});
		}
		if (condOwner.HasCond("StatInstallProgress") && condOwner.HasCond("StatInstallProgressMax"))
		{
			double num2 = condOwner.GetCondAmount("StatInstallProgress") / condOwner.GetCondAmount("StatInstallProgressMax") * 100.0;
			string text2 = text;
			text = string.Concat(new object[]
			{
				text2,
				"\n\nUninstall Progress: ",
				num2,
				"%"
			});
		}
		return GUITooltip.RemoveTrailingNewlines(text);
	}

	private string TooltipTextFormat3(CondOwner co, Task2 task)
	{
		if (co == null || task == null)
		{
			return string.Empty;
		}
		string text = string.Empty;
		string str = (co.strPlaceholderInstallFinish == null) ? co.FriendlyName : DataHandler.GetCOFriendlyName(co.strPlaceholderInstallFinish);
		string text2 = task.strInteraction;
		Interaction interaction = DataHandler.GetInteraction(task.strInteraction, null, true);
		if (interaction != null)
		{
			text2 = interaction.strTitle;
		}
		text2 = text2 + " " + str + "\n";
		text = text2;
		if (this.bStatusFormat)
		{
			text = text + "\n" + task.strStatus;
		}
		else
		{
			List<string> list = new List<string>();
			if (interaction != null)
			{
				list = GUITooltip.GetItemQualityList(interaction.strLootCTsUse);
				if (list.Count > 0)
				{
					text += "\nTools required:\n";
					foreach (string str2 in list)
					{
						text = text + str2 + "\n";
					}
				}
				list = GUITooltip.GetItemQualityList(interaction.strLootItmInputs);
				if (list.Count > 0)
				{
					text += "\nInput items required:\n";
					foreach (string str3 in list)
					{
						text = text + str3 + "\n";
					}
				}
				list = GUITooltip.GetItemQualityList(interaction.strLootCTsGive);
				if (list.Count > 0)
				{
					foreach (string str4 in list)
					{
						text = text + str4 + "\n";
					}
				}
				list = GUITooltip.GetItemQualityList(interaction.strLootCTsRemoveUs);
				if (list.Count > 0)
				{
					foreach (string str5 in list)
					{
						text = text + str5 + "\n";
					}
				}
			}
			if (co.aLot.Count > 0)
			{
				string text3 = string.Empty;
				text3 += "\nParts in lot:";
				foreach (CondOwner condOwner in co.aLot)
				{
					string cofriendlyName = DataHandler.GetCOFriendlyName(condOwner.strName);
					text3 = text3 + " " + cofriendlyName + ",";
				}
				text3 = text3.Remove(text3.Length - 1);
				text = text + "\n" + text3;
			}
		}
		if (interaction != null)
		{
			DataHandler.ReleaseTrackedInteraction(interaction);
		}
		text = GUITooltip.RemoveTrailingNewlines(text);
		return text;
	}

	private static string TooltipTextFormat4(Interaction ia)
	{
		if (ia == null)
		{
			return string.Empty;
		}
		string text = string.Empty;
		string text2 = string.Empty;
		text = text + "<b>" + ia.strTitle + "</b>\n";
		if (ia.strTooltip != null && ia.strTooltip != string.Empty)
		{
			text = text + GrammarUtils.GetInflectedString(ia.strTooltip, ia) + "\n";
		}
		else
		{
			text = text + GrammarUtils.GenerateDescription(ia) + "\n";
		}
		if (ia.nMoveType == Interaction.MoveType.GAMBIT)
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			GUISocialStatus.GetPassFailConds(list2, list, ia.objUs, ia.objThem, ia);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_GAMBIT", false));
			if (list.Count > 0 || list2.Count > 0)
			{
				Color color = DataHandler.GetColor("SocialStatusRed");
				Color color2 = DataHandler.GetColor("SocialStatusGreen");
				string value = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
				string value2 = "<color=#" + ColorUtility.ToHtmlStringRGB(color2) + ">";
				string value3 = "</color>";
				bool flag = false;
				if (list2.Count > 0)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_STAKES_PASS", false));
					foreach (string strCondName in list2)
					{
						if (flag)
						{
							stringBuilder.Append(DataHandler.GetString("SOCIAL_STAKES_COMMA", false));
						}
						flag = true;
						stringBuilder.Append(value2);
						stringBuilder.Append(DataHandler.GetCondFriendlyName(strCondName));
						stringBuilder.Append(value3);
					}
				}
				if (list.Count > 0)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_STAKES_FAIL", false));
					flag = false;
					foreach (string strCondName2 in list)
					{
						if (flag)
						{
							stringBuilder.Append(DataHandler.GetString("SOCIAL_STAKES_COMMA", false));
						}
						flag = true;
						stringBuilder.Append(value);
						stringBuilder.Append(DataHandler.GetCondFriendlyName(strCondName2));
						stringBuilder.Append(value3);
					}
				}
				text += stringBuilder.ToString();
			}
		}
		else if (ia.nMoveType == Interaction.MoveType.SOCIAL_CORE)
		{
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendLine();
			stringBuilder2.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_CORE", false));
			text += stringBuilder2.ToString();
		}
		else if (ia.nMoveType == Interaction.MoveType.COMMAND)
		{
			StringBuilder stringBuilder3 = new StringBuilder();
			stringBuilder3.AppendLine();
			stringBuilder3.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_COMMAND", false));
			text += stringBuilder3.ToString();
		}
		else if (ia.nMoveType == Interaction.MoveType.STAKES)
		{
			StringBuilder stringBuilder4 = new StringBuilder();
			stringBuilder4.AppendLine();
			stringBuilder4.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_STAKES", false));
			text += stringBuilder4.ToString();
		}
		else if (ia.nMoveType == Interaction.MoveType.GIG)
		{
			StringBuilder stringBuilder5 = new StringBuilder();
			stringBuilder5.AppendLine();
			stringBuilder5.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_GIG", false));
			text += stringBuilder5.ToString();
		}
		else if (ia.strActionGroup == "Talk")
		{
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine();
			stringBuilder6.AppendLine(DataHandler.GetString("TOOLTIP_SOCIAL_SOCIAL", false));
			text += stringBuilder6.ToString();
		}
		if (ia.CTTestUs != null)
		{
			if (ia.CTTestUs.aReqs != null && ia.CTTestUs.aReqs.Length > 0)
			{
				text2 = string.Empty;
				bool flag2 = false;
				foreach (string strName in ia.CTTestUs.aReqs)
				{
					Condition cond = DataHandler.GetCond(strName);
					if (cond != null)
					{
						if (cond.nDisplaySelf != 0)
						{
							if (flag2)
							{
								text2 += ", ";
							}
							text2 += cond.strNameFriendly;
							flag2 = true;
						}
					}
				}
				if (text2.Length > 0)
				{
					text += "\n<b>We need:</b> \n";
					text += text2;
					text += "\n";
				}
			}
			if (ia.CTTestUs.aForbids != null && ia.CTTestUs.aForbids.Length > 0)
			{
				text2 = string.Empty;
				bool flag2 = false;
				foreach (string strName2 in ia.CTTestUs.aForbids)
				{
					Condition cond2 = DataHandler.GetCond(strName2);
					if (cond2 != null)
					{
						if (cond2.nDisplaySelf != 0)
						{
							if (flag2)
							{
								text2 += ", ";
							}
							text2 += cond2.strNameFriendly;
							flag2 = true;
						}
					}
				}
				if (text2.Length > 0)
				{
					text += "\n<b>We can't be:</b> \n";
					text += text2;
					text += "\n";
				}
			}
		}
		if (ia.LootCTsUs != null || ia.LootCTsThem != null)
		{
			string socialPreview = GUISocialCombat2.GetSocialPreview(ia);
			if (!string.IsNullOrEmpty(socialPreview))
			{
				text += "\n<b>Effects:</b> \n";
				text = text + socialPreview + "\n";
			}
		}
		List<string> list3 = new List<string>();
		list3 = GUITooltip.GetItemQualityList(ia.strLootCTsUse);
		if (list3.Count > 0)
		{
			text += "\n<b>Tools required:</b>\n";
			foreach (string str in list3)
			{
				text = text + str + "\n";
			}
		}
		list3 = GUITooltip.GetItemQualityList(ia.strLootItmInputs);
		string text3 = string.Empty;
		if (list3.Count > 0)
		{
			foreach (string str2 in list3)
			{
				text3 = text3 + str2 + "\n";
			}
		}
		list3 = GUITooltip.GetItemQualityList(ia.strLootCTsGive);
		if (list3.Count > 0)
		{
			foreach (string str3 in list3)
			{
				text3 = text3 + str3 + "\n";
			}
		}
		if (text3 != string.Empty)
		{
			text = text + "\n<b>Items given:</b>\n" + text3;
		}
		list3 = GUITooltip.GetItemQualityList(ia.strLootCTsRemoveUs);
		if (list3.Count > 0)
		{
			text += "\n<b>Items consumed:</b>\n";
			foreach (string str4 in list3)
			{
				text = text + str4 + "\n";
			}
		}
		text = GUITooltip.RemoveTrailingNewlines(text);
		return text;
	}

	private static string RemoveTrailingNewlines(string strRetval)
	{
		int num = strRetval.Length - 1;
		while (num >= 0 && strRetval[num] == '\n')
		{
			num--;
		}
		if (num >= 0 && num + 1 < strRetval.Length)
		{
			strRetval = strRetval.Substring(0, num + 1);
		}
		return strRetval;
	}

	[SerializeField]
	private TextMeshProUGUI tooltipText;

	[SerializeField]
	public CanvasGroup tooltipCG;

	[SerializeField]
	private RectTransform tooltipRect;

	[SerializeField]
	private Image bgFrame;

	private Interaction tooltipIA;

	private CondOwner tooltipCO;

	private Color _defaultFrameClr = Color.white;

	private Color _crewFrameClr = new Color(1f, 0.7647059f, 0f);

	private Color _dislikedFrameClr = new Color(0.59607846f, 0.13333334f, 0.13333334f);

	public GUITooltip.TooltipWindow window;

	private float fTooltipHideTimer;

	private bool bStatusFormat = true;

	public enum TooltipWindow
	{
		Inventory,
		Trade,
		Repair,
		Uninstall,
		Install,
		Task,
		QAB,
		MTT,
		Hide,
		Crew
	}
}
