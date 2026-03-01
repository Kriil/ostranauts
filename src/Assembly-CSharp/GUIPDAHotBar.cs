using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIPDAHotBar : MonoBehaviour
{
	private void GenerateAppIcons()
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
		List<string> strApps = DataHandler.dictSettings["UserSettings"].strApps;
		int homeIndex = strApps.IndexOf("home");
		if (homeIndex >= 7 || homeIndex < 0)
		{
			strApps.Insert(0, "home");
		}
		int i = 0;
		while (i < strApps.Count && i <= 8)
		{
			if (!string.IsNullOrEmpty(strApps[i]) && this.dictAppIcons.ContainsKey(strApps[i]) && this.dictAppIcons[strApps[i]] != null)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_prefabApp, this.m_appPanel.transform);
				GUIPDAApp component = gameObject.GetComponent<GUIPDAApp>();
				component.UpdateInfo(this.dictAppIcons[strApps[i]]);
				component.UpdateNotifs(0);
				this.dictAppObjects.Add(component.strName, component);
			}
			yield return null;
			i++;
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

	public void Refresh()
	{
		this.bLoaded = false;
		this.Activate();
	}

	public GameObject m_prefabApp;

	private Dictionary<string, JsonPDAAppIcon> dictAppIcons;

	private Dictionary<string, GUIPDAApp> dictAppObjects;

	public GameObject m_appPanel;

	private bool bLoaded;

	private bool bRunning;

	private const int iMaxApps = 8;
}
