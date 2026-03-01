using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ostranauts.Components;
using Ostranauts.Core;
using Ostranauts.UI.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Options/settings UI for video, audio, controls, and interface preferences.
// This front-end reads and writes PlayerPrefs plus user settings used by
// systems like LoadManager autosaves, Visibility flicker, and date formatting.
public class GUIOptions : MonoBehaviour
{
	// Initializes dropdown label dictionaries for autosave and visual-effect settings.
	private void Awake()
	{
		this.dictAutoSaveInterval = new Dictionary<int, string>
		{
			{
				-1,
				"Off"
			},
			{
				300,
				"5 mins"
			},
			{
				600,
				"10 mins"
			},
			{
				900,
				"15 mins"
			},
			{
				1200,
				"20 mins"
			},
			{
				1800,
				"30 mins"
			},
			{
				3600,
				"60 mins"
			}
		};
		this.dictAutoSaveMaxCount = new Dictionary<int, string>
		{
			{
				3,
				"3"
			},
			{
				5,
				"5"
			},
			{
				7,
				"7"
			},
			{
				10,
				"10"
			}
		};
		this.dictFlickerAmount = new Dictionary<int, string>
		{
			{
				-1,
				"Off"
			},
			{
				1,
				"Soft"
			},
			{
				2,
				"Full"
			}
		};
	}

	// Waits until DataHandler is initialized before binding controls that depend on loaded settings/data.
	private IEnumerator Start()
	{
		yield return new WaitUntil(() => DataHandler.bInitialised);
		this.Init();
		yield break;
	}

	// Allows keyboard shortcuts such as Escape while the options UI is open.
	private void Update()
	{
		this.KeyHandler();
	}

