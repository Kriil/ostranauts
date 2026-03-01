using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;
using UnityEngine.UI;

public class GUIChargenHomeworld : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.dictRegionToggles = new Dictionary<string, Toggle>();
		this.dictPaymentToggles = new Dictionary<int, Toggle>();
		this.dictPaymentCaps = new Dictionary<int, Image>();
		this.aPaymentText = new List<string>();
		this.aPaymentSubtext = new List<string>();
		this.aPaymentText.Add("ERROR");
		this.aPaymentText.Add("PREPAY");
		this.aPaymentText.Add("CREDIT");
		this.aPaymentSubtext.Add("CODE 48 55 4D 41 4E");
		this.aPaymentSubtext.Add("FUNDS VERIFIED");
		this.aPaymentSubtext.Add("CITIZENSHIP VERIFIED");
		this.dictPaymentToggles[0] = base.transform.Find("chkHack").GetComponent<Toggle>();
		this.dictPaymentToggles[1] = base.transform.Find("chkPrepay").GetComponent<Toggle>();
		this.dictPaymentToggles[2] = base.transform.Find("chkCredit").GetComponent<Toggle>();
		this.dictPaymentToggles[0].onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ChangeStrata(0);
		});
		this.dictPaymentToggles[1].onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ChangeStrata(1);
		});
		this.dictPaymentToggles[2].onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ChangeStrata(2);
		});
		this.dictPaymentCaps[0] = base.transform.Find("bmpCapHack").GetComponent<Image>();
		this.dictPaymentCaps[1] = base.transform.Find("bmpCapPrepay").GetComponent<Image>();
		this.dictPaymentCaps[2] = base.transform.Find("bmpCapCredit").GetComponent<Image>();
		this.lblRegion = base.transform.Find("lblRegion").GetComponent<Text>();
		this.lblPayment = base.transform.Find("lblPayment").GetComponent<Text>();
		this.lblPaymentSub = base.transform.Find("lblPaymentSub").GetComponent<Text>();
		this.lblRegionPamphlet = base.transform.Find("btnPamphlet/lblRegionPamphlet").GetComponent<Text>();
		this.lblRisks = base.transform.Find("btnPamphlet/lblRisks").GetComponent<Text>();
		this.btnPamphlet = base.transform.Find("btnPamphlet").GetComponent<Button>();
		this.btnPamphlet.onClick.AddListener(delegate()
		{
			this.TogglePamphlet();
		});
		this.tfRegions = base.transform.Find("pnlRegions");
		this.grpRegions = base.transform.Find("chkGroupRegions").GetComponent<ToggleGroup>();
		this.rowRegionTemplate = (Resources.Load("GUIShip/GUIChargenHomeworldRow") as GameObject);
		foreach (JsonHomeworld jsonHomeworld in DataHandler.dictHomeworlds.Values)
		{
			GUIChargenHomeworldRow component = UnityEngine.Object.Instantiate<GameObject>(this.rowRegionTemplate, this.tfRegions).GetComponent<GUIChargenHomeworldRow>();
			component.Init(jsonHomeworld, this.grpRegions, new Action<JsonHomeworld>(this.ChangeRegion));
			this.dictRegionToggles[jsonHomeworld.strATCCode] = component.chkRegion;
		}
	}

	private void ChangeRegion(JsonHomeworld jsh)
	{
		if (this.bIgnoreListeners)
		{
			return;
		}
		int defaultStrata = this.nStrataOld;
		if (jsh != null && !GUIChargenHomeworld.IsValidStrata(defaultStrata, jsh))
		{
			defaultStrata = this.GetDefaultStrata(jsh);
			if (defaultStrata < 0)
			{
				Debug.Log(string.Concat(new object[]
				{
					"Cannot set strata ",
					defaultStrata,
					" on ",
					jsh.strATCCode
				}));
				return;
			}
		}
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		objThem.GetComponent<GUIChargenStack>().ChangeHomeworld(jsh, defaultStrata);
		this.nStrataOld = defaultStrata;
		this.jshOld = jsh;
		this.SetUI(this.jshOld, this.nStrataOld);
	}

	private void ChangeStrata(int nStrataNew)
	{
		if (this.bIgnoreListeners)
		{
			return;
		}
		if (this.jshOld != null && !GUIChargenHomeworld.IsValidStrata(nStrataNew, this.jshOld))
		{
			nStrataNew = this.GetDefaultStrata(this.jshOld);
			if (nStrataNew < 0)
			{
				Debug.Log(string.Concat(new object[]
				{
					"Cannot set strata ",
					nStrataNew,
					" on ",
					this.jshOld.strATCCode
				}));
				return;
			}
		}
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		objThem.GetComponent<GUIChargenStack>().ChangeHomeworld(this.jshOld, nStrataNew);
		this.nStrataOld = nStrataNew;
		this.SetUI(this.jshOld, this.nStrataOld);
	}

	public static bool IsValidStrata(int nStrata, JsonHomeworld jsh)
	{
		if (jsh == null)
		{
			return false;
		}
		if (nStrata == 2)
		{
			return jsh.aCondsCitizen != null && jsh.aCondsCitizen.Length > 0;
		}
		if (nStrata != 1)
		{
			return nStrata == 0 && jsh.aCondsIllegal != null && jsh.aCondsIllegal.Length > 0;
		}
		return jsh.aCondsResident != null && jsh.aCondsResident.Length > 0;
	}

	private int GetDefaultStrata(JsonHomeworld jsh)
	{
		if (jsh == null)
		{
			return -1;
		}
		if (GUIChargenHomeworld.IsValidStrata(0, jsh))
		{
			return 0;
		}
		if (GUIChargenHomeworld.IsValidStrata(1, jsh))
		{
			return 1;
		}
		if (GUIChargenHomeworld.IsValidStrata(2, jsh))
		{
			return 2;
		}
		return -1;
	}

	private void GetDataFromCO()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		GUIChargenStack component = objThem.GetComponent<GUIChargenStack>();
		this.SetUI(component.GetHomeworld(), component.Strata);
		this.btnPamphlet.GetComponent<Animator>().SetInteger("AnimState", 1);
		this.COSelf.AddCondAmount("IsHomeworldSet", 1.0, 0.0, 0f);
		MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(this.COSelf.strID);
	}

	private void SetUI(JsonHomeworld jsh, int nStrata)
	{
		this.jshOld = jsh;
		this.nStrataOld = nStrata;
		string text = string.Empty;
		if (this.jshOld != null && this.dictRegionToggles.ContainsKey(this.jshOld.strATCCode))
		{
			this.bIgnoreListeners = true;
			this.dictRegionToggles[this.jshOld.strATCCode].isOn = true;
			this.dictPaymentToggles[this.nStrataOld].isOn = true;
			this.bIgnoreListeners = false;
			this.lblRegion.text = this.jshOld.strATCCode;
			this.lblPayment.text = this.aPaymentText[nStrata];
			this.lblPaymentSub.text = this.aPaymentSubtext[nStrata];
			this.lblRegionPamphlet.text = this.jshOld.strATCCode;
			string[] array;
			switch (this.nStrataOld)
			{
			case 0:
				array = this.jshOld.aCondsIllegal;
				break;
			case 1:
				array = this.jshOld.aCondsResident;
				break;
			case 2:
				array = this.jshOld.aCondsCitizen;
				break;
			default:
				array = new string[0];
				break;
			}
			foreach (string key in array)
			{
				if (DataHandler.dictConds.ContainsKey(key))
				{
					text = text + "- " + DataHandler.dictConds[key].strDesc.Replace("[us] ", string.Empty) + "\n";
				}
			}
			bool flag = GUIChargenHomeworld.IsValidStrata(2, this.jshOld);
			this.dictPaymentCaps[2].enabled = !flag;
			this.dictPaymentToggles[2].enabled = flag;
			flag = GUIChargenHomeworld.IsValidStrata(1, this.jshOld);
			this.dictPaymentCaps[1].enabled = !flag;
			this.dictPaymentToggles[1].enabled = flag;
			flag = GUIChargenHomeworld.IsValidStrata(0, this.jshOld);
			this.dictPaymentCaps[0].enabled = !flag;
			this.dictPaymentToggles[0].enabled = flag;
		}
		else
		{
			this.lblRegion.text = "----";
			this.lblPayment.text = "----";
			this.lblPaymentSub.text = string.Empty;
			this.lblRegionPamphlet.text = "----";
			bool flag2 = false;
			this.dictPaymentCaps[2].enabled = !flag2;
			this.dictPaymentToggles[2].enabled = flag2;
			this.dictPaymentCaps[1].enabled = !flag2;
			this.dictPaymentToggles[1].enabled = flag2;
			this.dictPaymentCaps[0].enabled = !flag2;
			this.dictPaymentToggles[0].enabled = flag2;
		}
		this.lblRisks.text = text;
	}

	private void TogglePamphlet()
	{
		int integer = this.btnPamphlet.GetComponent<Animator>().GetInteger("AnimState");
		if (integer == 1)
		{
			this.btnPamphlet.GetComponent<Animator>().SetInteger("AnimState", 0);
		}
		else
		{
			this.btnPamphlet.GetComponent<Animator>().SetInteger("AnimState", 1);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.GetDataFromCO();
	}

	public const int STRATA_ILLEGAL = 0;

	public const int STRATA_RESIDENT = 1;

	public const int STRATA_CITIZEN = 2;

	public const int ANIM_OPEN = 0;

	public const int ANIM_CLOSED = 1;

	private Text lblRegion;

	private Text lblPayment;

	private Text lblPaymentSub;

	private Text lblRegionPamphlet;

	private Text lblRisks;

	private Button btnPamphlet;

	private Transform tfRegions;

	private ToggleGroup grpRegions;

	private GameObject rowRegionTemplate;

	private JsonHomeworld jshOld;

	private int nStrataOld;

	private Dictionary<string, Toggle> dictRegionToggles;

	private Dictionary<int, Toggle> dictPaymentToggles;

	private Dictionary<int, Image> dictPaymentCaps;

	private List<string> aPaymentText;

	private List<string> aPaymentSubtext;

	private bool bIgnoreListeners;
}
