using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Interfaces;
using UnityEngine;

// Base class for installed-ship GUI panels. Owns the per-panel prop map, GUI
// harness integration, pause-on-open behavior, and common open-window tracking.
public class GUIData : MonoBehaviour
{
	// Initializes the GUI harness and the base prop-map bookkeeping.
	protected virtual void Awake()
	{
		this.igh = base.gameObject.AddComponent<IGUIHarness>();
		this.dictPropMap = new Dictionary<string, string>();
		this.strCOKey = string.Empty;
		this.strFriendlyName = string.Empty;
	}

	// Returns the current runtime prop map for this panel.
	public virtual Dictionary<string, string> GetPropMap()
	{
		return this.dictPropMap;
	}

	// Updates the local prop map and mirrors the change back into the owning
	// CondOwner GUI prop-map dictionary when possible.
	public void SetPropMapData(string strKey, string strValue)
	{
		if (strKey == null)
		{
			return;
		}
		if (strValue == null)
		{
			this.dictPropMap.Remove(strKey);
		}
		else
		{
			this.dictPropMap[strKey] = strValue;
		}
		Dictionary<string, string> dictionary = null;
		if (this.COSelf != null && this.COSelf.mapGUIPropMaps.TryGetValue(this.strCOKey, out dictionary))
		{
			if (strValue == null)
			{
				dictionary.Remove(strKey);
			}
			else
			{
				dictionary[strKey] = strValue;
			}
		}
	}

	// Reads a boolean value from the panel prop map, with a fallback default.
	public bool GetPropMapData(string strKey, bool defaultReturnValue)
	{
		if (string.IsNullOrEmpty(strKey))
		{
			return defaultReturnValue;
		}
		string value;
		if (this.dictPropMap.TryGetValue(strKey, out value))
		{
			bool result = false;
			if (bool.TryParse(value, out result))
			{
				return result;
			}
		}
		return defaultReturnValue;
	}

	// Reads an integer value from the panel prop map, with a fallback default.
	public int GetPropMapData(string strKey, int defaultReturnValue)
	{
		if (string.IsNullOrEmpty(strKey))
		{
			return defaultReturnValue;
		}
		string s;
		if (this.dictPropMap.TryGetValue(strKey, out s))
		{
			int result = 0;
			if (int.TryParse(s, out result))
			{
				return result;
			}
		}
		return defaultReturnValue;
	}

	// Raw string accessor for prop-map values when callers need custom parsing.
	protected string GetPropMapData(string strKey)
	{
		if (strKey == null || !this.dictPropMap.ContainsKey(strKey))
		{
			return null;
		}
		return this.dictPropMap[strKey];
	}

	// Binds the panel to a CondOwner and its saved GUI prop map, restoring common
	// direction/prefab settings and pause state.
	public virtual void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		this.COSelf = coSelf;
		this.strCoSelfID = coSelf.strID;
		this.strCOKey = strCOKey;
		this.strFriendlyName = strCOKey;
		if (dict != null)
		{
			List<string> list = new List<string>(dict.Keys);
			foreach (string text in list)
			{
				this.dictPropMap[text] = dict[text];
				if (text == "strFriendlyName")
				{
					this.strFriendlyName = dict[text];
				}
			}
		}
		string strGPMKey = string.Empty;
		if (dict.ContainsKey("strGUIPrefabLeft") && dict["strGUIPrefabLeft"] != null)
		{
			strGPMKey = dict["strGUIPrefabLeft"];
			this.igh.SetGUIDir(coSelf, strGPMKey, "strGUIPrefabLeft");
		}
		if (dict.ContainsKey("strGUIPrefabRight") && dict["strGUIPrefabRight"] != null)
		{
			strGPMKey = dict["strGUIPrefabRight"];
			this.igh.SetGUIDir(coSelf, strGPMKey, "strGUIPrefabRight");
		}
		if (dict.ContainsKey("strGUIPrefabTop") && dict["strGUIPrefabTop"] != null)
		{
			strGPMKey = dict["strGUIPrefabTop"];
			this.igh.SetGUIDir(coSelf, strGPMKey, "strGUIPrefabTop");
		}
		if (dict.ContainsKey("strGUIPrefabBottom") && dict["strGUIPrefabBottom"] != null)
		{
			strGPMKey = dict["strGUIPrefabBottom"];
			this.igh.SetGUIDir(coSelf, strGPMKey, "strGUIPrefabBottom");
		}
		if (this.bPausesGame)
		{
			this.bPausedOld = CrewSim.Paused;
			CrewSim.Paused = true;
		}
	}

	// Optional hook for panels that need to switch their focused input target.
	public virtual void SetInput(CondOwner co)
	{
	}

	// Closes the panel, persists any pending prop-map state through the harness,
	// and restores pause state if this panel paused the sim.
	public virtual void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		this.dictPropMap = null;
		this.bActive = false;
		this.igh.SaveAndClose();
		CanvasManager.CleanupDropDownBlockers(base.gameObject);
		if (this.bPausesGame)
		{
			CrewSim.Paused = this.bPausedOld;
		}
	}

	// Optional per-panel refresh hook used by some installed GUIs.
	public virtual void UpdateUI()
	{
	}

	// Tracks a child data window so this panel can close it later if needed.
	public void RegisterOpenWindow(IDataWindow dataWindow)
	{
		if (dataWindow == null)
		{
			return;
		}
		if (this.ActiveWindows.Contains(dataWindow))
		{
			return;
		}
		this.ActiveWindows.Add(dataWindow);
	}

	// Removes a previously tracked child data window.
	public void UnregisterWindow(IDataWindow dataWindow)
	{
		if (dataWindow == null)
		{
			return;
		}
		for (int i = this.ActiveWindows.Count - 1; i >= 0; i--)
		{
			if (this.ActiveWindows[i] == dataWindow)
			{
				this.ActiveWindows.RemoveAt(i);
			}
		}
		this.ActiveWindows.TrimExcess();
	}

	// Closes the most recently registered child window, if one is still open.
	public bool CloseOutermostWindow()
	{
		if (this.ActiveWindows != null)
		{
			for (int i = this.ActiveWindows.Count - 1; i >= 0; i--)
			{
				if (this.ActiveWindows[i] != null)
				{
					this.ActiveWindows[i].CloseExternally();
					return true;
				}
			}
		}
		return false;
	}

	public virtual CondOwner COSelf
	{
		get
		{
			if ((this.coSelfTemp == null || this.coSelfTemp.bDestroyed) && this.strCoSelfID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strCoSelfID, out this.coSelfTemp);
			}
			return this.coSelfTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strCoSelfID = value.strID;
			this.coSelfTemp = null;
		}
	}

	protected IGUIHarness igh;

	protected Dictionary<string, string> dictPropMap;

	protected string strCoSelfID;

	private CondOwner coSelfTemp;

	public string strCOKey;

	public string strFriendlyName;

	protected bool bPausesGame;

	protected bool bPausedOld;

	public bool bActive;

	private readonly List<IDataWindow> ActiveWindows = new List<IDataWindow>();
}
