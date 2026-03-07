using System;
using UnityEngine;

namespace Ostranauts.Components
{
	[RequireComponent(typeof(CanvasGroup))]
	public class Blinker : MonoBehaviour
	{
		private void Awake()
		{
			if (this._cg == null)
			{
				this._cg = base.GetComponent<CanvasGroup>();
			}
		}

		private void Update()
		{
			this._cg.alpha = Mathf.PingPong(Time.unscaledTime * this._speed, 1f);
		}

		[SerializeField]
		private float _speed = 1f;

		private CanvasGroup _cg;
	}
}
