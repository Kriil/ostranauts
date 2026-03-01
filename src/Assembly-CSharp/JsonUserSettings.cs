using System;
using System.Collections.Generic;
using UnityEngine;

// Saved user preference payload. This appears to bridge options UI, PlayerPrefs-
// style behavior, app layout, autosaves, and mod/save-path preferences.
public class JsonUserSettings
{
	// Likely the singleton settings record id.
	public string strName { get; set; }

	public string strPathMods { get; set; }

	public string strDateFormat { get; set; }

	public string strTemperatureUnit { get; set; }

	public string strVerbose { get; set; }

	public string strUseAxis { get; set; }

	public List<string> strApps { get; set; }

	public int nAutosaveInterval { get; set; }

	public int nAutosaveMaxCount { get; set; }

	public bool bMuteObjectives { get; set; }

	public bool bMuteInfo { get; set; }

	public string strShowTutorials { get; set; }

	public string strNewPlayer { get; set; }

	public string strSaveLocation { get; set; }

	public bool bSaveOnClose { get; set; }

	public bool bDisableParallaxRotation { get; set; }

	public int nFlickerAmount { get; set; }

	// Returns the chosen save path, defaulting to Unity's persistent data path.
	public string GetSaveLocation()
	{
		return this.strSaveLocation ?? Application.persistentDataPath;
	}

	// Interprets the saved tutorial toggle, which is stored as a string.
	public bool ShowTutorials()
	{
		return this.strShowTutorials != null && this.strShowTutorials != "false";
	}

	// Writes the tutorial toggle back in the string-based format used by saves.
	public void SetShowTutorial(bool show)
	{
		this.strShowTutorials = ((!show) ? "false" : "true");
	}

	// Converts the saved date-format string into the enum used by UI/time code.
	public MathUtils.DateFormat DateFormat()
	{
		if (!Enum.IsDefined(typeof(MathUtils.DateFormat), this.strDateFormat))
		{
			return MathUtils.DateFormat.YYYY_MM_DD;
		}
		return (MathUtils.DateFormat)Enum.Parse(typeof(MathUtils.DateFormat), this.strDateFormat);
	}

	// Converts the saved temperature-unit string into the enum used by UI/tooltips.
	public MathUtils.TemperatureUnit TemperatureUnit()
	{
		if (!Enum.IsDefined(typeof(MathUtils.TemperatureUnit), this.strTemperatureUnit))
		{
			return MathUtils.TemperatureUnit.K;
		}
		return (MathUtils.TemperatureUnit)Enum.Parse(typeof(MathUtils.TemperatureUnit), this.strTemperatureUnit);
	}

	// Initializes a default settings profile for first run / missing settings.
	public void Init()
	{
		this.strName = "UserSettings";
		this.strDateFormat = MathUtils.DateFormat.YYYY_MM_DD.ToString();
		this.strTemperatureUnit = MathUtils.TemperatureUnit.K.ToString();
		this.strPathMods = Application.dataPath + "/Mods/loading_order.json";
		this.strVerbose = "False";
		this.strUseAxis = "False";
		this.strApps = new List<string>();
		this.strApps.Add("goals");
		this.strApps.Add("roster");
		this.strApps.Add("gigs");
		this.strApps.Add("tasks");
		this.strApps.Add("viz");
		this.strApps.Add("build");
		this.strApps.Add("orders");
		this.nAutosaveInterval = 900;
		this.nAutosaveMaxCount = 5;
		this.bMuteObjectives = false;
		this.bMuteInfo = false;
		this.strShowTutorials = "true";
		this.strNewPlayer = "true";
		this.strSaveLocation = Application.persistentDataPath;
		this.bSaveOnClose = true;
		this.bDisableParallaxRotation = false;
		this.nFlickerAmount = 2;
	}

	// Returns a detached copy suitable for editing without mutating the live set.
	public JsonUserSettings Clone()
	{
		return new JsonUserSettings
		{
			strName = this.strName,
			strDateFormat = this.strDateFormat,
			strTemperatureUnit = this.strTemperatureUnit,
			strPathMods = this.strPathMods,
			strVerbose = this.strVerbose,
			strUseAxis = this.strUseAxis,
			strShowTutorials = this.strShowTutorials,
			strNewPlayer = this.strNewPlayer,
			strSaveLocation = this.strSaveLocation,
			bSaveOnClose = this.bSaveOnClose,
			bDisableParallaxRotation = this.bDisableParallaxRotation,
			nFlickerAmount = this.nFlickerAmount
		};
	}

	public void CopyTo(JsonUserSettings jusFinal)
	{
		if (jusFinal.strName == null)
		{
			jusFinal.strName = this.strName;
		}
		if (jusFinal.strDateFormat == null)
		{
			jusFinal.strDateFormat = this.strDateFormat;
		}
		if (jusFinal.strTemperatureUnit == null)
		{
			jusFinal.strTemperatureUnit = this.strTemperatureUnit;
		}
		if (jusFinal.strPathMods == null)
		{
			jusFinal.strPathMods = this.strPathMods;
		}
		if (jusFinal.strVerbose == null)
		{
			jusFinal.strVerbose = this.strVerbose;
		}
		if (jusFinal.strUseAxis == null)
		{
			jusFinal.strUseAxis = this.strUseAxis;
		}
		if (jusFinal.strApps == null)
		{
			jusFinal.strApps = this.strApps;
		}
		if (jusFinal.nAutosaveInterval == 0)
		{
			jusFinal.nAutosaveInterval = this.nAutosaveInterval;
		}
		if (jusFinal.nAutosaveMaxCount == 0)
		{
			jusFinal.nAutosaveMaxCount = this.nAutosaveMaxCount;
		}
		if (jusFinal.strShowTutorials == null)
		{
			jusFinal.strShowTutorials = this.strShowTutorials;
		}
		if (jusFinal.strNewPlayer == null)
		{
			jusFinal.strNewPlayer = this.strNewPlayer;
		}
		if (jusFinal.strSaveLocation == null)
		{
			jusFinal.strSaveLocation = this.strSaveLocation;
		}
		if (jusFinal.nFlickerAmount == 0)
		{
			jusFinal.nFlickerAmount = this.nFlickerAmount;
		}
	}
}
