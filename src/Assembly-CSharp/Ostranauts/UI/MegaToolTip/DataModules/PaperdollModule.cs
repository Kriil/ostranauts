using System;
using System.Collections;
using Ostranauts.Core;
using UnityEngine;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class PaperdollModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
		}

		public override void SetData(CondOwner co)
		{
			if (!co.IsRobot)
			{
				MonoSingleton<GUIRenderTargets>.Instance.SetFace(co, false);
			}
			base.StartCoroutine(this.DelayedSet(co));
		}

		private IEnumerator DelayedSet(CondOwner co)
		{
			yield return null;
			this._paperDollManager.SetMttPaperDoll(co);
			yield break;
		}

		[SerializeField]
		private GUIPaperDollManager _paperDollManager;

		private CondOwner _coThem;
	}
}
