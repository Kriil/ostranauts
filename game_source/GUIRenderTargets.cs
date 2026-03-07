using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Parallax.UI.Portrait;
using UnityEngine;

public class GUIRenderTargets : MonoSingleton<GUIRenderTargets>
{
	private new void Awake()
	{
		base.Awake();
		this.bInit = false;
	}

	private void Init()
	{
		if (this.bInit)
		{
			return;
		}
		GUIRenderTargets.goThis = GameObject.Find("OffscreenDraw");
		GUIRenderTargets.goDockSys = GUIRenderTargets.goThis.transform.Find("CanvasDockSysDraw/goDockSys").gameObject;
		GUIRenderTargets.goLines = GUIRenderTargets.goThis.transform.Find("CanvasOrbitDraw/goLines").gameObject;
		this.goPortraits = GUIRenderTargets.goThis.transform.Find("goPortraits").gameObject;
		this.bInit = true;
	}

	public void ShowOrbits(bool bShow)
	{
		this.Init();
		this.goATC.SetActive(bShow);
		GUIRenderTargets.goDockSys.SetActive(bShow);
		GUIRenderTargets.goLines.SetActive(bShow);
	}

	public void UpdateName(string oldId, string newId)
	{
		if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId))
		{
			return;
		}
		PortraitContainer portraitContainer;
		if (this._portraitContainers.TryGetValue(oldId, out portraitContainer))
		{
			this._portraitContainers.Remove(oldId);
			portraitContainer.CoId = newId;
			this._portraitContainers.Add(newId, portraitContainer);
		}
	}

	public Texture CreatePortrait(CondOwner condOwner)
	{
		if (condOwner == null || string.IsNullOrEmpty(condOwner.strID))
		{
			return null;
		}
		if (this._portraitContainers.ContainsKey(condOwner.strID))
		{
			return this._portraitContainers[condOwner.strID].SetFaceAnim(condOwner, false);
		}
		PortraitContainer portraitContainer = UnityEngine.Object.Instantiate<PortraitContainer>(this.portraitContainerPrefab, this.goPortraits.transform);
		this._portraitContainers.Add(condOwner.strID, portraitContainer);
		int num = 0;
		foreach (KeyValuePair<string, PortraitContainer> keyValuePair in this._portraitContainers)
		{
			if (!(keyValuePair.Value == null))
			{
				PortraitContainer value = keyValuePair.Value;
				value.transform.localPosition = new Vector3(portraitContainer.transform.localPosition.x + (float)(2 * num), base.transform.localPosition.y, base.transform.localPosition.z);
				num++;
			}
		}
		return portraitContainer.SetFaceAnim(condOwner, false);
	}

	public void SetTransform(CondOwner co, Vector3? vMousePosNorm = null)
	{
		PortraitContainer portraitContainer = null;
		if (this._portraitContainers.TryGetValue(co.strID, out portraitContainer))
		{
			portraitContainer.SetTransform(vMousePosNorm);
		}
	}

	public void SetFace(CondOwner co, bool bForce = false)
	{
		if (!this._portraitContainers.ContainsKey(co.strID))
		{
			this.CreatePortrait(co);
		}
		else
		{
			this.SetFaceAnim(co, bForce);
		}
	}

	public void ResetLoadedPortraits()
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew == null || selectedCrew.ship == null || CrewSim.coPlayer == null)
		{
			return;
		}
		List<CondOwner> people = selectedCrew.ship.GetPeople(true);
		List<CondOwner> crewMembers = CrewSim.coPlayer.Company.GetCrewMembers(null);
		List<string> list = new List<string>();
		using (Dictionary<string, PortraitContainer>.Enumerator enumerator = this._portraitContainers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, PortraitContainer> kvp = enumerator.Current;
				if (kvp.Value == null)
				{
					list.Add(kvp.Key);
				}
				else if (!people.Any((CondOwner x) => x.strID == kvp.Value.CoId) && !crewMembers.Any((CondOwner x) => x.strID == kvp.Value.CoId))
				{
					list.Add(kvp.Key);
				}
			}
		}
		foreach (string key in list)
		{
			PortraitContainer portraitContainer;
			if (this._portraitContainers.TryGetValue(key, out portraitContainer))
			{
				UnityEngine.Object.Destroy(portraitContainer.gameObject);
			}
			this._portraitContainers.Remove(key);
		}
	}

	private void SetFaceAnim(CondOwner co, bool bForce = false)
	{
		PortraitContainer portraitContainer = null;
		if (this._portraitContainers.TryGetValue(co.strID, out portraitContainer))
		{
			portraitContainer.SetFaceAnim(co, bForce);
		}
	}

	public void UpdateFaces(CondOwner co, string condName, bool remove)
	{
		if (co == null || string.IsNullOrEmpty(co.strID))
		{
			return;
		}
		PortraitContainer portraitContainer = null;
		if (this._portraitContainers.TryGetValue(co.strID, out portraitContainer))
		{
			portraitContainer.UpdateFace(condName, remove);
		}
	}

	[SerializeField]
	private PortraitContainer portraitContainerPrefab;

	[SerializeField]
	private GameObject goATC;

	[SerializeField]
	private GameObject goPortraits;

	public static GameObject goDockSys;

	public static GameObject goLines;

	private bool bInit;

	private Dictionary<string, PortraitContainer> _portraitContainers = new Dictionary<string, PortraitContainer>();

	public static GameObject goThis;
}
