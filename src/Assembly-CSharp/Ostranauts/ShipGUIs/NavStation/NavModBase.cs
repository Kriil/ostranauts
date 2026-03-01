using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModBase : MonoBehaviour
	{
		protected virtual void Awake()
		{
			this._guiOrbitDraw = base.GetComponentInParent<GUIOrbitDraw>();
			if (this._guiOrbitDraw == null && CrewSim.goIntUIPanel != null)
			{
				this._guiOrbitDraw = CrewSim.goIntUIPanel.GetComponentInChildren<GUIOrbitDraw>();
			}
			if (GUIOrbitDraw.NavModMessageEvent != null && this._guiOrbitDraw != null)
			{
				GUIOrbitDraw.NavModMessageEvent.AddListener(new UnityAction<NavModMessageType, object>(this.OnNavModMessage));
			}
		}

		private void Start()
		{
			if (this._guiOrbitDraw == null)
			{
				return;
			}
			this.COSelf = this._guiOrbitDraw.COSelf;
			this.dictPropMap = this.COSelf.mapGUIPropMaps["Panel A"];
			this.Init();
		}

		protected void OnDestroy()
		{
			if (GUIOrbitDraw.NavModMessageEvent != null)
			{
				GUIOrbitDraw.NavModMessageEvent.RemoveListener(new UnityAction<NavModMessageType, object>(this.OnNavModMessage));
			}
		}

		protected virtual void Init()
		{
		}

		protected virtual void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType == NavModMessageType.UpdateUI)
			{
				this.UpdateUI();
			}
		}

		protected virtual void UpdateUI()
		{
		}

		protected CondOwner COSelf;

		protected Dictionary<string, string> dictPropMap;

		protected GUIOrbitDraw _guiOrbitDraw;
	}
}