	// Binds every tab, slider, toggle, and dropdown to PlayerPrefs-backed settings.
	// Likely used in both main menu and in-game pause/options flows.
	private void Init()
	{
		this.cgOptions = base.transform.parent.GetComponent<CanvasGroup>();
		this.btnQuit = base.transform.Find("pnlMenu/btnQuit/btn").GetComponent<Button>();
		this.btnQuit.onClick.AddListener(delegate()
		{
			this.Exit();
		});
		this.btnVideo = base.transform.Find("pnlMenu/btnVideo/btn").GetComponent<Button>();
		this.btnVideo.onClick.AddListener(delegate()
		{
			this.State = this.cgVideo;
		});
		this.chkScreen = base.transform.Find("pnlVideo/pnlStyle/chkScreen").GetComponent<Toggle>();
		this.chkScreen.isOn = Screen.fullScreen;
		this.chkScreen.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.SetRes();
		});
		this.chkScreen.GetComponent<GUIAudioToggle>().requiresInit = false;
		this.sldFPS = base.transform.Find("pnlVideo/pnlFPS/sliderFPS/Slider").GetComponent<Slider>();
		this.sldFPS.value = (float)(PlayerPrefs.GetInt("TargetFPS", 60) / 10);
		this.sldFPS.onValueChanged.AddListener(delegate(float A_1)
		{
			this.SetFPS(this.sldFPS.value);
		});
		this.txtFPS = base.transform.Find("pnlVideo/pnlFPS/txtFPS").GetComponent<TMP_Text>();
		this.txtFPS.text = PlayerPrefs.GetInt("TargetFPS", 60).ToString();
		this.sldAO = base.transform.Find("pnlVideo/pnlAO/sliderAO/Slider").GetComponent<Slider>();
		this.sldAO.value = (float)(PlayerPrefs.GetInt("AmbientOcclusion", 8) / 2);
		this.sldAO.onValueChanged.AddListener(delegate(float A_1)
		{
			this.SetAO(this.sldAO.value);
		});
		this.txtAO = base.transform.Find("pnlVideo/pnlAO/txtAO").GetComponent<TMP_Text>();
		this.txtAO.text = PlayerPrefs.GetInt("AmbientOcclusion", 8).ToString();
		if (this.txtAO.text == "0")
		{
			this.txtAO.text = "Off";
		}
		this.chkParallax = base.transform.Find("pnlVideo/pnlParallax/chkParallax").GetComponent<Toggle>();
		this.chkParallax.isOn = (PlayerPrefs.GetInt("Parallax", 1) == 1);
		this.chkParallax.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleParallax(this.chkParallax.isOn);
		});
		this.chkParallax.GetComponent<GUIAudioToggle>().requiresInit = false;
		this.chkLoS = base.transform.Find("pnlVideo/pnlLoS/chkLoS").GetComponent<Toggle>();
		this.chkLoS.isOn = (PlayerPrefs.GetInt("LineOfSight", 1) == 1);
		this.chkLoS.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleLoS(this.chkLoS.isOn);
		});
		this.chkLoS.GetComponent<GUIAudioToggle>().requiresInit = false;
		this.chkA = base.transform.Find("pnlVideo/pnlScreenShake/chkSS").GetComponent<Toggle>();
		this.chkA.isOn = (PlayerPrefs.GetFloat("ScreenShakeMod", 1f) == 1f);
		this.chkA.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ScreenShake(this.chkA.isOn);
		});
		this.chkA.GetComponent<GUIAudioToggle>().requiresInit = false;
		this.chkB = base.transform.Find("pnlVideo/pnlTurbo/chkB").GetComponent<Toggle>();
		this.chkB.isOn = (PlayerPrefs.GetInt("TurboB", 1) == 1);
		this.chkB.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.TurboB(this.chkB.isOn);
		});
		this.btnSaveVideo = base.transform.Find("pnlVideo/pnlOutputs/btnSave/btn").GetComponent<Button>();
		this.btnSaveVideo.onClick.AddListener(delegate()
		{
			this.SavePrefs();
		});
		this.txtPrefsVideo = base.transform.Find("pnlVideo/pnlOutputs/imgLCD/TextMeshPro - InputField").GetComponent<TMP_InputField>();
		this.txtPrefsVideo.text = PlayerPrefs.GetString("PrefsVideo");
		this.LoadPrefs();
		this.ddRes = base.transform.Find("pnlVideo/pnlResolution/ddRes").GetComponent<TMP_Dropdown>();
		List<string> list = new List<string>();
		this.dictRes = new Dictionary<string, Resolution>();
		int value = 0;
		this.ddRes.ClearOptions();
		foreach (Resolution value2 in Screen.resolutions)
		{
			string text = value2.width + "x" + value2.height;
			float num = (float)value2.width / (float)value2.height;
			if ((double)num >= 1.77 && (double)num <= 1.78)
			{
				text += "*";
			}
			if (list.IndexOf(text) < 0)
			{
				list.Add(text);
				this.dictRes[text] = value2;
				if (Screen.width == value2.width && Screen.height == value2.height)
				{
					value = list.Count - 1;
				}
			}
		}
		this.ddRes.AddOptions(list);
		this.ddRes.value = value;
		this.ddRes.onValueChanged.AddListener(delegate(int A_1)
		{
			this.SetRes();
		});
		this.cgVideo = base.transform.Find("pnlVideo").GetComponent<CanvasGroup>();
		this.btnAudio = base.transform.Find("pnlMenu/btnAudio/btn").GetComponent<Button>();
		this.btnAudio.onClick.AddListener(delegate()
		{
			this.State = this.cgAudio;
		});
		this.cgAudio = base.transform.Find("pnlAudio").GetComponent<CanvasGroup>();
		this.ledMaster = base.transform.Find("pnlAudio/pnlLedsMaster").GetComponent<GUILedMeter>();
		this.ledMaster.SetState(2);
		this.slidMaster = base.transform.Find("pnlAudio/SliderMaster").GetComponent<Slider>();
		this.VolMaster = this.VolMaster;
		this.slidMaster.value = this.VolMaster;
		this.slidMaster.onValueChanged.AddListener(delegate(float A_1)
		{
			this.VolMaster = this.slidMaster.value;
		});
		this.ledMusic = base.transform.Find("pnlAudio/pnlLedsMusic").GetComponent<GUILedMeter>();
		this.ledMusic.SetState(2);
		this.slidMusic = base.transform.Find("pnlAudio/SliderMusic").GetComponent<Slider>();
		this.VolMusic = this.VolMusic;
		this.slidMusic.value = this.VolMusic;
		this.slidMusic.onValueChanged.AddListener(delegate(float A_1)
		{
			this.VolMusic = this.slidMusic.value;
		});
		this.ledEffects = base.transform.Find("pnlAudio/pnlLedsEffects").GetComponent<GUILedMeter>();
		this.ledEffects.SetState(2);
		this.slidEffects = base.transform.Find("pnlAudio/SliderEffects").GetComponent<Slider>();
		this.VolEffects = this.VolEffects;
		this.slidEffects.value = this.VolEffects;
		this.slidEffects.onValueChanged.AddListener(delegate(float A_1)
		{
			this.VolEffects = this.slidEffects.value;
		});
		this.btnControls = base.transform.Find("pnlMenu/btnControls/btn").GetComponent<Button>();
		this.btnControls.onClick.AddListener(delegate()
		{
			this.State = this.cgControls;
		});
		this.cgControls = base.transform.Find("pnlControls").GetComponent<CanvasGroup>();
		this.btnInterface = base.transform.Find("pnlMenu/btnInterface/btn").GetComponent<Button>();
		this.btnInterface.onClick.AddListener(delegate()
		{
			this.State = this.cgInterface;
		});
		this.cgInterface = base.transform.Find("pnlInterface").GetComponent<CanvasGroup>();
		this.ddDateFormat = base.transform.Find("pnlInterface/ddDateFormat").GetComponent<TMP_Dropdown>();
		this.dictDateFormat = new Dictionary<string, string>();
		this.dictDateFormat.Add("ISO 8601: YYYY-MM-DD", MathUtils.DateFormat.YYYY_MM_DD.ToString());
		this.dictDateFormat.Add("DD-MM-YYYY", MathUtils.DateFormat.DD_MM_YYYY.ToString());
		this.dictDateFormat.Add("MM-DD-YYYY", MathUtils.DateFormat.MM_DD_YYYY.ToString());
		this.ddDateFormat.ClearOptions();
		this.ddDateFormat.AddOptions(new List<string>(this.dictDateFormat.Keys));
		foreach (TMP_Dropdown.OptionData optionData in this.ddDateFormat.options)
		{
			if (DataHandler.GetUserSettings().strDateFormat == this.dictDateFormat[optionData.text])
			{
				this.ddDateFormat.value = this.ddDateFormat.options.IndexOf(optionData);
				break;
			}
		}
		this.ddDateFormat.onValueChanged.AddListener(delegate(int A_1)
		{
			this.ChangeDateFormat();
		});
		this.ddTempUnits = base.transform.Find("pnlInterface/ddTempUnits").GetComponent<TMP_Dropdown>();
		this.dictTempUnits = new Dictionary<string, string>();
		this.dictTempUnits.Add("Kelvin", MathUtils.TemperatureUnit.K.ToString());
		this.dictTempUnits.Add("Celsius", MathUtils.TemperatureUnit.C.ToString());
		this.ddTempUnits.ClearOptions();
		this.ddTempUnits.AddOptions(new List<string>(this.dictTempUnits.Keys));
		foreach (TMP_Dropdown.OptionData optionData2 in this.ddTempUnits.options)
		{
			if (DataHandler.GetUserSettings().strTemperatureUnit == this.dictTempUnits[optionData2.text])
			{
				this.ddTempUnits.value = this.ddTempUnits.options.IndexOf(optionData2);
				break;
			}
		}
		this.ddTempUnits.onValueChanged.AddListener(delegate(int A_1)
		{
			this.ChangeTempUnits();
		});
		this.ddautoSaveInt = base.transform.Find("pnlInterface/ddAutoSaveInterval").GetComponent<TMP_Dropdown>();
		this.ddautoSaveInt.ClearOptions();
		this.ddautoSaveInt.AddOptions(new List<string>(this.dictAutoSaveInterval.Values));
		string a;
		if (this.dictAutoSaveInterval.TryGetValue(DataHandler.GetUserSettings().nAutosaveInterval, out a))
		{
			foreach (TMP_Dropdown.OptionData optionData3 in this.ddautoSaveInt.options)
			{
				if (a == optionData3.text)
				{
					this.ddautoSaveInt.value = this.ddautoSaveInt.options.IndexOf(optionData3);
					break;
				}
			}
		}
		this.ddautoSaveInt.onValueChanged.AddListener(new UnityAction<int>(this.ChangeAutoSaveInterval));
		this.ddautoSaveMaxCount = base.transform.Find("pnlInterface/ddautoSaveMaxCount").GetComponent<TMP_Dropdown>();
		this.ddautoSaveMaxCount.ClearOptions();
		this.ddautoSaveMaxCount.AddOptions(new List<string>(this.dictAutoSaveMaxCount.Values));
		string a2;
		if (this.dictAutoSaveMaxCount.TryGetValue(DataHandler.GetUserSettings().nAutosaveMaxCount, out a2))
		{
			foreach (TMP_Dropdown.OptionData optionData4 in this.ddautoSaveMaxCount.options)
			{
				if (a2 == optionData4.text)
				{
					this.ddautoSaveMaxCount.value = this.ddautoSaveMaxCount.options.IndexOf(optionData4);
					break;
				}
			}
		}
		this.ddautoSaveMaxCount.onValueChanged.AddListener(new UnityAction<int>(this.ChangeAutoSaveMaxCount));
		this.btnResetSettings = base.transform.Find("pnlInterface/btnResetSettings").GetComponent<Button>();
		this.btnResetSettings.onClick.AddListener(new UnityAction(this.ConfirmReset));
		this.btnFiles = base.transform.Find("pnlMenu/btnFiles/btn").GetComponent<Button>();
		this.btnFiles.onClick.AddListener(delegate()
		{
			this.State = this.cgFiles;
		});
		this.cgFiles = base.transform.Find("pnlFiles").GetComponent<CanvasGroup>();
		Button component = base.transform.Find("pnlFiles/btnMods/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.TryOpenModFolder();
		});
		base.transform.Find("pnlFiles/btnMods/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = DataHandler.strModFolder;
		component = base.transform.Find("pnlFiles/btnModsHelp").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL("https://docs.google.com/document/d/1MVRows7dK7nS8DsQqSSG_UxGJRlOFmp0wv7eEN7SLGk/edit?usp=sharing");
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		component = base.transform.Find("pnlFiles/btnScreenshots/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(Application.persistentDataPath + "/Screenshots");
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		base.transform.Find("pnlFiles/btnScreenshots/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = Application.persistentDataPath + "/Screenshots";
		component = base.transform.Find("pnlFiles/btnManuals/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(Application.streamingAssetsPath + "/images/manuals");
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		base.transform.Find("pnlFiles/btnManuals/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = Application.streamingAssetsPath + "/images/manuals";
		component = base.transform.Find("pnlFiles/btnSave1/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(MonoSingleton<LoadManager>.Instance.SavesPath);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		base.transform.Find("pnlFiles/btnSave1/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = MonoSingleton<LoadManager>.Instance.SavesPath;
		component = base.transform.Find("pnlFiles/btnSettings/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(Application.persistentDataPath);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		base.transform.Find("pnlFiles/btnSettings/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = Application.persistentDataPath;
		component = base.transform.Find("pnlFiles/btnAssets/btn").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(Application.streamingAssetsPath);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		base.transform.Find("pnlFiles/btnAssets/boxFilePath/txt").GetComponent<TextMeshProUGUI>().text = Application.streamingAssetsPath;
		this.aCGs = new List<CanvasGroup>
		{
			this.cgVideo,
			this.cgAudio,
			this.cgControls,
			this.cgInterface,
			this.cgFiles
		};
		this.tfList = base.transform.Find("pnlFiles/pnlList/Viewport/pnlListContent");
		JsonModList jsonModList = null;
		GUIModRow component2 = Resources.Load<GameObject>("prefabModRow").GetComponent<GUIModRow>();
		if (DataHandler.dictModList.TryGetValue("Mod Loading Order", out jsonModList))
		{
			foreach (string text2 in jsonModList.aLoadOrder)
			{
				GUIModRow guimodRow = UnityEngine.Object.Instantiate<GUIModRow>(component2, this.tfList);
				JsonModInfo jsonModInfo = null;
				if (text2 != null && text2 != string.Empty && DataHandler.dictModInfos.TryGetValue(text2, out jsonModInfo))
				{
					guimodRow.txtName.text = jsonModInfo.strName;
					guimodRow.Status = jsonModInfo.Status;
				}
				else
				{
					guimodRow.txtName.text = text2;
					guimodRow.Status = GUIModRow.ModStatus.Missing;
				}
			}
		}
		this._toggleBGRotation = base.transform.Find("pnlInterface/chkToggleBGRotation").GetComponent<ToggleSideSwitch>();
		if (this._toggleBGRotation.State != DataHandler.GetUserSettings().bDisableParallaxRotation)
		{
			this._toggleBGRotation.State = DataHandler.GetUserSettings().bDisableParallaxRotation;
		}
		this._toggleBGRotation.OnClick.AddListener(new UnityAction<bool>(this.ChangeParallaxRotation));
		this.ddFlickerAmount = base.transform.Find("pnlInterface/ddFlickerAmount").GetComponent<TMP_Dropdown>();
		this.ddFlickerAmount.ClearOptions();
		this.ddFlickerAmount.AddOptions(new List<string>(this.dictFlickerAmount.Values));
		string a3;
		if (this.dictFlickerAmount.TryGetValue(DataHandler.GetUserSettings().nFlickerAmount, out a3))
		{
			foreach (TMP_Dropdown.OptionData optionData5 in this.ddFlickerAmount.options)
			{
				if (a3 == optionData5.text)
				{
					this.ddFlickerAmount.value = this.ddFlickerAmount.options.IndexOf(optionData5);
					break;
				}
			}
		}
		this.ddFlickerAmount.onValueChanged.AddListener(new UnityAction<int>(this.ChangeFlickerAmount));
	}

	private void ConfirmReset()
	{
		if (this.goConfirm != null)
		{
			return;
		}
		this.goConfirm = UnityEngine.Object.Instantiate<GameObject>(this.ConfirmationDialoguePrefab, base.transform);
		Color clrBg = new Color(0.09411765f, 0.08627451f, 0.08627451f);
		Color clrFg = new Color(0.18039216f, 0.1764706f, 0.16078432f);
		Color clrFont = new Color(0.7058824f, 0.7058824f, 0.7058824f);
		this.goConfirm.GetComponent<GUIConfirmationDialogue>().Setup(DataHandler.GetString("GUI_CONFIRM_RESET", false), new Action(this.ResetSettings), clrBg, clrFg, clrFont);
	}

	private void ResetSettings()
	{
		DataHandler.ResetUserSettings();
		DataHandler.SaveUserSettings();
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
		GUIActionKeySelector componentInChildren = base.GetComponentInChildren<GUIActionKeySelector>();
		if (componentInChildren != null)
		{
			componentInChildren.Reset();
		}
		else
		{
			Debug.LogWarning("GuiActionKeySelector not found under Options");
		}
		this.Init();
	}

	private void ChangeParallaxRotation(bool disableRotation)
	{
		DataHandler.GetUserSettings().bDisableParallaxRotation = disableRotation;
		DataHandler.SaveUserSettings();
		ParallaxController.DisableParallaxRotation = disableRotation;
	}

	private void ChangeDateFormat()
	{
		DataHandler.GetUserSettings().strDateFormat = this.dictDateFormat[this.ddDateFormat.options[this.ddDateFormat.value].text];
		DataHandler.SaveUserSettings();
	}

	private void ChangeTempUnits()
	{
		DataHandler.GetUserSettings().strTemperatureUnit = this.dictTempUnits[this.ddTempUnits.options[this.ddTempUnits.value].text];
		MathUtils.ResetTemperatureString();
		DataHandler.SaveUserSettings();
	}

	private CanvasGroup State
	{
		get
		{
			return this.cgCurrent;
		}
		set
		{
			if (value == null || this.cgCurrent == value)
			{
				return;
			}
			foreach (CanvasGroup canvasGroup in this.aCGs)
			{
				if (canvasGroup == value)
				{
					CanvasManager.ShowCanvasGroup(canvasGroup);
				}
				else
				{
					CanvasManager.HideCanvasGroup(canvasGroup);
				}
			}
			this.cgCurrent = value;
		}
	}

	private void ChangeAutoSaveInterval(int option)
	{
		foreach (KeyValuePair<int, string> keyValuePair in this.dictAutoSaveInterval)
		{
			if (!(this.ddautoSaveInt.options[this.ddautoSaveInt.value].text != keyValuePair.Value))
			{
				DataHandler.GetUserSettings().nAutosaveInterval = keyValuePair.Key;
				DataHandler.SaveUserSettings();
				break;
			}
		}
	}

	private void ChangeAutoSaveMaxCount(int option)
	{
		foreach (KeyValuePair<int, string> keyValuePair in this.dictAutoSaveMaxCount)
		{
			if (!(this.ddautoSaveMaxCount.options[this.ddautoSaveMaxCount.value].text != keyValuePair.Value))
			{
				DataHandler.GetUserSettings().nAutosaveMaxCount = keyValuePair.Key;
				DataHandler.SaveUserSettings();
				break;
			}
		}
	}

	private void ChangeFlickerAmount(int option)
	{
		foreach (KeyValuePair<int, string> keyValuePair in this.dictFlickerAmount)
		{
			if (!(this.ddFlickerAmount.options[this.ddFlickerAmount.value].text != keyValuePair.Value))
			{
				DataHandler.GetUserSettings().nFlickerAmount = keyValuePair.Key;
				DataHandler.SaveUserSettings();
				Visibility.UpdateBaseFlickerAmount();
				Powered.UpdateBaseFlickerAmount();
				break;
			}
		}
	}

	private void TryOpenModFolder()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(DataHandler.strModFolder);
			Directory.CreateDirectory(directoryName);
			string path = Path.Combine(directoryName, "loading_order.json");
			if (!File.Exists(path))
			{
				JsonModList jsonModList = new JsonModList();
				jsonModList.strName = "Mod Loading Order";
				jsonModList.aLoadOrder = new string[]
				{
					"core"
				};
				jsonModList.aIgnorePatterns = new string[0];
				Dictionary<string, JsonModList> dictionary = new Dictionary<string, JsonModList>();
				dictionary[jsonModList.strName] = jsonModList;
				string contents = DataHandler.CreateJsonFromData<JsonModList>(dictionary);
				File.WriteAllText(path, contents);
			}
			Application.OpenURL(directoryName);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
		}
	}

	private void SetRes()
	{
		string text = this.ddRes.options[this.ddRes.value].text;
		Resolution resolution = this.dictRes[text];
		Screen.SetResolution(resolution.width, resolution.height, this.chkScreen.isOn);
	}

	private float VolMaster
	{
		get
		{
			float result = 0.678f;
			if (PlayerPrefs.HasKey("fVolMaster"))
			{
				result = PlayerPrefs.GetFloat("fVolMaster");
			}
			return result;
		}
		set
		{
			float num = Mathf.Clamp(value, 0.001f, 1f);
			AudioManager.am.SetMixerFloat("VolMaster", Mathf.Log(num) * 20f);
			this.ledMaster.SetValue(num);
			PlayerPrefs.SetFloat("fVolMaster", num);
		}
	}

	private float VolMusic
	{
		get
		{
			float result = 1f;
			if (PlayerPrefs.HasKey("fVolMusic"))
			{
				result = PlayerPrefs.GetFloat("fVolMusic");
			}
			return result;
		}
		set
		{
			float num = Mathf.Clamp(value, 0.001f, 1f);
			AudioManager.am.SetMixerFloat("VolMusic", Mathf.Log(num) * 20f);
			this.ledMusic.SetValue(num);
			PlayerPrefs.SetFloat("fVolMusic", num);
		}
	}

	private float VolEffects
	{
		get
		{
			float result = 1f;
			if (PlayerPrefs.HasKey("fVolEffects"))
			{
				result = PlayerPrefs.GetFloat("fVolEffects");
			}
			return result;
		}
		set
		{
			float num = Mathf.Clamp(value, 0.001f, 1f);
			AudioManager.am.SetMixerFloat("VolEffects", Mathf.Log(num) * 20f);
			this.ledEffects.SetValue(num);
			PlayerPrefs.SetFloat("fVolEffects", num);
		}
	}

	private void KeyHandler()
	{
		if (GUIActionKeySelector.commandEscape.Down && SceneManager.GetActiveScene().name == "MainMenu2" && this.cgOptions != null && this.cgOptions.alpha == 1f)
		{
			this.Exit();
		}
	}

	public void Exit()
	{
		this.cgOptions.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, false, true);
		foreach (CanvasGroup canvasGroup in this.aCGs)
		{
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}
		this.cgOptions.interactable = false;
		this.cgOptions.blocksRaycasts = false;
		this.cgCurrent = null;
	}

	public void LoadPrefs()
	{
		this.txtPrefsVideo.text = PlayerPrefs.GetString("PrefsVideo");
		string[] array = this.txtPrefsVideo.text.Split(new char[]
		{
			'\n'
		});
		this.txtPrefsVideo.text = string.Empty;
		foreach (string text in array)
		{
			if (!(text.Trim() == string.Empty))
			{
				string[] array3 = text.Split(new char[]
				{
					':'
				});
				TMP_InputField tmp_InputField = this.txtPrefsVideo;
				string text2 = tmp_InputField.text;
				tmp_InputField.text = string.Concat(new string[]
				{
					text2,
					array3[0].Trim(),
					":",
					PlayerPrefs.GetString(array3[0].Trim()),
					"\n"
				});
			}
		}
	}

	public void SavePrefs()
	{
		ConsoleToGUI.instance.LogInfo("Saved Prefs!");
		PlayerPrefs.SetString("PrefsVideo", this.txtPrefsVideo.text);
		string[] array = this.txtPrefsVideo.text.Split(new char[]
		{
			'\n'
		});
		foreach (string text in array)
		{
			if (!(text.Trim() == string.Empty))
			{
				string[] array3 = text.Split(new char[]
				{
					':'
				});
				PlayerPrefs.SetString(array3[0].Trim(), array3[1].Trim());
			}
		}
		PlayerPrefs.Save();
	}

	public void SetFPS(float fps)
	{
		int num = (int)fps * 10;
		if (fps == 16f)
		{
			this.txtFPS.text = "1000";
			PlayerPrefs.SetInt("TargetFPS", 1000);
			Application.targetFrameRate = 1000;
			return;
		}
		PlayerPrefs.SetInt("TargetFPS", num);
		Application.targetFrameRate = num;
		this.txtFPS.text = num.ToString() + "fps";
	}

	public void SetAO(float ao)
	{
		int num = Mathf.Clamp((int)ao * 2, 0, 8);
		PlayerPrefs.SetInt("AmbientOcclusion", num);
		this.txtAO.text = num.ToString();
		if (ao == 0f)
		{
			this.txtAO.text = "Off";
		}
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.camMain.GetComponent<GameRenderer>().AOLoops = num;
		}
	}

	public void ToggleParallax(bool toggle)
	{
		PlayerPrefs.SetInt("Parallax", (!toggle) ? 0 : 1);
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.camMain.GetComponent<GameRenderer>().ShowParallax = toggle;
		}
	}

	public void ToggleLoS(bool toggle)
	{
		PlayerPrefs.SetInt("LineOfSight", (!toggle) ? 0 : 1);
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.camMain.GetComponent<GameRenderer>().HideLoS = !toggle;
		}
	}

	public void ScreenShake(bool toggle)
	{
		PlayerPrefs.SetFloat("ScreenShakeMod", (float)((!toggle) ? 0 : 1));
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.fShakeUserPref = (float)((!toggle) ? 0 : 1);
		}
	}

	public void TurboB(bool toggle)
	{
		PlayerPrefs.SetInt("TurboB", (!toggle) ? 0 : 1);
		if (toggle)
		{
			TextMeshProUGUI[] componentsInChildren = this.chkB.GetComponentsInChildren<TextMeshProUGUI>();
			string text = string.Empty;
			switch (UnityEngine.Random.Range(0, 6))
			{
			case 0:
				text = "HYPER";
				break;
			case 1:
				text = "PLUS";
				break;
			case 2:
				text = "MEGA";
				break;
			case 3:
				text = "SUPER";
				break;
			case 4:
				text = "FAST";
				break;
			case 5:
				text = "YES";
				break;
			}
			foreach (TextMeshProUGUI textMeshProUGUI in componentsInChildren)
			{
				textMeshProUGUI.text = text;
			}
		}
	}

	[SerializeField]
	protected GameObject ConfirmationDialoguePrefab;

	private CanvasGroup cgOptions;

	private CanvasGroup cgCurrent;

	private Button btnQuit;

	private Button btnVideo;

	private CanvasGroup cgVideo;

	private TMP_Dropdown ddRes;

	private Toggle chkScreen;

	private Dictionary<string, Resolution> dictRes;

	private Toggle chkParallax;

	private Toggle chkLoS;

	private Slider sldFPS;

	private TMP_Text txtFPS;

	private Slider sldAO;

	private TMP_Text txtAO;

	private Toggle chkA;

	private Toggle chkB;

	private TMP_InputField txtPrefsVideo;

	private Button btnSaveVideo;

	private Button btnAudio;

	private CanvasGroup cgAudio;

	private Slider slidMaster;

	private Slider slidMusic;

	private Slider slidEffects;

	private GUILedMeter ledMaster;

	private GUILedMeter ledMusic;

	private GUILedMeter ledEffects;

	private Transform tfList;

	private Button btnControls;

	private CanvasGroup cgControls;

	private Button btnInterface;

	private CanvasGroup cgInterface;

	private TMP_Dropdown ddDateFormat;

	private Dictionary<string, string> dictDateFormat;

	private TMP_Dropdown ddTempUnits;

	private Dictionary<string, string> dictTempUnits;

	private TMP_Dropdown ddautoSaveInt;

	private Dictionary<int, string> dictAutoSaveInterval;

	private TMP_Dropdown ddautoSaveMaxCount;

	private Dictionary<int, string> dictAutoSaveMaxCount;

	private Button btnFiles;

	private CanvasGroup cgFiles;

	private List<CanvasGroup> aCGs;

	private ToggleSideSwitch _toggleBGRotation;

	private TMP_Dropdown ddFlickerAmount;

	private Dictionary<int, string> dictFlickerAmount;

	private Button btnResetSettings;

	private GameObject goConfirm;
}
