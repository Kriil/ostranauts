using System;
using Ostranauts.UI.MegaToolTip.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class ModuleBase : MonoBehaviour, IDataModule
	{
		protected void Awake()
		{
			ModuleHost.UpdateUI.AddListener(new UnityAction(this.OnUpdateUI));
		}

		public virtual void SetData(CondOwner co)
		{
		}

		public virtual bool IsMarkedForDestroy()
		{
			return this._IsMarkedForDestroy;
		}

		protected virtual void OnUpdateUI()
		{
		}

		public void Destroy()
		{
			ModuleHost.UpdateUI.RemoveListener(new UnityAction(this.OnUpdateUI));
			UnityEngine.Object.Destroy(base.gameObject);
		}

		protected bool _IsMarkedForDestroy;
	}
}
