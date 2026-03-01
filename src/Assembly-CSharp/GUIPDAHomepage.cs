using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIPDAHomepage : MonoBehaviour
{
	public void GenerateAppIcons()
	{
		for (int i = this.m_appPanel.transform.childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.Destroy(this.m_appPanel.transform.GetChild(i).gameObject, 0.1f);
		}
		base.StartCoroutine(this._GenerateAppIcons());
	}

	private IEnumerator _GenerateAppIcons()
	{
		if (this.bRunning)
		{
			yield break;
		}
		this.bRunning = true;
		this.dictAppIcons = DataHandler.dictPDAAppIcons;
		yield return new WaitForSeconds(0.1f);
		this.dictAppIcons = DataHandler.dictPDAAppIcons;
		this.dictAppObjects = new Dictionary<string, GUIPDAApp>();
		foreach (JsonPDAAppIcon appInfo in this.dictAppIcons.Values)
		{
			if (!appInfo.bHidden || CrewSim.bEnableDebugCommands)
			{
				if (!(appInfo.strName == "home") && !(appInfo.strName == "exit"))
				{
					GameObject appObj = UnityEngine.Object.Instantiate<GameObject>(this.m_prefabApp, this.m_appPanel.transform);
					GUIPDAApp pdaApp = appObj.GetComponent<GUIPDAApp>();
					pdaApp.UpdateInfo(appInfo);
					pdaApp.UpdateNotifs(0);
					this.dictAppObjects.Add(pdaApp.strName, pdaApp);
					yield return null;
				}
			}
		}
		this.bRunning = false;
		this.UpdateAppNotifs();
		yield break;
	}

	public void UpdateAppNotifs()
	{
		if (this.bRunning || !this.bLoaded)
		{
			return;
		}
		foreach (GUIPDAApp guipdaapp in this.dictAppObjects.Values)
		{
			if (guipdaapp.strName == "goals")
			{
				guipdaapp.UpdateNotifs(GUIPDA.instance.NewObjectives);
			}
		}
	}

	public void Activate()
	{
		if (!this.bLoaded)
		{
			this.bLoaded = true;
			this.GenerateAppIcons();
		}
		this.UpdateAppNotifs();
	}

	public void UnHideApp(string appName)
	{
		JsonPDAAppIcon jsonPDAAppIcon = null;
		DataHandler.dictPDAAppIcons.TryGetValue(appName, out jsonPDAAppIcon);
		if (jsonPDAAppIcon == null)
		{
			return;
		}
		jsonPDAAppIcon.bHidden = false;
		DataHandler.dictPDAAppIcons[appName] = jsonPDAAppIcon;
		this.GenerateAppIcons();
	}

	public void HideApp(string appName)
	{
		JsonPDAAppIcon jsonPDAAppIcon = null;
		DataHandler.dictPDAAppIcons.TryGetValue(appName, out jsonPDAAppIcon);
		if (jsonPDAAppIcon == null)
		{
			return;
		}
		jsonPDAAppIcon.bHidden = true;
		DataHandler.dictPDAAppIcons[appName] = jsonPDAAppIcon;
		this.GenerateAppIcons();
	}

	public GameObject m_prefabApp;

	private Dictionary<string, JsonPDAAppIcon> dictAppIcons;

	private Dictionary<string, GUIPDAApp> dictAppObjects;

	public GameObject m_appPanel;

	private bool bLoaded;

	private bool bRunning;
}
