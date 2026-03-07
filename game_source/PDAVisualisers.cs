using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PDA visualizer/overlay controller.
// This manages ship visualization presets such as damage or power overlays
// and pushes those render settings out to the active visualization systems.
public class PDAVisualisers : MonoBehaviour
{
	// Initializes default presets and the saved preset dictionary.
	private void Awake()
	{
		this._gradientUI.material.SetFloat("_OverlayMode", (float)this._Gradient);
		this._gradientUI.material.SetFloat("_OverlayBlend", this._fOpacity);
		this._currentPreset = RenderPresets.Default;
		this._renderPresets.Clear();
		this._renderPresets = new List<RenderPresets>
		{
			RenderPresets.Default
		};
		this._bAssembledUIFirstTime = false;
		this._savedPresets = new Dictionary<string, VizorPreset>();
		this.MakeDefaultPresets();
	}

	// Polls for reset requests and throttled ship update notifications.
	private void Update()
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null && selectedCrew.HasCond("IsPDAVizReset"))
		{
			this.PresetSimple(RenderPresets.Default.ToString());
			this.AssembleUI();
			selectedCrew.ZeroCondAmount("IsPDAVizReset");
		}
		this._pingTime += Time.deltaTime;
		if (this.bNotifyShips || (this._togglePingUpdates && this._pingTime > 0.5f))
		{
			this._pingTime = 0f;
			this.NotifyShips();
		}
	}

	// Applies a saved preset by name, falling back to Default when the same preset is toggled again.
	public void PresetSimple(string name)
	{
		if (string.IsNullOrEmpty(name) || !this._savedPresets.ContainsKey(name))
		{
			Debug.Log("Preset name not found in list: " + name);
			return;
		}
		VizorPreset vizorPreset = this._savedPresets[name];
		if (this._currentPreset == vizorPreset._preset && !this._bResolvingCustomInfos)
		{
			vizorPreset = this._savedPresets["Default"];
		}
		this._currentPreset = vizorPreset._preset;
		this.OverlayVariable = vizorPreset._variable;
		this.Max = vizorPreset._max;
		this.Min = vizorPreset._min;
		this.Opacity = vizorPreset._opacity;
		this.Gradient = vizorPreset._rom;
		this._toggleFOV.isOn = vizorPreset._fov;
		this._toggleLights.isOn = vizorPreset._lights;
		this._toggleExteriors.isOn = vizorPreset._exteriors;
		this._toggleCeiling.isOn = vizorPreset._ceiling;
		this._toggleAO.isOn = vizorPreset._ao;
		this._toggleLogScale.isOn = vizorPreset._log;
		this._togglePlaceholders.isOn = vizorPreset._placeholders;
		this._toggleTasks.isOn = vizorPreset._tasks;
		this._togglePower.isOn = vizorPreset._power;
		this._toggleUpdates.isOn = vizorPreset._updates;
		this.SetPresetToggles(vizorPreset._preset);
		this.bNotifyShips = true;
		this.PresetDamageExtern(vizorPreset._preset == RenderPresets.Damage);
	}

	// Restores the neutral/default visualizer settings.
	public void PresetDefault(bool toggle)
	{
		if (!toggle)
		{
			return;
		}
		Debug.Log("Changing preset to default");
		this._currentPreset = RenderPresets.Default;
		this.OverlayVariable = "_None";
		this.Max = 1f;
		this.Min = 0f;
		this.Opacity = 1f;
		this.Gradient = RenderOverlayMode.Default;
		this._toggleLights.isOn = true;
		this._toggleExteriors.isOn = true;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = true;
		this._toggleTasks.isOn = true;
		this._toggleUpdates.isOn = false;
		this._toggleLogScale.isOn = false;
		this.SetPresetToggles(this._presetDefault);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	// Enables the power overlay preset used to visualize powered systems.
	public void PresetPower(bool toggle)
	{
		if (!toggle)
		{
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to power");
		this._currentPreset = RenderPresets.Power;
		this.OverlayVariable = "_Power";
		this.Max = 1f;
		this.Min = 0f;
		this.Opacity = 0.95f;
		this.Gradient = RenderOverlayMode.Golden;
		this._toggleLights.isOn = true;
		this._toggleExteriors.isOn = true;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = true;
		this._togglePlaceholders.isOn = true;
		this._toggleTasks.isOn = true;
		this._toggleUpdates.isOn = true;
		this._toggleLogScale.isOn = true;
		this.SetPresetToggles(this._presetPower);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	// Enables the damage overlay preset, likely used by repair workflows and tutorials.
	public void PresetDamage(bool toggle)
	{
		if (!toggle)
		{
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialDmgVizOffWaiting"))
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialDmgVizOffWaiting");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to damage");
		this._currentPreset = RenderPresets.Damage;
		this.OverlayVariable = "_Damage";
		this.Max = 1f;
		this.Min = 0f;
		this.Opacity = 1f;
		this.Gradient = RenderOverlayMode.Oldred;
		this._toggleLights.isOn = false;
		this._toggleExteriors.isOn = false;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = false;
		this._toggleTasks.isOn = false;
		this._toggleUpdates.isOn = false;
		this._toggleLogScale.isOn = false;
		this.SetPresetToggles(this._presetDamage);
		this.bNotifyShips = true;
		this.PresetDamageExtern(true);
	}

	// Synchronizes external UI pieces with the damage-visualizer state.
	private void PresetDamageExtern(bool bOn)
	{
		MonoSingleton<GUIItemList>.Instance.ToggleDmg(bOn);
		if (bOn)
		{
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialDmgVizWaiting"))
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialDmgVizWaiting");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
		}
		else if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialDmgVizOffWaiting"))
		{
			CrewSim.coPlayer.ZeroCondAmount("TutorialDmgVizOffWaiting");
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		}
	}

	public void PresetMass(bool toggle)
	{
		if (!toggle)
		{
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to mass");
		this._currentPreset = RenderPresets.Mass;
		this.OverlayVariable = "_Mass";
		this.Max = 1000f;
		this.Min = 0f;
		this.Opacity = 0.5f;
		this.Gradient = RenderOverlayMode.Rainbow;
		this._toggleLights.isOn = false;
		this._toggleExteriors.isOn = false;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = false;
		this._toggleTasks.isOn = false;
		this._toggleLogScale.isOn = true;
		this._toggleUpdates.isOn = false;
		this.SetPresetToggles(this._presetMass);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	public void PresetPrice(bool toggle)
	{
		if (!toggle)
		{
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to price");
		this._currentPreset = RenderPresets.Price;
		this.OverlayVariable = "_Price";
		this.Max = 10000f;
		this.Min = 0f;
		this.Opacity = 0.5f;
		this.Gradient = RenderOverlayMode.InverseRainbow;
		this._toggleLights.isOn = false;
		this._toggleExteriors.isOn = false;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = false;
		this._toggleTasks.isOn = false;
		this._toggleLogScale.isOn = true;
		this._toggleUpdates.isOn = false;
		this.SetPresetToggles(this._presetPrice);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	public void PresetHeat(bool toggle)
	{
		if (!toggle)
		{
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to heat");
		this._currentPreset = RenderPresets.Heat;
		this.OverlayVariable = "_Heat";
		this.Max = 673f;
		this.Min = 293f;
		this.Opacity = 0.95f;
		this.Gradient = RenderOverlayMode.HeatMap;
		this._toggleLights.isOn = false;
		this._toggleExteriors.isOn = false;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = false;
		this._toggleTasks.isOn = false;
		this._toggleLogScale.isOn = true;
		this._toggleAO.isOn = true;
		this._toggleUpdates.isOn = true;
		this.SetPresetToggles(this._presetHeat);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	public void PresetPressure(bool toggle)
	{
		if (!toggle)
		{
			this.PresetDefault(true);
			return;
		}
		Debug.Log("Changing preset to heat");
		this._currentPreset = RenderPresets.Pressure;
		this.OverlayVariable = "_Pressure";
		this.Max = 2000f;
		this.Min = 0f;
		this.Opacity = 0.95f;
		this.Gradient = RenderOverlayMode.Tricolor;
		this._toggleLights.isOn = false;
		this._toggleExteriors.isOn = false;
		this._toggleCeiling.isOn = true;
		this._togglePower.isOn = false;
		this._togglePlaceholders.isOn = false;
		this._toggleTasks.isOn = false;
		this._toggleLogScale.isOn = true;
		this._toggleUpdates.isOn = true;
		this.SetPresetToggles(this._presetPressure);
		this.bNotifyShips = true;
		this.PresetDamageExtern(false);
	}

	public void ToggleFOV(bool toggle)
	{
		PhotoMode.SetFog(toggle);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._fov = toggle;
	}

	public void TogglePingUpdates(bool toggle)
	{
		this._togglePingUpdates = toggle;
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._updates = toggle;
	}

	public void ToggleLights(bool value)
	{
		PhotoMode.SetLights(value);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._lights = value;
	}

	public void ToggleExteriors(bool value)
	{
		PhotoMode.SetParallax(value);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._exteriors = value;
	}

	public void ToggleCeiling(bool value)
	{
		List<Ship> allLoadedShips = CrewSim.GetAllLoadedShips();
		foreach (Ship ship in allLoadedShips)
		{
			List<CondOwner> cos = ship.GetCOs(DataHandler.GetCondTrigger("TIsCeiling"), true, true, true);
			foreach (CondOwner condOwner in cos)
			{
				if (value)
				{
					condOwner.gameObject.layer = LayerMask.NameToLayer("Default");
				}
				else
				{
					condOwner.gameObject.layer = LayerMask.NameToLayer("Ship Offscreen");
				}
			}
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._ceiling = value;
	}

	public void TogglePower(bool value)
	{
		Debug.Log("Setting power visualiser to " + value.ToString());
		if (value)
		{
			if (!CrewSim.PowerVizVisible)
			{
				CrewSim.objInstance.TogglePowerUI(CrewSim.shipCurrentLoaded, null);
			}
		}
		else if (CrewSim.PowerVizVisible)
		{
			CrewSim.objInstance.TogglePowerUI(CrewSim.shipCurrentLoaded, null);
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._power = value;
	}

	public void TogglePowerQuietly(bool value)
	{
		CrewSim.SetToggleWithoutNotify(this._togglePower, value);
		this._savedPresets[this._currentPreset.ToString()]._power = value;
	}

	public void TogglePlaceholders(bool value)
	{
		if (value)
		{
			CrewSim.objInstance.camMain.cullingMask |= 1 << LayerMask.NameToLayer("Placeholder");
		}
		else
		{
			CrewSim.objInstance.camMain.cullingMask &= ~(1 << LayerMask.NameToLayer("Placeholder"));
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._placeholders = value;
	}

	public void ToggleTasks(bool value)
	{
		if (value)
		{
			CrewSim.objInstance.camMain.cullingMask |= 1 << LayerMask.NameToLayer("Task");
		}
		else
		{
			CrewSim.objInstance.camMain.cullingMask &= ~(1 << LayerMask.NameToLayer("Task"));
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this._savedPresets[this._currentPreset.ToString()]._tasks = value;
	}

	public void ToggleLogScale(bool value)
	{
		this._useLogScale = value;
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
		this._savedPresets[this._currentPreset.ToString()]._log = value;
	}

	public void ToggleAO(bool value)
	{
		PhotoMode.SetAO(value);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
		this._savedPresets[this._currentPreset.ToString()]._ao = value;
	}

	public string OverlayVariable
	{
		get
		{
			return this._strVariable;
		}
		set
		{
			this._strVariable = value;
			if (this._strVariable == null)
			{
				this._strVariable = string.Empty;
			}
			this._txtOverlayVariable.text = this._strVariable;
			if (this._strVariable.Length > 0 && this._strVariable[0] != '_')
			{
				if (DataHandler.GetCond(this._strVariable) == null)
				{
					this._txtOverlayVariableCol.color = this.m_colOff;
				}
				else
				{
					this._txtOverlayVariableCol.color = this.m_colEnabled;
				}
			}
			else
			{
				this._txtOverlayVariableCol.color = Color.white;
			}
			this._savedPresets[this._currentPreset.ToString()]._variable = this._strVariable;
			CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
			this.bNotifyShips = true;
		}
	}

	public void UpVariable()
	{
		if (this._variables.Contains(this._strVariable))
		{
			int num = this._variables.IndexOf(this._strVariable) + 1;
			if (num >= this._variables.Count)
			{
				this.OverlayVariable = this._variables[0];
			}
			else
			{
				this.OverlayVariable = this._variables[num];
			}
		}
		else
		{
			this.OverlayVariable = this._variables[0];
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
	}

	public void DownVariable()
	{
		if (this._variables.Contains(this._strVariable))
		{
			int num = this._variables.IndexOf(this._strVariable) - 1;
			if (num < 0)
			{
				num = this._variables.Count - 1;
			}
			this.OverlayVariable = this._variables[num];
		}
		else
		{
			this.OverlayVariable = this._variables[0];
		}
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
	}

	public float Min
	{
		get
		{
			return this._fMin;
		}
		set
		{
			this._fMin = value;
			this._txtOverlayMin.text = this._fMin.ToString();
			CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
			this._savedPresets[this._currentPreset.ToString()]._min = this._fMin;
		}
	}

	public void SetMin(string strParse)
	{
		float fMin = 0f;
		if (strParse != null && strParse.Length > 0)
		{
			try
			{
				fMin = float.Parse(strParse);
			}
			catch
			{
				Debug.LogWarning("failed to recognise inputted number, changing to 0");
			}
		}
		this._fMin = fMin;
		this._txtOverlayMin.text = this._fMin.ToString();
		this._savedPresets[this._currentPreset.ToString()]._min = this._fMin;
		this.bNotifyShips = true;
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
	}

	public float Max
	{
		get
		{
			return this._fMax;
		}
		set
		{
			this._fMax = value;
			this._txtOverlayMax.text = this._fMax.ToString();
			CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
			this._savedPresets[this._currentPreset.ToString()]._max = this._fMax;
		}
	}

	public void SetMax(string strParse)
	{
		float fMax = 0f;
		if (strParse != null && strParse.Length > 0)
		{
			try
			{
				fMax = float.Parse(strParse);
			}
			catch
			{
				Debug.LogWarning("failed to recognise inputted number, changing to 0");
			}
		}
		this._fMax = fMax;
		this._txtOverlayMax.text = this._fMax.ToString();
		this._savedPresets[this._currentPreset.ToString()]._max = this._fMax;
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
	}

	public RenderOverlayMode Gradient
	{
		get
		{
			return this._Gradient;
		}
		set
		{
			this._Gradient = value;
			this._txtOverlayGradient.text = this._Gradient.ToString();
			this._gradientUI.material.SetFloat("_OverlayMode", (float)this._Gradient);
			this._gradientUI.material.SetFloat("_OverlayBlend", this._fOpacity);
			CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
			this._savedPresets[this._currentPreset.ToString()]._rom = this._Gradient;
		}
	}

	public void UpGradient()
	{
		for (int i = 0; i <= 12; i++)
		{
			this.Gradient++;
			if (this.Gradient >= RenderOverlayMode.MaxVal)
			{
				this.Gradient = RenderOverlayMode.Default;
			}
			if (this._allowedModes.Contains(this.Gradient))
			{
				this.bNotifyShips = true;
				AudioManager.am.PlayAudioEmitter("PDAScannerSound", false, false);
				CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
				return;
			}
		}
		AudioManager.am.PlayAudioEmitter("PDAScannerSound", false, false);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
	}

	public void DownGradient()
	{
		for (int i = 0; i <= 12; i++)
		{
			this.Gradient--;
			if (this.Gradient < RenderOverlayMode.Default)
			{
				this.Gradient = RenderOverlayMode.Disco;
			}
			if (this._allowedModes.Contains(this.Gradient))
			{
				this.bNotifyShips = true;
				return;
			}
		}
		AudioManager.am.PlayAudioEmitter("PDAScannerSound", false, false);
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
	}

	public float Opacity
	{
		get
		{
			return this._fOpacity;
		}
		set
		{
			this._fOpacity = value;
			this._OverlayOpacity.value = this._fOpacity;
			CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
			this._savedPresets[this._currentPreset.ToString()]._opacity = this._fOpacity;
			this.bNotifyShips = true;
		}
	}

	public void SetOpacity(float opacity)
	{
		this._fOpacity = opacity;
		CrewSim.SetToggleWithoutNotify(this._presetDefault, false);
		this.bNotifyShips = true;
	}

	private void SetPresetToggles(Toggle toggleIn)
	{
		List<Toggle> list = new List<Toggle>
		{
			this._presetDamage,
			this._presetMass,
			this._presetPower,
			this._presetPrice,
			this._presetHeat,
			this._presetPressure,
			this._presetDefault
		};
		bool flag = false;
		string strJAE = "PDAScannerSound";
		if (toggleIn == this._presetDefault)
		{
			strJAE = "PDAScannerSoundOff";
		}
		foreach (Toggle toggle in list)
		{
			CrewSim.SetToggleWithoutNotify(toggle, toggle == toggleIn);
			if (toggle.isOn)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			CrewSim.SetToggleWithoutNotify(this._presetDefault, true);
		}
		if (CrewSim.objInstance.FinishedLoading)
		{
			AudioManager.am.TweakAudioEmitter(strJAE, MathUtils.Rand(0.9f, 1.15f, MathUtils.RandType.Flat, null), 1f);
			AudioManager.am.PlayAudioEmitter(strJAE, false, false);
		}
	}

	private void SetPresetToggles(RenderPresets preset)
	{
		Toggle y;
		switch (preset)
		{
		case RenderPresets.Default:
			y = this._presetDefault;
			break;
		case RenderPresets.Power:
			y = this._presetPower;
			break;
		case RenderPresets.Damage:
			y = this._presetDamage;
			break;
		case RenderPresets.Mass:
			y = this._presetMass;
			break;
		case RenderPresets.Price:
			y = this._presetPrice;
			break;
		case RenderPresets.Heat:
			y = this._presetHeat;
			break;
		case RenderPresets.Pressure:
			y = this._presetPressure;
			break;
		default:
			y = this._presetDefault;
			break;
		}
		List<Toggle> list = new List<Toggle>
		{
			this._presetDamage,
			this._presetMass,
			this._presetPower,
			this._presetPrice,
			this._presetHeat,
			this._presetPressure,
			this._presetDefault
		};
		bool flag = false;
		string strJAE = "PDAScannerSound";
		if (preset == RenderPresets.Default)
		{
			strJAE = "PDAScannerSoundOff";
		}
		foreach (Toggle toggle in list)
		{
			CrewSim.SetToggleWithoutNotify(toggle, toggle == y);
			if (toggle.isOn)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			CrewSim.SetToggleWithoutNotify(this._presetDefault, true);
		}
		if (CrewSim.objInstance.FinishedLoading)
		{
			AudioManager.am.TweakAudioEmitter(strJAE, MathUtils.Rand(0.9f, 1.15f, MathUtils.RandType.Flat, null), 1f);
			AudioManager.am.PlayAudioEmitter(strJAE, false, false);
		}
	}

	private void NotifyShips()
	{
		this._gradientUI.material.SetFloat("_OverlayMode", (float)this._Gradient);
		this._gradientUI.material.SetFloat("_OverlayBlend", this._fOpacity);
		List<Ship> allLoadedShips = CrewSim.GetAllLoadedShips();
		foreach (Ship ship in allLoadedShips)
		{
			ship.VisualizeOverlays(true);
		}
		this.bNotifyShips = false;
	}

	public float InverseLerp(float value)
	{
		float num = value - this._fMin;
		num /= this._fMax - this._fMin;
		num = Mathf.Clamp01(num);
		if (this._useLogScale)
		{
			num = 1f + Mathf.Log10(num) / 2.5f;
			num = Mathf.Clamp01(num);
		}
		return num;
	}

	public void ResolveCustomInfo(string customInfos)
	{
		if (string.IsNullOrEmpty(customInfos))
		{
			this.PresetSimple(RenderPresets.Default.ToString());
			return;
		}
		Debug.Log("Resolving Custom Infos for PDA Viz");
		string[] array = customInfos.Split(new char[]
		{
			'|'
		});
		this._bResolvingCustomInfos = true;
		bool flag = false;
		int num = 0;
		foreach (string text in array)
		{
			Debug.Log("Parsing: " + text);
			string[] array3 = text.Split(new char[]
			{
				':'
			});
			if (array3.Length >= 2)
			{
				string text2 = array3[0];
				if (text2 != null)
				{
					if (text2 == "version")
					{
						num = int.Parse(array3[1]);
						if (num == this._version)
						{
							flag = true;
						}
					}
				}
			}
		}
		if (!flag)
		{
			if (num == 0)
			{
				if (CrewSim.GetSelectedCrew() != null)
				{
					CrewSim.GetSelectedCrew().LogMessage("PDA Firmware update, resetting preset to default!", "Badish", CrewSim.GetSelectedCrew().strName);
				}
				this.PresetSimple(RenderPresets.Default.ToString());
				return;
			}
		}
		foreach (string text3 in array)
		{
			Debug.Log("Parsing: " + text3);
			string[] array5 = text3.Split(new char[]
			{
				':'
			});
			if (array5.Length >= 2)
			{
				string text4 = array5[0];
				if (text4 != null)
				{
					if (text4 == "preset")
					{
						this.PresetSimple(array5[1]);
						return;
					}
				}
			}
		}
		this._bResolvingCustomInfos = false;
		foreach (string text5 in array)
		{
			Debug.Log("Parsing: " + text5);
			string[] array7 = text5.Split(new char[]
			{
				':'
			});
			if (array7.Length >= 2)
			{
				string text6 = array7[0];
				switch (text6)
				{
				case "toggleFOV":
					this.ToggleFOV(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleFOV, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleLights":
					this.ToggleLights(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleLights, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleExteriors":
					this.ToggleExteriors(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleExteriors, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleCeiling":
					this.ToggleCeiling(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleCeiling, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "togglePower":
					this.TogglePower(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._togglePower, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "togglePlaceholders":
					this.TogglePlaceholders(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._togglePlaceholders, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleTasks":
					this.ToggleTasks(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleTasks, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleLogScale":
					this.ToggleLogScale(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleLogScale, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleAO":
					this.ToggleAO(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleAO, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "toggleUpdates":
					this.TogglePingUpdates(1 == int.Parse(array7[1]));
					CrewSim.SetToggleWithoutNotify(this._toggleUpdates, 1 == int.Parse(array7[1]));
					goto IL_CED;
				case "OverlayVariable":
					this.OverlayVariable = array7[1];
					goto IL_CED;
				case "Min":
					this.Min = float.Parse(array7[1]);
					goto IL_CED;
				case "Max":
					this.Max = float.Parse(array7[1]);
					goto IL_CED;
				case "Gradient":
					this.Gradient = (RenderOverlayMode)int.Parse(array7[1]);
					goto IL_CED;
				case "Opacity":
					this.Opacity = float.Parse(array7[1]);
					goto IL_CED;
				case "showPresetPower":
					this._presetPower.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetPower.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showPresetDamage":
					this._presetDamage.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetDamage.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showPresetPrice":
					this._presetPrice.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetPrice.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showPresetMass":
					this._presetMass.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetMass.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showPresetHeat":
					this._presetHeat.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetHeat.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showPresetPressure":
					this._presetPressure.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._presetPressure.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleFOV":
					this._toggleFOV.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleFOV.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleLights":
					this._toggleLights.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleLights.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleExteriors":
					this._toggleExteriors.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleExteriors.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleCeiling":
					this._toggleCeiling.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleCeiling.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showTogglePower":
					this._togglePower.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._togglePower.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showTogglePlaceholders":
					this._togglePlaceholders.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._togglePlaceholders.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleTasks":
					this._toggleTasks.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleTasks.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleLogScale":
					this._toggleLogScale.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleLogScale.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleAO":
					this._toggleAO.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleAO.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showToggleUpdates":
					this._toggleUpdates.GetComponent<CanvasGroup>().alpha = (float)int.Parse(array7[1]);
					this._toggleUpdates.GetComponent<CanvasGroup>().interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showValueInput":
					this._pnlValue.alpha = (float)int.Parse(array7[1]);
					this._pnlValue.interactable = (1 >= int.Parse(array7[1]));
					this._valueInput.interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showGradientInput":
					this._pnlGradient.alpha = (float)int.Parse(array7[1]);
					this._pnlGradient.interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showMinMaxInput":
					this._pnlMinMax.alpha = (float)int.Parse(array7[1]);
					this._pnlMinMax.interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "showOpacityInput":
					this._pnlOpacity.alpha = (float)int.Parse(array7[1]);
					this._pnlOpacity.interactable = (1 >= int.Parse(array7[1]));
					goto IL_CED;
				case "addAllowedVariable":
					this._variables.Add(array7[1]);
					goto IL_CED;
				case "removeAllowedVariable":
					this._variables.Remove(array7[1]);
					goto IL_CED;
				case "addAllowedRenderMode":
					this._allowedModes.Add((RenderOverlayMode)int.Parse(array7[1]));
					goto IL_CED;
				case "removeAllowedRenderMode":
					this._allowedModes.Remove((RenderOverlayMode)int.Parse(array7[1]));
					goto IL_CED;
				case "preset":
				case "presetDefault":
				case "presetDamage":
				case "presetPower":
				case "presetMass":
				case "presetHeat":
				case "presetPressure":
				case "presetPrice":
					goto IL_CED;
				}
				Debug.LogWarning("PDA Visualiser setting not recognised! " + array7[0]);
			}
			IL_CED:;
		}
	}

	public void EnableEverything()
	{
		this._allowedModes.Clear();
		this._allowedModes = new List<RenderOverlayMode>
		{
			RenderOverlayMode.Default,
			RenderOverlayMode.Clean,
			RenderOverlayMode.Monochrome,
			RenderOverlayMode.Tricolor,
			RenderOverlayMode.Rainbow,
			RenderOverlayMode.Oldred,
			RenderOverlayMode.Opacity,
			RenderOverlayMode.Golden,
			RenderOverlayMode.InverseRainbow,
			RenderOverlayMode.HeatMap,
			RenderOverlayMode.Glitch,
			RenderOverlayMode.Disco
		};
		this._variables.Clear();
		this._variables = new List<string>
		{
			"_None",
			"_Damage",
			"_Power",
			"_Price",
			"_Heat",
			"_Mass",
			"_Pressure"
		};
		this._renderPresets.Clear();
		this._renderPresets = new List<RenderPresets>
		{
			RenderPresets.Default,
			RenderPresets.Power,
			RenderPresets.Damage,
			RenderPresets.Mass,
			RenderPresets.Price,
			RenderPresets.Heat,
			RenderPresets.Pressure
		};
		this._valueInput.interactable = true;
		this._presetPower.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetPower.GetComponent<CanvasGroup>().interactable = true;
		this._presetDamage.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetDamage.GetComponent<CanvasGroup>().interactable = true;
		this._presetPrice.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetPrice.GetComponent<CanvasGroup>().interactable = true;
		this._presetMass.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetMass.GetComponent<CanvasGroup>().interactable = true;
		this._presetHeat.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetHeat.GetComponent<CanvasGroup>().interactable = true;
		this._presetPressure.GetComponent<CanvasGroup>().alpha = 1f;
		this._presetPressure.GetComponent<CanvasGroup>().interactable = true;
		this._toggleFOV.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleFOV.GetComponent<CanvasGroup>().interactable = true;
		this._toggleLights.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleLights.GetComponent<CanvasGroup>().interactable = true;
		this._toggleExteriors.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleExteriors.GetComponent<CanvasGroup>().interactable = true;
		this._toggleCeiling.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleCeiling.GetComponent<CanvasGroup>().interactable = true;
		this._togglePower.GetComponent<CanvasGroup>().alpha = 1f;
		this._togglePower.GetComponent<CanvasGroup>().interactable = true;
		this._togglePlaceholders.GetComponent<CanvasGroup>().alpha = 1f;
		this._togglePlaceholders.GetComponent<CanvasGroup>().interactable = true;
		this._toggleTasks.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleTasks.GetComponent<CanvasGroup>().interactable = true;
		this._toggleLogScale.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleLogScale.GetComponent<CanvasGroup>().interactable = true;
		this._toggleAO.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleAO.GetComponent<CanvasGroup>().interactable = true;
		this._toggleUpdates.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleUpdates.GetComponent<CanvasGroup>().interactable = true;
		this._pnlGradient.alpha = 1f;
		this._pnlGradient.interactable = true;
		this._pnlMinMax.alpha = 1f;
		this._pnlMinMax.interactable = true;
		this._pnlOpacity.alpha = 1f;
		this._pnlOpacity.interactable = true;
		this._pnlValue.alpha = 1f;
		this._pnlValue.interactable = true;
	}

	public void MinimalControls()
	{
		this._allowedModes.Clear();
		this._allowedModes = new List<RenderOverlayMode>
		{
			RenderOverlayMode.Default
		};
		this._variables.Clear();
		this._variables = new List<string>
		{
			"_None"
		};
		this._renderPresets.Clear();
		this._renderPresets = new List<RenderPresets>
		{
			RenderPresets.Default
		};
		this._valueInput.interactable = false;
		this._presetPower.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetPower.GetComponent<CanvasGroup>().interactable = false;
		this._presetDamage.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetDamage.GetComponent<CanvasGroup>().interactable = false;
		this._presetPrice.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetPrice.GetComponent<CanvasGroup>().interactable = false;
		this._presetMass.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetMass.GetComponent<CanvasGroup>().interactable = false;
		this._presetHeat.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetHeat.GetComponent<CanvasGroup>().interactable = false;
		this._presetPressure.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._presetPressure.GetComponent<CanvasGroup>().interactable = false;
		this._toggleFOV.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._toggleFOV.GetComponent<CanvasGroup>().interactable = false;
		this._toggleLights.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._toggleLights.GetComponent<CanvasGroup>().interactable = false;
		this._toggleExteriors.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleExteriors.GetComponent<CanvasGroup>().interactable = true;
		this._toggleCeiling.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleCeiling.GetComponent<CanvasGroup>().interactable = true;
		this._togglePower.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._togglePower.GetComponent<CanvasGroup>().interactable = false;
		this._togglePlaceholders.GetComponent<CanvasGroup>().alpha = 1f;
		this._togglePlaceholders.GetComponent<CanvasGroup>().interactable = true;
		this._toggleTasks.GetComponent<CanvasGroup>().alpha = 1f;
		this._toggleTasks.GetComponent<CanvasGroup>().interactable = true;
		this._toggleLogScale.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._toggleLogScale.GetComponent<CanvasGroup>().interactable = false;
		this._toggleAO.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._toggleAO.GetComponent<CanvasGroup>().interactable = false;
		this._toggleUpdates.GetComponent<CanvasGroup>().alpha = this._minimumOpacity;
		this._toggleUpdates.GetComponent<CanvasGroup>().interactable = false;
		this._pnlGradient.alpha = this._minimumOpacity;
		this._pnlGradient.interactable = false;
		this._pnlMinMax.alpha = this._minimumOpacity;
		this._pnlMinMax.interactable = false;
		this._pnlOpacity.alpha = this._minimumOpacity;
		this._pnlOpacity.interactable = false;
		this._pnlValue.alpha = this._minimumOpacity;
		this._pnlValue.interactable = false;
	}

	public void AssembleUI()
	{
		this.MinimalControls();
		base.StartCoroutine(this._Assemble());
	}

	public IEnumerator _Assemble()
	{
		CondTrigger ct = DataHandler.GetCondTrigger("TIsWristSlotted");
		CondOwner coUser = CrewSim.GetSelectedCrew();
		List<CondOwner> pdas = coUser.GetCOs(false, ct);
		this._bAssembledUIFirstTime = true;
		if (pdas == null || pdas.Count == 0)
		{
			coUser.LogMessage(DataHandler.GetString("PDA_VIZ_ERROR_NO_SLOTTED", false), "Bad", coUser.strID);
			yield break;
		}
		if (pdas.Count > 1)
		{
			coUser.LogMessage(DataHandler.GetString("PDA_VIZ_ERROR_TOO_MANY", false), "Bad", coUser.strID);
		}
		ct = DataHandler.GetCondTrigger("TIsPDACartridge");
		List<CondOwner> carts = pdas[0].GetCOs(true, ct);
		if (carts == null || carts.Count == 0)
		{
			coUser.LogMessage(DataHandler.GetString("PDA_VIZ_ERROR_NO_CARTS", false), "Bad", coUser.strID);
			this.PresetSimple(RenderPresets.Default.ToString());
			yield break;
		}
		yield return null;
		foreach (CondOwner co in carts)
		{
			if (co.HasCond("PDAPresetPower"))
			{
				this._presetPower.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetPower.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Power))
				{
					this._renderPresets.Add(RenderPresets.Power);
				}
			}
			if (co.HasCond("PDAPresetDamage"))
			{
				this._presetDamage.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetDamage.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Damage))
				{
					this._renderPresets.Add(RenderPresets.Damage);
				}
			}
			if (co.HasCond("PDAPresetPrice"))
			{
				this._presetPrice.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetPrice.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Price))
				{
					this._renderPresets.Add(RenderPresets.Price);
				}
			}
			if (co.HasCond("PDAPresetMass"))
			{
				this._presetMass.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetMass.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Mass))
				{
					this._renderPresets.Add(RenderPresets.Mass);
				}
			}
			if (co.HasCond("PDAPresetHeat"))
			{
				this._presetHeat.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetHeat.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Heat))
				{
					this._renderPresets.Add(RenderPresets.Heat);
				}
			}
			if (co.HasCond("PDAPresetPressure"))
			{
				this._presetPressure.GetComponent<CanvasGroup>().alpha = 1f;
				this._presetPressure.GetComponent<CanvasGroup>().interactable = true;
				if (!this._renderPresets.Contains(RenderPresets.Pressure))
				{
					this._renderPresets.Add(RenderPresets.Pressure);
				}
			}
			yield return null;
			if (co.HasCond("PDAToggleFOV"))
			{
				this._toggleFOV.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleFOV.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleLights"))
			{
				this._toggleLights.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleLights.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleExteriors"))
			{
				this._toggleExteriors.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleExteriors.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleCeiling"))
			{
				this._toggleCeiling.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleCeiling.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDATogglePower"))
			{
				this._togglePower.GetComponent<CanvasGroup>().alpha = 1f;
				this._togglePower.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDATogglePlaceholders"))
			{
				this._togglePlaceholders.GetComponent<CanvasGroup>().alpha = 1f;
				this._togglePlaceholders.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleTasks"))
			{
				this._toggleTasks.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleTasks.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleLogScale"))
			{
				this._toggleLogScale.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleLogScale.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleContours"))
			{
				this._toggleAO.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleAO.GetComponent<CanvasGroup>().interactable = true;
			}
			if (co.HasCond("PDAToggleRealTime"))
			{
				this._toggleUpdates.GetComponent<CanvasGroup>().alpha = 1f;
				this._toggleUpdates.GetComponent<CanvasGroup>().interactable = true;
			}
			yield return null;
			bool bValueOn = false;
			if (co.HasCond("PDAVariableAny"))
			{
				this._valueInput.interactable = true;
				bValueOn = true;
			}
			if (co.HasCond("PDAVariable_Power") && !this._variables.Contains("_Power"))
			{
				this._variables.Add("_Power");
			}
			if (co.HasCond("PDAVariable_Damage") && !this._variables.Contains("_Damage"))
			{
				this._variables.Add("_Damage");
			}
			if (co.HasCond("PDAVariable_Mass") && !this._variables.Contains("_Mass"))
			{
				this._variables.Add("_Mass");
			}
			if (co.HasCond("PDAVariable_Price") && !this._variables.Contains("_Price"))
			{
				this._variables.Add("_Price");
			}
			if (co.HasCond("PDAVariable_Heat") && !this._variables.Contains("_Heat"))
			{
				this._variables.Add("_Heat");
			}
			if (co.HasCond("PDAVariable_Pressure") && !this._variables.Contains("_Pressure"))
			{
				this._variables.Add("_Pressure");
			}
			yield return null;
			if (co.HasCond("PDAGradient01"))
			{
				this._allowedModes.Add(RenderOverlayMode.Clean);
			}
			if (co.HasCond("PDAGradient02"))
			{
				this._allowedModes.Add(RenderOverlayMode.Monochrome);
			}
			if (co.HasCond("PDAGradient03"))
			{
				this._allowedModes.Add(RenderOverlayMode.Tricolor);
			}
			if (co.HasCond("PDAGradient04"))
			{
				this._allowedModes.Add(RenderOverlayMode.Rainbow);
			}
			if (co.HasCond("PDAGradient05"))
			{
				this._allowedModes.Add(RenderOverlayMode.Oldred);
			}
			if (co.HasCond("PDAGradient06"))
			{
				this._allowedModes.Add(RenderOverlayMode.Opacity);
			}
			if (co.HasCond("PDAGradient07"))
			{
				this._allowedModes.Add(RenderOverlayMode.Golden);
			}
			if (co.HasCond("PDAGradient08"))
			{
				this._allowedModes.Add(RenderOverlayMode.InverseRainbow);
			}
			if (co.HasCond("PDAGradient09"))
			{
				this._allowedModes.Add(RenderOverlayMode.HeatMap);
			}
			if (co.HasCond("PDAGradient10"))
			{
				this._allowedModes.Add(RenderOverlayMode.Glitch);
			}
			if (co.HasCond("PDAGradient11"))
			{
				this._allowedModes.Add(RenderOverlayMode.Disco);
			}
			yield return null;
			if (co.HasCond("PDAShowInputGradient") || this._allowedModes.Count > 1)
			{
				this._pnlGradient.alpha = 1f;
				this._pnlGradient.interactable = true;
			}
			if (co.HasCond("PDAShowInputMinMax"))
			{
				this._pnlMinMax.alpha = 1f;
				this._pnlMinMax.interactable = true;
			}
			if (co.HasCond("PDAShowInputOpacity"))
			{
				this._pnlOpacity.alpha = 1f;
				this._pnlOpacity.interactable = true;
			}
			if (bValueOn || co.HasCond("PDAShowInputValue") || this._variables.Count > 1)
			{
				this._pnlValue.alpha = 1f;
				this._pnlValue.interactable = true;
			}
		}
		this._renderPresets.Sort();
		yield break;
	}

	public void ResetPresets()
	{
		this.MakeDefaultPresets();
		base.StartCoroutine(this._ResetPresets());
	}

	private IEnumerator _ResetPresets()
	{
		this.EnableEverything();
		yield return new WaitForSeconds(0.25f);
		this.PresetDamage(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetPower(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetPrice(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetMass(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetHeat(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetPressure(true);
		yield return new WaitForSeconds(0.25f);
		this.PresetDefault(true);
		yield return null;
		this.AssembleUI();
		yield break;
	}

	public void Cycle()
	{
		base.StartCoroutine(this._CycleAsync());
	}

	private IEnumerator _CycleAsync()
	{
		if (!this._bAssembledUIFirstTime)
		{
			this.MinimalControls();
			yield return this._Assemble();
		}
		this._Cycle();
		yield break;
	}

	private void _Cycle()
	{
		Debug.Log("Current Preset:" + this._currentPreset.ToString());
		RenderPresets item = this._currentPreset;
		if (this._renderPresets.Contains(item))
		{
			Debug.Log("List Contains Preset!");
			int num = this._renderPresets.IndexOf(item) + 1;
			Debug.Log("Index: " + num);
			if (num >= this._renderPresets.Count)
			{
				Debug.Log("Overflowed!");
				item = this._renderPresets[0];
			}
			else
			{
				item = this._renderPresets[num];
			}
		}
		else
		{
			item = this._renderPresets[0];
		}
		Debug.Log("New Preset:" + item.ToString());
		this.PresetSimple(item.ToString());
	}

	public string CreateCustomInfo()
	{
		string text = string.Empty;
		if (this._renderPresets.Count <= 1)
		{
			return text;
		}
		string text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"version:",
			this._version,
			"|"
		});
		text = text + "preset:" + this._currentPreset.ToString() + "|";
		Debug.Log(text);
		return text;
	}

	public string PresetsToCustomInfos()
	{
		string text = string.Empty;
		foreach (KeyValuePair<string, VizorPreset> keyValuePair in this._savedPresets)
		{
			string text2 = text;
			text = string.Concat(new string[]
			{
				text2,
				keyValuePair.Key,
				"#",
				keyValuePair.Value.ToCustomInfo(),
				";"
			});
		}
		return text;
	}

	public void PresetsFromCustomInfos(string customInfo)
	{
		string[] array = customInfo.Split(new char[]
		{
			';'
		});
		foreach (string text in array)
		{
			if (text != null && text.Length != 0)
			{
				string[] array3 = text.Split(new char[]
				{
					'#'
				});
				if (array3.Length == 0 || array3[0].Length == 0)
				{
					Debug.LogError("Incorrectly formatted preset custom info: [" + text + "] in: " + customInfo);
				}
				else
				{
					if (!this._savedPresets.ContainsKey(array3[0]))
					{
						this._savedPresets[array3[0]] = new VizorPreset("_None", 1f, 0f, 1f, RenderOverlayMode.Default, RenderPresets.Default, true, true, true, true, true, true, true, false, false, false);
					}
					this._savedPresets[array3[0]].FromCustomInfo(array3[1]);
				}
			}
		}
	}

	public void MakeDefaultPresets()
	{
		this._savedPresets[RenderPresets.Default.ToString()] = new VizorPreset("_None", 1f, 0f, 1f, RenderOverlayMode.Default, RenderPresets.Default, true, true, true, true, true, true, true, false, false, false);
		this._savedPresets[RenderPresets.Power.ToString()] = new VizorPreset("_Power", 1f, 0f, 0.95f, RenderOverlayMode.Golden, RenderPresets.Power, true, true, true, true, false, true, false, true, true, true);
		this._savedPresets[RenderPresets.Damage.ToString()] = new VizorPreset("_Damage", 1f, 0f, 1f, RenderOverlayMode.Oldred, RenderPresets.Damage, true, false, false, true, false, false, false, false, false, false);
		this._savedPresets[RenderPresets.Mass.ToString()] = new VizorPreset("_Mass", 1000f, 0f, 0.5f, RenderOverlayMode.Rainbow, RenderPresets.Mass, true, false, false, true, false, true, false, false, true, false);
		this._savedPresets[RenderPresets.Price.ToString()] = new VizorPreset("_Price", 10000f, 0f, 0.5f, RenderOverlayMode.InverseRainbow, RenderPresets.Price, true, false, false, true, false, true, false, false, true, false);
		this._savedPresets[RenderPresets.Heat.ToString()] = new VizorPreset("_Heat", 673f, 293f, 0.95f, RenderOverlayMode.HeatMap, RenderPresets.Heat, true, false, false, true, false, true, false, false, true, true);
		this._savedPresets[RenderPresets.Pressure.ToString()] = new VizorPreset("_Pressure", 2000f, 0f, 0.95f, RenderOverlayMode.Tricolor, RenderPresets.Pressure, true, false, false, true, false, true, false, false, true, true);
	}

	private bool _useLogScale;

	private bool bNotifyShips;

	private bool _bAssembledUIFirstTime;

	private bool _bResolvingCustomInfos;

	[SerializeField]
	private Color m_colEnabled;

	[SerializeField]
	private Color m_colOff;

	[SerializeField]
	private Toggle _presetDefault;

	[SerializeField]
	private Toggle _presetPower;

	[SerializeField]
	private Toggle _presetDamage;

	[SerializeField]
	private Toggle _presetMass;

	[SerializeField]
	private Toggle _presetPrice;

	[SerializeField]
	private Toggle _presetHeat;

	[SerializeField]
	private Toggle _presetPressure;

	[SerializeField]
	private Toggle _toggleFOV;

	[SerializeField]
	private Toggle _toggleLights;

	[SerializeField]
	private Toggle _toggleExteriors;

	[SerializeField]
	private Toggle _toggleCeiling;

	[SerializeField]
	private Toggle _togglePower;

	[SerializeField]
	private Toggle _togglePlaceholders;

	[SerializeField]
	private Toggle _toggleTasks;

	[SerializeField]
	private Toggle _toggleLogScale;

	[SerializeField]
	private Toggle _toggleAO;

	[SerializeField]
	private Toggle _toggleUpdates;

	[SerializeField]
	private CanvasGroup _valueInput;

	[SerializeField]
	private CanvasGroup _pnlValue;

	[SerializeField]
	private CanvasGroup _pnlMinMax;

	[SerializeField]
	private CanvasGroup _pnlGradient;

	[SerializeField]
	private CanvasGroup _pnlOpacity;

	[SerializeField]
	private TMP_InputField _txtOverlayVariable;

	[SerializeField]
	private TextMeshProUGUI _txtOverlayVariableCol;

	[SerializeField]
	private TMP_InputField _txtOverlayMin;

	[SerializeField]
	private TMP_InputField _txtOverlayMax;

	[SerializeField]
	private TextMeshProUGUI _txtOverlayGradient;

	[SerializeField]
	private Slider _OverlayOpacity;

	[SerializeField]
	private List<RenderOverlayMode> _allowedModes = new List<RenderOverlayMode>
	{
		RenderOverlayMode.Default,
		RenderOverlayMode.Oldred
	};

	[SerializeField]
	private List<string> _variables = new List<string>
	{
		"_None",
		"_Damage"
	};

	[SerializeField]
	private List<RenderPresets> _renderPresets = new List<RenderPresets>();

	[SerializeField]
	private RenderPresets _currentPreset;

	[SerializeField]
	private Image _gradientUI;

	[SerializeField]
	private float _pingTime;

	[SerializeField]
	private bool _togglePingUpdates;

	[SerializeField]
	private float _minimumOpacity = 0.25f;

	[SerializeField]
	private int _version = 1;

	private Dictionary<string, VizorPreset> _savedPresets;

	private string _strVariable = "_None";

	private float _fMin;

	private float _fMax = 1f;

	private RenderOverlayMode _Gradient;

	private float _fOpacity = 0.8f;
}
