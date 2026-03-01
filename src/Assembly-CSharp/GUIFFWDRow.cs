using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUIFFWDRow : MonoBehaviour
{
	private void Awake()
	{
		for (int i = 0; i < this.aHours.Length; i++)
		{
			this.aHours[i] = base.transform.Find("txtHour" + (i + 1)).GetComponent<TMP_Text>();
		}
		if (GUIFFWDRow.aPayloadIAs == null)
		{
			GUIFFWDRow.aPayloadIAs = DataHandler.GetLoot("ACTFFWDContextPayloads").GetAllLootNames();
		}
		if (GUIFFWDRow.aRiskFactors == null)
		{
			GUIFFWDRow.aRiskFactors = DataHandler.GetLoot("ACTFFWDContextCondsFilter").GetAllLootNames();
		}
		if (GUIFFWDRow.aEventIAs == null)
		{
			GUIFFWDRow.aEventIAs = DataHandler.GetLoot("ACTFFWDContextEvents").GetAllLootNames();
		}
	}

	public void SetCrew(CondOwner co, int nHours)
	{
		if (co == null)
		{
			return;
		}
		this.dictPayloads = new Dictionary<string, int>();
		foreach (string key in GUIFFWDRow.aPayloadIAs)
		{
			this.dictPayloads[key] = 0;
		}
		this.dictPayloads["Tick1HourShiftFree"] = 0;
		this.dictPayloads["Tick1HourShiftSleep"] = 0;
		this.dictPayloads["Tick1HourShiftWork"] = 0;
		this._co = co;
		this.txtCrew.text = co.FriendlyName + "\n<i>Active Effects</i>";
		for (int i = 0; i < this.aHours.Length; i++)
		{
			this.aHours[i].text = "--";
			if (i < nHours)
			{
				JsonShift shift = co.Company.GetShift(StarSystem.nUTCHour + i, co);
				int num = shift.nID;
				if (num < 0)
				{
					num = 0;
				}
				string text = "Tick1HourShiftFree";
				if (num == 1)
				{
					text = "Tick1HourShiftSleep";
				}
				else if (num == 2)
				{
					text = "Tick1HourShiftWork";
				}
				Interaction interaction = DataHandler.GetInteraction(text, null, false);
				if (interaction != null)
				{
					Dictionary<string, int> dictionary;
					string key2;
					(dictionary = this.dictPayloads)[key2 = text] = dictionary[key2] + 1;
					this.aHours[i].text = interaction.strTitle;
					string strShiftDesc = "<b>" + interaction.strTitle + ":</b> " + interaction.strDesc;
					string str = "<i>Idle</i>";
					string strActiveDesc = string.Empty;
					foreach (string text2 in GUIFFWDRow.aPayloadIAs)
					{
						interaction = DataHandler.GetInteraction(text2, null, false);
						if (interaction != null)
						{
							if (interaction.Triggered(co, co, false, false, false, true, null))
							{
								string key3;
								(dictionary = this.dictPayloads)[key3 = text2] = dictionary[key3] + 1;
								str = "<i>" + interaction.strTitle + "</i>";
								strActiveDesc = "<b>" + interaction.strTitle + ":</b> " + interaction.strDesc;
								break;
							}
						}
					}
					TMP_Text tmp_Text = this.aHours[i];
					tmp_Text.text = tmp_Text.text + "\n" + str;
					GUIEnterExitHandler guienterExitHandler = this.aHours[i].gameObject.AddComponent<GUIEnterExitHandler>();
					guienterExitHandler.SetDelegates(delegate
					{
						GUITooltip2.SetToolTip("Shift and Active Effects", strShiftDesc + "\n\n" + strActiveDesc, true);
					}, delegate
					{
						GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
					});
				}
			}
		}
		string text3 = string.Empty;
		this.nAlerts = 0;
		foreach (string text4 in GUIFFWDRow.aRiskFactors)
		{
			if (this._co.HasCond(text4))
			{
				if (this.nAlerts > 0)
				{
					text3 += ", ";
				}
				text3 += this._co.mapConds[text4].strNameFriendly;
				this.nAlerts++;
			}
		}
		if (this.nAlerts == 0)
		{
			text3 += "None";
		}
		this.txtMood.text = text3;
		GUIEnterExitHandler guienterExitHandler2 = this.txtMood.gameObject.AddComponent<GUIEnterExitHandler>();
		guienterExitHandler2.SetDelegates(delegate
		{
			GUITooltip2.SetToolTip("Fast-Forward Risks", "These conditions are life-threatening, and fast-forward now may result in death!", true);
		}, delegate
		{
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		});
	}

	public string ApplyEffects()
	{
		if (this.CO == null)
		{
			return "None";
		}
		if (!this.CO.bAlive)
		{
			return "Dead";
		}
		string text = string.Empty;
		this.nShip = 0;
		bool flag = false;
		float num = (float)this.nAlerts * 0.1f;
		float num2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		if (this.nAlerts > 0 && num2 <= num)
		{
			this.CO.AddCondAmount("Death", 1.0, 0.0, 0f);
			flag = true;
			text = "Died";
		}
		foreach (KeyValuePair<string, int> keyValuePair in this.dictPayloads)
		{
			if (flag)
			{
				break;
			}
			if (keyValuePair.Value != 0)
			{
				Interaction interaction = DataHandler.GetInteraction(keyValuePair.Key, null, false);
				if (interaction != null)
				{
					Interaction interaction2 = interaction;
					CondOwner co = this.CO;
					interaction.objThem = co;
					interaction2.objUs = co;
					for (int i = 0; i < keyValuePair.Value; i++)
					{
						interaction.ApplyChain(null);
					}
					if (interaction.strName == "Tick1HourShiftWork")
					{
						this.nShip += keyValuePair.Value;
					}
					if (text.Length > 0)
					{
						text += ", ";
					}
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						keyValuePair.Value,
						"x ",
						interaction.strTitle
					});
				}
			}
		}
		num = 0.25f;
		num2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		if (!flag && num2 <= num)
		{
			MathUtils.ShuffleList<string>(GUIFFWDRow.aEventIAs);
			foreach (string strName in GUIFFWDRow.aEventIAs)
			{
				Interaction interaction3 = DataHandler.GetInteraction(strName, null, false);
				if (interaction3 != null)
				{
					interaction3.objUs = this.CO;
					if (interaction3.strThemType == "Self")
					{
						interaction3.objThem = this.CO;
					}
					else
					{
						interaction3.objThem = this.GetIAThem(interaction3);
					}
					if (!(interaction3.objThem == null) && interaction3.Triggered(false, false, false))
					{
						List<string> list = new List<string>();
						interaction3.ApplyChain(list);
						foreach (string str in list)
						{
							if (text.Length > 0)
							{
								text += ", ";
							}
							text += str;
						}
						break;
					}
				}
			}
		}
		if (text.Length == 0)
		{
			text = "None";
		}
		return text;
	}

	private CondOwner GetIAThem(Interaction iaTrigger)
	{
		if (iaTrigger == null)
		{
			return null;
		}
		if (iaTrigger.PSpecTestThem == null)
		{
			if (iaTrigger.CTTestThem != null)
			{
				List<CondOwner> list = new List<CondOwner>();
				if (iaTrigger.ShipTestThem == null || iaTrigger.ShipTestThem.Matches(this.CO.ship, this.CO))
				{
					list.AddRange(this.CO.ship.GetCOs(iaTrigger.CTTestThem, true, false, true));
				}
				foreach (Ship ship in this.CO.ship.GetAllDockedShipsFull())
				{
					if (iaTrigger.ShipTestThem == null || iaTrigger.ShipTestThem.Matches(ship, this.CO))
					{
						list.AddRange(ship.GetCOs(iaTrigger.CTTestThem, true, false, true));
					}
				}
				list.RemoveAll((CondOwner person) => person == this.CO);
				if (list.Count > 0)
				{
					return list[MathUtils.Rand(0, list.Count - 1, MathUtils.RandType.Flat, null)];
				}
			}
			return null;
		}
		PersonSpec person2 = StarSystem.GetPerson(iaTrigger.PSpecTestThem, this.CO.socUs, false, null, iaTrigger.ShipTestThem);
		if (person2 == null || person2.GetCO() == null)
		{
			return null;
		}
		return person2.GetCO();
	}

	public int Ship
	{
		get
		{
			return this.nShip;
		}
	}

	public CondOwner CO
	{
		get
		{
			return this._co;
		}
	}

	[SerializeField]
	private TMP_Text txtCrew;

	[SerializeField]
	private TMP_Text txtMood;

	private TMP_Text[] aHours = new TMP_Text[6];

	private Dictionary<string, int> dictPayloads;

	private static List<string> aPayloadIAs;

	private static List<string> aRiskFactors;

	private static List<string> aEventIAs;

	private int nAlerts;

	private int nShip;

	private CondOwner _co;

	private const string HIGHLIGHT_BEG = "<i>";

	private const string HIGHLIGHT_END = "</i>";
}
