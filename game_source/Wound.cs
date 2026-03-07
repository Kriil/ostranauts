using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events;
using Ostranauts.Events.DTOs;
using UnityEngine;

public class Wound : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		if (Wound.OnWoundUpdated == null)
		{
			Wound.OnWoundUpdated = new WoundUpdatedEvent();
		}
		this.coUs = base.GetComponent<CondOwner>();
		this.aPNGsNonBlank = new List<JsonStringPair>();
		this.mapThresholdSlotEffects = new Dictionary<string, JsonSlotEffects>();
		this.aCondsBefore = new HashSet<string>();
		this.aSlotOverlaps = new List<string>();
		this.m_vBluntStrings = new List<string>();
		this.m_vCutStrings = new List<string>();
		Wound.aBluntAudio = new List<string>
		{
			"AnimHitBluntSound01",
			"AnimHitBluntSound02",
			"AnimHitBluntSound03"
		};
		Wound.aCutAudio = new List<string>
		{
			"AnimHitCutSound01",
			"AnimHitCutSound02",
			"AnimHitCutSound03"
		};
	}

	private void Update()
	{
		if (this.dfEpochLast <= 0.0)
		{
			this.dfEpochLast = StarSystem.fEpoch;
		}
		double num = StarSystem.fEpoch - this.dfEpochLast;
		if (num >= 1.0)
		{
			this.Run(num);
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	private void Run(double fElapsed)
	{
		double num = fElapsed / 3600.0;
		if (this.coUs == null)
		{
			Debug.LogError("ERROR: Null CO on wound " + this.strName);
			this.dfEpochLast = StarSystem.fEpoch;
			return;
		}
		CondOwner condOwner = this.coUs.RootParent(null);
		if (condOwner == null)
		{
			Debug.LogError("ERROR: Null RootParent() on wound " + this.coUs.strName);
			this.dfEpochLast = StarSystem.fEpoch;
			return;
		}
		double condAmount = condOwner.GetCondAmount("StatWoundFraction", false);
		this.coUs.SetCondAmount("ThreshStatWoundCut", condAmount, 0.0);
		this.coUs.SetCondAmount("ThreshStatWoundBlunt", condAmount, 0.0);
		double num2 = condOwner.GetCondAmount("StatWoundHealRate", false);
		if (condOwner.HasCond("DcGrav01"))
		{
			num2 *= 0.05;
		}
		double num3 = this.coUs.GetCondAmount("StatWoundBlunt", false);
		double num4 = this.coUs.GetCondAmount("StatWoundCut", false);
		double num5 = this.coUs.GetCondAmount("StatInfectionRate", false);
		double num6 = this.coUs.GetCondAmount("StatBloodRate", false);
		double num7 = this.coUs.GetCondAmount("StatPain", false);
		double num8 = this.coUs.GetCondAmount("StatDisinfectAmount", false);
		bool flag = this.coUs.HasCond("IsSplinted");
		double num9 = num2;
		if (this.coUs.HasCond("FracturedBone") && !flag)
		{
			num9 *= (double)MathUtils.Rand(0f, 1f, MathUtils.RandType.Mid, null) - 0.5;
		}
		if (num8 > 0.0)
		{
			num8 = MathUtils.Clamp(num8, 0.0, 1.0);
			num5 *= 1.0 - num8;
			this.coUs.ZeroCondAmount("StatDisinfectAmount");
		}
		num5 += num5 * num * num4 * 0.1;
		num4 -= num4 * num * num2 * (1.0 - num5) * 0.5;
		num3 -= num3 * num * num9 * (1.0 - num5) * 0.5;
		if (num4 < 0.01)
		{
			num4 = 0.0;
		}
		if (num3 < 0.01)
		{
			num3 = 0.0;
		}
		if (num4 <= 0.0)
		{
			num5 = 0.0;
		}
		if (num6 >= 0.1)
		{
			num6 -= 0.5 * num6 * num * num2 * 10.0;
		}
		else
		{
			num6 = 0.0;
		}
		double num10 = num7;
		num7 = this.CalcPain(num4, num3, num5, this.coUs.HasCond("FracturedBone"));
		this.ValidateStats(ref num5, ref num6, ref num7);
		double num11 = (num7 - num10) * (num7 - num10);
		if (num10 > num7)
		{
			num11 = -num11;
		}
		this.coUs.SetCondAmount("StatWoundBlunt", num3, 0.0);
		this.coUs.SetCondAmount("StatWoundCut", num4, 0.0);
		this.coUs.SetCondAmount("StatInfectionRate", num5, 0.0);
		this.coUs.SetCondAmount("StatBloodRate", num6, 0.0);
		this.coUs.SetCondAmount("StatPain", num10 + num11, 0.0);
		this.ApplyEffectsParent(condOwner, num, num6, num5 * num4, num11 * 66.0);
		this.dfEpochLast = StarSystem.fEpoch;
		this.fDmgLeft = MathUtils.Max(1.0 - num3, 1.0 - num4);
	}

	private void ValidateStats(ref double m_fInfectRate, ref double m_fBleedRate, ref double m_fPain)
	{
		if (m_fInfectRate < 0.0)
		{
			m_fInfectRate = 0.0;
		}
		else if (m_fInfectRate > 1.0)
		{
			m_fInfectRate = 1.0;
		}
		if (m_fBleedRate < 0.0)
		{
			m_fBleedRate = 0.0;
		}
		else if (m_fBleedRate > 1.0)
		{
			m_fBleedRate = 1.0;
		}
		if (m_fPain < 0.0)
		{
			m_fPain = 0.0;
		}
		else if (m_fPain > 1.0)
		{
			m_fPain = 1.0;
		}
	}

	private void ApplyEffectsParent(CondOwner coRootParent, double fHours, double m_fBleedAmount, double m_fInfectAmount, double m_fPainAmount)
	{
		bool flag = this.coUs.HasCond("IsStaunched", false);
		if (!flag && m_fBleedAmount > 0.0)
		{
			coRootParent.AddCondAmount("StatBlood", m_fBleedAmount * fHours, 0.0, 0f);
		}
		coRootParent.AddCondAmount("StatInfection", m_fInfectAmount * fHours, 0.0, 0f);
		coRootParent.AddCondAmount("StatPain", m_fPainAmount, 0.0, 0f);
		CondOwner condOwner = null;
		if (this.coUs.slotNow != null && this.coUs.slotNow.compSlots != null)
		{
			condOwner = this.coUs.slotNow.compSlots.GetComponent<CondOwner>();
		}
		if (condOwner == null)
		{
			return;
		}
		CondOwner condOwner2 = this.coUs;
		if (condOwner2.objCOParent != null)
		{
			condOwner2 = condOwner2.RootParent(null);
		}
		bool flag2 = m_fPainAmount != 0.0;
		this.strMergedName = string.Empty;
		this.aPNGsNonBlank.Clear();
		IEnumerable<KeyValuePair<string, JsonSlotEffects>> enumerable = (m_fPainAmount + m_fBleedAmount + m_fInfectAmount <= 0.0) ? this.mapThresholdSlotEffects.Reverse<KeyValuePair<string, JsonSlotEffects>>() : this.mapThresholdSlotEffects;
		foreach (KeyValuePair<string, JsonSlotEffects> keyValuePair in enumerable)
		{
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = this.coUs.HasCond(keyValuePair.Key);
			if (flag5 && flag && keyValuePair.Key.IndexOf("Blood") >= 0)
			{
				flag5 = false;
			}
			if (this.aCondsBefore.Contains(keyValuePair.Key))
			{
				if (!flag5)
				{
					flag4 = true;
				}
			}
			else if (flag5)
			{
				flag3 = true;
			}
			if (flag3)
			{
				Slots.ApplyIAEffects(condOwner, this.coUs, keyValuePair.Value, false, false);
				if (condOwner2 != null)
				{
					this.ApplyMeshEffects(keyValuePair.Value, this.coUs.slotNow, condOwner2.Crew, false);
				}
				if (!this.aCondsBefore.Contains(keyValuePair.Key))
				{
					this.aCondsBefore.Add(keyValuePair.Key);
				}
				flag2 = true;
			}
			if (flag4)
			{
				Slots.ApplyIAEffects(condOwner, this.coUs, keyValuePair.Value, true, false);
				if (condOwner2 != null)
				{
					this.ApplyMeshEffects(keyValuePair.Value, this.coUs.slotNow, condOwner2.GetComponent<Crew>(), true);
				}
				this.aCondsBefore.Remove(keyValuePair.Key);
				flag2 = true;
			}
			if (flag5 && keyValuePair.Value.strSlotImage != null && keyValuePair.Value.strSlotImage != string.Empty && !this.strMergedName.Contains(keyValuePair.Value.strSlotImage))
			{
				this.strMergedName += keyValuePair.Value.strSlotImage;
				JsonStringPair jsonStringPair = new JsonStringPair();
				jsonStringPair.strName = keyValuePair.Value.strSlotImage;
				this.aPNGsNonBlank.Add(jsonStringPair);
			}
		}
		if (!flag2)
		{
			return;
		}
		Wound.OnWoundUpdated.Invoke(new PaperdollWoundDTO
		{
			CoDoll = condOwner2,
			CoSlot = this.coUs,
			WoundTex = this.GetSlotImage()
		});
	}

	private void ApplyMeshEffects(JsonSlotEffects jse, Slot slot, Crew crew, bool bRemove)
	{
		if (jse == null || crew == null || slot == null)
		{
			return;
		}
		Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(jse.mapMeshTextures, null);
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			string[] array = keyValuePair.Value.Split(new char[]
			{
				':'
			});
			if (array.Length > 0)
			{
				string strValue = array[0];
				string strValueNorm = array[0] + "n";
				if (array.Length > 1)
				{
					strValueNorm = array[1];
				}
				crew.OuterParts(keyValuePair.Key, slot, strValue, strValueNorm, bRemove);
			}
		}
	}

	public Texture2D GetSlotImage()
	{
		if (this.strMergedName == null || this.strMergedName == string.Empty)
		{
			this.strMergedName = "paperdoll/bodyBlank";
		}
		Texture2D texture2D = DataHandler.LoadPNG(this.strMergedName + ".png", false, false);
		if (texture2D.name == "missing.png")
		{
			Debug.Log("Generating PNG: " + this.strMergedName);
			Crew.MergeTextures(this.aPNGsNonBlank, this.strMergedName, false);
			texture2D = DataHandler.LoadPNG(this.strMergedName + ".png", false, false);
		}
		return texture2D;
	}

	public double DamageLeft()
	{
		return this.fDmgLeft / 0.1;
	}

	public Vector2 Damage(JsonAttackMode jam, CondOwner coSource, bool bUnsocket = true, string strAttacker = null)
	{
		if (jam != null)
		{
			return this.Damage((double)jam.fDmgBlunt * jam.GetDmgAmount(coSource), (double)jam.fDmgCut * jam.GetDmgAmount(coSource), (double)jam.fPenetration, coSource, jam.strNameFriendly, bUnsocket, strAttacker, false);
		}
		return Vector2.zero;
	}

	public Vector2 Damage(double fBluntAmount, double fCutAmount, double fPenetration, CondOwner coSource, string strWith, bool bUnsocket = true, string strAttacker = null, bool bSkipArmor = false)
	{
		if (!this.Bluntable)
		{
			fBluntAmount = 0.0;
		}
		if (!this.Cuttable)
		{
			fCutAmount = 0.0;
		}
		if (fBluntAmount < 0.0 || fCutAmount < 0.0)
		{
			return default(Vector2);
		}
		double num = StarSystem.fEpoch - this.dfEpochLast;
		if (num > 0.0)
		{
			this.Run(num);
		}
		CondOwner condOwner = this.coUs;
		if (this.coUs.slotNow != null)
		{
			condOwner = this.coUs.RootParent(null);
		}
		string friendlyName = condOwner.FriendlyName;
		fBluntAmount *= 0.1;
		fCutAmount *= 0.1;
		double num2 = this.coUs.GetCondAmount("StatArmorCut");
		double num3 = this.coUs.GetCondAmount("StatArmorBlunt");
		List<CondOwner> list = new List<CondOwner>();
		foreach (string strSlot in this.aSlotOverlaps)
		{
			if (bSkipArmor)
			{
				break;
			}
			if (condOwner.compSlots == null)
			{
				break;
			}
			Slot slot = condOwner.compSlots.GetSlot(strSlot);
			if (slot != null)
			{
				CondOwner outermostCO = slot.GetOutermostCO();
				if (outermostCO != null)
				{
					if (!list.Contains(outermostCO))
					{
						list.Add(outermostCO);
						num2 += outermostCO.GetCondAmount("StatArmorCut");
						num3 += outermostCO.GetCondAmount("StatArmorBlunt");
					}
				}
			}
		}
		double num4 = 0.0;
		double num5 = 0.0;
		if (fCutAmount > 0.0 && num2 > 0.0)
		{
			if (num2 >= fPenetration)
			{
				num4 = 1.0;
			}
			else
			{
				num4 = 1.0 - (fPenetration - num2) / fPenetration;
			}
			num5 = num4 * fCutAmount;
			fBluntAmount += num5;
			fCutAmount -= num5;
		}
		if (fBluntAmount > 0.0 && num3 > 0.0)
		{
			if (num3 >= fBluntAmount)
			{
				num5 += fBluntAmount;
				fBluntAmount = 0.0;
			}
			else
			{
				num5 += num3;
				fBluntAmount -= num3;
			}
		}
		if (num5 > 0.0)
		{
			foreach (CondOwner condOwner2 in list)
			{
				if (condOwner2 != null)
				{
					condOwner2.AddCondAmount("StatDamage", num5 / 0.1, 0.0, 0f);
				}
			}
		}
		if (fBluntAmount < 0.0)
		{
			fBluntAmount = 0.0;
		}
		if (fCutAmount < 0.0)
		{
			fCutAmount = 0.0;
		}
		double num6 = this.coUs.GetCondAmount("StatWoundBlunt");
		double num7 = this.coUs.GetCondAmount("StatWoundCut");
		double num8 = this.coUs.GetCondAmount("StatInfectionRate");
		double num9 = this.coUs.GetCondAmount("StatBloodRate");
		double num10 = this.coUs.GetCondAmount("StatPain");
		num7 += fCutAmount;
		num6 += fBluntAmount;
		if (num7 < 0.0)
		{
			num7 = 0.0;
		}
		if (num6 < 0.0)
		{
			num6 = 0.0;
		}
		if (this.coUs.compSlots != null && fCutAmount + fBluntAmount > 0.0)
		{
			foreach (Slot slot2 in this.coUs.compSlots.GetSlotsDepthFirst(false))
			{
				CondOwner outermostCO2 = slot2.GetOutermostCO();
				if (outermostCO2 != null)
				{
					if (num4 > 0.0)
					{
						outermostCO2.AddCondAmount("StatDamage", num5 / 0.1, 0.0, 0f);
					}
					if (fBluntAmount > 0.0 && num3 > 0.0)
					{
						outermostCO2.AddCondAmount("StatDamage", MathUtils.Min(num3, fBluntAmount) / 0.1, 0.0, 0f);
					}
				}
				if (bUnsocket)
				{
					this.coUs.compSlots.UnSlotItem(slot2.strName, null, false);
				}
			}
			if (condOwner == CrewSim.GetSelectedCrew())
			{
				BeatManager.ResetTensionTimer();
			}
		}
		if (fCutAmount > 0.0)
		{
			num8 += 0.05 * MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null);
			if (num9 < num7)
			{
				num9 = num7;
			}
		}
		else if (this.bBleeds && fBluntAmount > 0.0 && num6 > 0.5)
		{
			num9 += num6 - 0.5;
		}
		double num11 = num10;
		num10 = this.CalcPain(num7, num6, num8, this.coUs.HasCond("FracturedBone"));
		string text = this.coUs.FriendlyName.ToLower();
		string text2;
		if (fBluntAmount <= 0.0 && fCutAmount <= 0.0 && (num3 > 0.0 || num2 > 0.0))
		{
			text = "armor";
			text2 = "barely affected";
		}
		else if (fBluntAmount > fCutAmount)
		{
			if (this.m_vBluntStrings.Count > 0)
			{
				text2 = this.m_vBluntStrings[this.GetIndex(fBluntAmount, this.m_vBluntStrings.Count)];
			}
			else if (this.m_vCutStrings.Count > 0)
			{
				text2 = this.m_vCutStrings[this.GetIndex(fBluntAmount, this.m_vCutStrings.Count)];
			}
			else
			{
				text2 = "damaged";
			}
			if (Wound.bAudio)
			{
				AudioEmitter component = this.coUs.RootParent(null).GetComponent<AudioEmitter>();
				if (component != null)
				{
					component.StartOther(Wound.aBluntAudio[this.GetIndex(fBluntAmount, Wound.aBluntAudio.Count)]);
				}
			}
		}
		else
		{
			if (this.m_vCutStrings.Count > 0)
			{
				text2 = this.m_vCutStrings[this.GetIndex(fCutAmount, this.m_vCutStrings.Count)];
			}
			else if (this.m_vBluntStrings.Count > 0)
			{
				text2 = this.m_vBluntStrings[this.GetIndex(fCutAmount, this.m_vBluntStrings.Count)];
			}
			else
			{
				text2 = "damaged";
			}
			if (Wound.bAudio)
			{
				AudioEmitter component2 = this.coUs.RootParent(null).GetComponent<AudioEmitter>();
				if (component2 != null)
				{
					component2.StartOther(Wound.aCutAudio[this.GetIndex(fCutAmount, Wound.aCutAudio.Count)]);
				}
			}
		}
		if (Wound.bAudio && this.coUs.RootParent(null) == CrewSim.GetSelectedCrew())
		{
			CrewSim.objInstance.CamShake(Mathf.Max((float)fBluntAmount, (float)fCutAmount));
		}
		string text3;
		if (!string.IsNullOrEmpty(strAttacker))
		{
			text3 = string.Concat(new string[]
			{
				strAttacker,
				" ",
				text2,
				" ",
				friendlyName,
				"'s ",
				text
			});
		}
		else if (coSource == null)
		{
			text3 = string.Concat(new string[]
			{
				friendlyName,
				"'s ",
				text,
				" was ",
				text2
			});
		}
		else
		{
			text3 = string.Concat(new string[]
			{
				coSource.FriendlyName,
				" ",
				text2,
				" ",
				friendlyName,
				"'s ",
				text
			});
		}
		if (!string.IsNullOrEmpty(strWith))
		{
			text3 = text3 + " with a " + strWith;
		}
		text3 += ".";
		condOwner.LogMessage(text3, "Bad", condOwner.strName);
		if (coSource != null)
		{
			coSource.LogMessage(text3, "Bad", condOwner.strName);
		}
		this.ValidateStats(ref num8, ref num9, ref num10);
		double num12 = (num10 - num11) * (num10 - num11);
		this.coUs.SetCondAmount("StatWoundBlunt", num6, 0.0);
		this.coUs.SetCondAmount("StatWoundCut", num7, 0.0);
		this.coUs.SetCondAmount("StatInfectionRate", num8, 0.0);
		this.coUs.SetCondAmount("StatBloodRate", num9, 0.0);
		this.coUs.SetCondAmount("StatPain", num11 + num12, 0.0);
		this.coUs.RootParent(null).AddCondAmount("StatPain", num12 * 66.0, 0.0, 0f);
		this.Run(0.0);
		if (fCutAmount + fBluntAmount >= 0.5 && !condOwner.HasCond("Unconscious"))
		{
			condOwner.AddCondAmount("Stunned", 1.0, 0.0, 0f);
			condOwner.AICancelAll(coSource);
		}
		if (this.coUs.HasCond("Crippled") && this.coUs.objCOParent != null && this.coUs.objCOParent.compSlots != null)
		{
			foreach (Slot slot3 in this.coUs.objCOParent.compSlots.GetSlotsHeldFirst(true))
			{
				if (slot3.bHoldSlot)
				{
					for (int i = 0; i < slot3.aCOs.Length; i++)
					{
						CondOwner condOwner3 = slot3.aCOs[i];
						if (!(condOwner3 == null))
						{
							if (!condOwner3.HasCond("IsHiddenInv") && !condOwner3.HasCond("IsSystem") && !condOwner3.bSlotLocked)
							{
								condOwner.DropCO(condOwner3, false, condOwner.ship, 0f, 0f, true, null);
							}
						}
					}
				}
			}
		}
		return new Vector2((float)fBluntAmount, (float)fCutAmount);
	}

	private double CalcPain(double fCut, double fBlunt, double fInfect, bool bBrokenBone)
	{
		double num = MathUtils.Max(fCut, fBlunt);
		num += fInfect;
		if (bBrokenBone)
		{
			num += 1.0;
		}
		return num;
	}

	private int GetIndex(double fVal, int nLength)
	{
		if (nLength <= 0)
		{
			return 0;
		}
		double num = MathUtils.Max(fVal * (double)nLength, 0.0);
		int value = Mathf.RoundToInt((float)num);
		return Mathf.Clamp(value, 0, nLength - 1);
	}

	public Texture2D GetPainPNG()
	{
		return DataHandler.LoadPNG(this.strPainPNG + ".png", false, false);
	}

	public void PostGameLoad()
	{
		this.Run(0.0);
	}

	public void SetData(string strName)
	{
		JsonWound wound = DataHandler.GetWound(strName);
		if (wound == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		this.strName = strName;
		this.strPainPNG = wound.strPainPNG;
		this.bBleeds = wound.bBleeds;
		this.fHitChance = wound.fHitChance;
		if (wound.aSlotOverlaps != null)
		{
			this.aSlotOverlaps = new List<string>(wound.aSlotOverlaps);
		}
		Loot loot = DataHandler.GetLoot(wound.strLootBluntVerbs);
		this.m_vBluntStrings = loot.GetLootNames(null, false, null);
		loot = DataHandler.GetLoot(wound.strLootCutVerbs);
		this.m_vCutStrings = loot.GetLootNames(null, false, null);
		foreach (KeyValuePair<string, string> keyValuePair in wound.mapEffects)
		{
			JsonSlotEffects slotEffect = DataHandler.GetSlotEffect(keyValuePair.Value);
			if (slotEffect != null)
			{
				this.mapThresholdSlotEffects[keyValuePair.Key] = slotEffect;
			}
		}
	}

	public bool Bluntable
	{
		get
		{
			return this.m_vBluntStrings != null && this.m_vBluntStrings.Count != 0;
		}
	}

	public bool Cuttable
	{
		get
		{
			return this.m_vCutStrings != null && this.m_vCutStrings.Count != 0;
		}
	}

	public static CondTrigger CTWound
	{
		get
		{
			if (Wound._ctWound == null)
			{
				Wound._ctWound = DataHandler.GetCondTrigger("TIsWound");
			}
			return Wound._ctWound;
		}
	}

	public static WoundUpdatedEvent OnWoundUpdated = new WoundUpdatedEvent();

	public string strName;

	private string strPainPNG;

	public double fDmgLeft = 1.0;

	private bool bBleeds;

	public float fHitChance;

	public CondOwner coUs;

	private double dfEpochLast = -1.0;

	private Dictionary<string, JsonSlotEffects> mapThresholdSlotEffects;

	private HashSet<string> aCondsBefore = new HashSet<string>();

	private string strMergedName;

	private List<JsonStringPair> aPNGsNonBlank;

	private List<string> aSlotOverlaps;

	private List<string> m_vBluntStrings;

	private List<string> m_vCutStrings;

	private static List<string> aBluntAudio;

	private static List<string> aCutAudio;

	private static CondTrigger _ctWound;

	public static bool bAudio = false;

	public const double fDmgCoeff = 0.1;

	private const double m_fPainCoeff = 66.0;

	private const double dfUpdateInterval = 1.0;
}
