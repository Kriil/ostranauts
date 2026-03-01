using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUILocks : GUIData
{
	protected override void Awake()
	{
		base.Awake();
	}

	private void Update()
	{
		this.KeyHandler();
		float num = CrewSim.fTotalGameSec - this.fUpdateLast;
		if (num >= 1f)
		{
			this.UpdateLockLamps();
		}
		if (this.bHackingEnabled)
		{
			this.HackingActive();
		}
	}

	private void PowerPack()
	{
		this.ClearKey();
		this.UpdateLockLamps();
	}

	private void TryHack(GUIContextButton gcb)
	{
		if (!gcb.GetComponent<Toggle>().isOn)
		{
			return;
		}
		if (this.COSelf.HasCond("IsOff"))
		{
			return;
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		Interaction interaction = DataHandler.GetInteraction("HackingCrimePenalty", null, false);
		if (interaction != null)
		{
			interaction.objUs = selectedCrew;
			interaction.objThem = this.COSelf;
			interaction.ApplyChain(null);
		}
		gcb.GetComponent<Toggle>().isOn = false;
		if (selectedCrew.HasCond("SkillHacking"))
		{
			this.bHackingEnabled = true;
			return;
		}
		selectedCrew.LogMessage(DataHandler.GetString("GUI_LOCK_HACK_FAIL01", false), "Bad", selectedCrew.strName);
		selectedCrew.LogMessage(DataHandler.GetString("GUI_LOCK_HACK_FAIL02", false), "Bad", selectedCrew.strName);
		this.bHackingEnabled = false;
	}

	private void HackingActive()
	{
		string text = this.sbDigits.ToString() + "7524";
		this.sbDigits.Length = 0;
		for (int i = 0; i < 4; i++)
		{
			char c = text[i];
			int num = 40;
			if (c == this.strAccessCode[i])
			{
				num /= 10;
			}
			if (i == 0)
			{
				num /= 2;
			}
			if (i == 2)
			{
				num /= 3;
			}
			if (UnityEngine.Random.Range(0, 100) < num)
			{
				c = (char)UnityEngine.Random.Range(48f, 57.9999f);
			}
			this.sbDigits.Append(c);
		}
		string text2 = this.sbDigits.ToString();
		this.pinDigits.State = ((!(text2 == this.strAccessCode) && UnityEngine.Random.Range(0, 100) >= 95) ? 0 : 2);
		this.pinDigits.SetValue(text2);
		if (text2 == this.strAccessCode && this.pinDigits.State == 2)
		{
			this.bHackingEnabled = false;
		}
	}

	private void LockSetup(GUILocks.LockType lt)
	{
		if (lt == GUILocks.LockType.PIN)
		{
			this.CurrentLockMode = GUILocks.LockType.PIN;
			this.sbDigits = new StringBuilder(4, 4);
			if (this.dictPropMap.ContainsKey("strPIN"))
			{
				this.strAccessCode = this.dictPropMap["strPIN"];
			}
			TMP_Text component = base.transform.Find("pnlLocks/pnlPIN/txtBrand").GetComponent<TMP_Text>();
			string empty = string.Empty;
			this.dictPropMap.TryGetValue("strBrand", out empty);
			component.text = empty;
			component = base.transform.Find("pnlLocks/pnlPIN/txtBrandSub").GetComponent<TMP_Text>();
			empty = string.Empty;
			this.dictPropMap.TryGetValue("strBrandSub", out empty);
			component.text = empty;
			this.pinDigits = base.transform.Find("pnlLocks/pnlPIN/pnlDigits").GetComponent<GUI7Seg>();
			this.lampLocked = base.transform.Find("pnlLocks/pnlPIN/bmpLocked").GetComponent<GUILamp>();
			this.lampUnlocked = base.transform.Find("pnlLocks/pnlPIN/bmpUnlocked").GetComponent<GUILamp>();
			for (int i = 0; i < 10; i++)
			{
				Button component2 = base.transform.Find("pnlLocks/pnlPIN/btn" + i).GetComponent<Button>();
				int nDigit = i;
				component2.onClick.RemoveAllListeners();
				component2.onClick.AddListener(delegate()
				{
					this.NumKey(nDigit.ToString());
				});
				AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnLockIn", "ShipUIBtnLockOut");
			}
			Button component3 = base.transform.Find("pnlLocks/pnlPIN/btnCE").GetComponent<Button>();
			component3.onClick.RemoveAllListeners();
			component3.onClick.AddListener(delegate()
			{
				this.ClearKey();
			});
			AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnLockIn", "ShipUIBtnLockOut");
			component3 = base.transform.Find("pnlLocks/pnlPIN/btnLock").GetComponent<Button>();
			component3.onClick.RemoveAllListeners();
			component3.onClick.AddListener(delegate()
			{
				this.LockKey();
			});
			AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnLockIn", "ShipUIBtnLockOut");
			component3 = base.transform.Find("pnlLocks/pnlPIN/btnUnlock").GetComponent<Button>();
			component3.onClick.RemoveAllListeners();
			component3.onClick.AddListener(delegate()
			{
				this.UnlockKey();
			});
			AudioManager.AddBtnAudio(component3.gameObject, "ShipUIBtnLockIn", "ShipUIBtnLockOut");
			if (this.COSelf.HasCond("IsFactoryReset"))
			{
				this.bmpLamp.State = 2;
			}
			else
			{
				this.bmpLamp.State = 0;
			}
			this.bLocked = this.COSelf.HasCond("IsLocked");
			this.UpdateLockLamps();
		}
	}

	private void UpdateLockLamps()
	{
		if (this.COSelf.HasCond("IsOff"))
		{
			this.lampLocked.State = 0;
			this.lampUnlocked.State = 0;
		}
		else if (this.bLocked)
		{
			this.lampLocked.State = 3;
			this.lampUnlocked.State = 0;
		}
		else
		{
			this.lampLocked.State = 0;
			this.lampUnlocked.State = 3;
		}
		this.fUpdateLast = CrewSim.fTotalGameSec;
	}

	private void SetPIN()
	{
		this.strAccessCode = this.sbDigits.ToString();
		base.SetPropMapData("strPIN", this.strAccessCode);
		this.COSelf.ZeroCondAmount("IsFactoryReset");
		this.bmpLamp.State = 0;
		this.COSelf.SetCondAmount("StatDebugProgressMax", 877.0, 0.0);
	}

	private void NumKey(string str)
	{
		if (this.COSelf.HasCond("IsOff"))
		{
			this.ClearKey();
			return;
		}
		AudioManager.am.PlayAudioEmitter("ShipUIBtnLockBeep", false, false);
		if (this.sbDigits.Length < this.sbDigits.MaxCapacity)
		{
			this.sbDigits.Append(str);
		}
		string text = this.sbDigits.ToString();
		if (text.Length == 0)
		{
			this.pinDigits.State = 0;
		}
		else
		{
			this.pinDigits.State = 2;
			this.pinDigits.SetValue(text);
		}
	}

	private void ClearKey()
	{
		if (!this.COSelf.HasCond("IsOff"))
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnLockBeep", false, false);
		}
		this.sbDigits.Length = 0;
		this.pinDigits.State = 0;
	}

	private void LockKey()
	{
		if (this.COSelf.HasCond("IsOff"))
		{
			this.ClearKey();
			return;
		}
		AudioManager.am.PlayAudioEmitter("ShipUIBtnLockBeep", false, false);
		if (this.COSelf.HasCond("IsFactoryReset") && this.sbDigits.Length == this.sbDigits.MaxCapacity)
		{
			this.SetPIN();
		}
		if (this.sbDigits.ToString() == this.strAccessCode)
		{
			Interaction interaction = DataHandler.GetInteraction("MSLock", null, false);
			Interaction interaction2 = interaction;
			CondOwner coself = this.COSelf;
			interaction.objThem = coself;
			interaction2.objUs = coself;
			interaction = interaction.GetReply();
			if (interaction != null)
			{
				this.ModeSwitchSelf(interaction.objLootModeSwitch);
			}
			AudioManager.am.PlayAudioEmitter("DoorLock", false, false);
		}
		this.ClearKey();
		this.UpdateLockLamps();
	}

	private void UnlockKey()
	{
		if (this.COSelf.HasCond("IsOff"))
		{
			this.ClearKey();
			return;
		}
		AudioManager.am.PlayAudioEmitter("ShipUIBtnLockBeep", false, false);
		if (this.COSelf.HasCond("IsFactoryReset") && this.sbDigits.Length == this.sbDigits.MaxCapacity)
		{
			this.SetPIN();
		}
		if (this.sbDigits.ToString() == this.strAccessCode)
		{
			Interaction interaction = DataHandler.GetInteraction("MSUnlock", null, false);
			Interaction interaction2 = interaction;
			CondOwner coself = this.COSelf;
			interaction.objThem = coself;
			interaction2.objUs = coself;
			interaction = interaction.GetReply();
			if (interaction != null)
			{
				this.ModeSwitchSelf(interaction.objLootModeSwitch);
			}
			AudioManager.am.PlayAudioEmitter("DoorUnlock", false, false);
		}
		this.ClearKey();
		this.UpdateLockLamps();
	}

	private void ModeSwitchSelf(Loot lootNew)
	{
		if (lootNew == null)
		{
			return;
		}
		List<string> lootNames = lootNew.GetLootNames(null, false, null);
		if (lootNames.Count > 0)
		{
			CondOwner condOwner = DataHandler.GetCondOwner(lootNames[0], null, null, !lootNew.bSuppress, null, null, this.strCoSelfID, null);
			this.COSelf.ModeSwitch(condOwner, this.COSelf.transform.position);
			this.Init(condOwner, condOwner.mapGUIPropMaps[this.strCOKey], this.strCOKey);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.bHackingEnabled = false;
		GUIContextButton component = (Resources.Load("btnSocialChoice") as GameObject).GetComponent<GUIContextButton>();
		Transform transform = base.transform.Find("pnlOptions/prefabScrollList/scrollMask/pnlContent");
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				UnityEngine.Object.Destroy(transform2.gameObject);
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
		if (!this.COSelf.HasCond("IsPowered") && this.COSelf.Pwr != null)
		{
			GUIContextButton guicontextButton = UnityEngine.Object.Instantiate<GUIContextButton>(component, transform);
			guicontextButton.GetComponentInChildren<TMP_Text>().text = DataHandler.GetString("GUI_LOCK_NO_POWER_BTN", false);
			if (CrewSim.GetSelectedCrew() != null)
			{
				guicontextButton.GetComponent<Toggle>().onValueChanged.AddListener(delegate(bool A_0)
				{
					string strMsg = DataHandler.GetString("GUI_LOCK_NO_POWER_MSG1", false) + GUIActionKeySelector.commandTogglePowerVis.KeyName + DataHandler.GetString("GUI_LOCK_NO_POWER_MSG2", false);
					CrewSim.GetSelectedCrew().LogMessage(strMsg, "Bad", CrewSim.GetSelectedCrew().strID);
				});
			}
		}
		else
		{
			List<CondOwner> list = new List<CondOwner>();
			if (CrewSim.GetSelectedCrew() != null)
			{
				list = CrewSim.GetSelectedCrew().GetCOs(false, GUILocks.CTPDA);
			}
			if (list == null)
			{
				return;
			}
			foreach (CondOwner x in list)
			{
				if (x != null)
				{
					GUIContextButton gcb = UnityEngine.Object.Instantiate<GUIContextButton>(component, transform);
					gcb.GetComponentInChildren<TMP_Text>().text = DataHandler.GetString("GUI_LOCK_HACK_BTN", false);
					gcb.GetComponent<Toggle>().onValueChanged.AddListener(delegate(bool A_1)
					{
						this.TryHack(gcb);
					});
					break;
				}
			}
		}
		if (coSelf.HasCond("IsLockPIN"))
		{
			this.LockSetup(GUILocks.LockType.PIN);
		}
	}

	private void KeyHandler()
	{
		if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
		{
			this.NumKey("0");
		}
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			this.NumKey("1");
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			this.NumKey("2");
		}
		if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
		{
			this.NumKey("3");
		}
		if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
		{
			this.NumKey("4");
		}
		if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
		{
			this.NumKey("5");
		}
		if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
		{
			this.NumKey("6");
		}
		if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
		{
			this.NumKey("7");
		}
		if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
		{
			this.NumKey("8");
		}
		if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
		{
			this.NumKey("9");
		}
		if (Input.GetKeyDown(KeyCode.Backspace))
		{
			this.ClearKey();
		}
		if (GUIActionKeySelector.commandAccept.Down)
		{
			if (this.bLocked)
			{
				this.UnlockKey();
			}
			else
			{
				this.LockKey();
			}
		}
	}

	public static bool IsLock(CondOwner co)
	{
		if (co == null)
		{
			return false;
		}
		if (GUILocks.ctIsLock == null)
		{
			GUILocks.ctIsLock = DataHandler.GetCondTrigger("TIsLock");
		}
		return GUILocks.ctIsLock.Triggered(co, null, false);
	}

	private static CondTrigger CTPDA
	{
		get
		{
			if (GUILocks.ctIsPDA == null)
			{
				DataHandler.GetCondTrigger("TIsComputerPDANotDamaged");
			}
			return GUILocks.ctIsPDA;
		}
	}

	private GUI7Seg pinDigits;

	private GUILocks.LockType CurrentLockMode;

	private GUILamp lampLocked;

	private GUILamp lampUnlocked;

	private float fUpdateLast;

	private bool bLocked;

	private StringBuilder sbDigits;

	private string strAccessCode = "1234";

	private bool bHackingEnabled;

	[SerializeField]
	protected GUILamp bmpLamp;

	private static CondTrigger ctIsLock;

	private static CondTrigger ctIsPDA;

	private enum LockType
	{
		Combo,
		Key,
		PIN,
		QKD,
		RFID
	}
}
