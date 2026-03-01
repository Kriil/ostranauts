using System;
using Ostranauts.ShipGUIs.Interfaces;
using UnityEngine;

namespace Ostranauts.ShipGUIs
{
	public abstract class GUIDataWindow : MonoBehaviour, IDataWindow
	{
		private void OnDestroy()
		{
			this.UnregisterWindow();
		}

		public void RegisterWindow()
		{
			GUIData componentInParent = base.GetComponentInParent<GUIData>();
			if (componentInParent != null)
			{
				componentInParent.RegisterOpenWindow(this);
			}
		}

		public void UnregisterWindow()
		{
			GUIData componentInParent = base.GetComponentInParent<GUIData>();
			if (componentInParent != null)
			{
				componentInParent.UnregisterWindow(this);
			}
		}

		public abstract void CloseExternally();
	}
}
