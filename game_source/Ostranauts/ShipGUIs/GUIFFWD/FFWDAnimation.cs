using System;
using UnityEngine;

namespace Ostranauts.ShipGUIs.GUIFFWD
{
	public class FFWDAnimation : MonoBehaviour
	{
		private void Awake()
		{
			this._startingPos = this.tfContainer.rect;
			this.Reset();
		}

		private void Reset()
		{
			this.cg.alpha = 0f;
			this.cg.blocksRaycasts = false;
			this.txtTitle.SetActive(false);
			this.tfContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 50f);
			this._timeStampFinished = 0f;
			this._pauseTimer = 0.5f;
		}

		public void Run(Action animDoneCallback, Action endCallback)
		{
			this._endCallback = endCallback;
			this._animationFinishedCallback = animDoneCallback;
			this.cg.blocksRaycasts = true;
			this._animate = true;
		}

		private void End()
		{
			if (this._endCallback != null)
			{
				this._endCallback();
			}
			AudioManager.am.StopAudioEmitter("FFWD");
			this.Reset();
		}

		private void Update()
		{
			if (!this._animate)
			{
				if (this._timeStampFinished > 0f && this._animationFinishedCallback != null)
				{
					this._animationFinishedCallback();
					this._animationFinishedCallback = null;
				}
				if (this._timeStampFinished > 0f && Time.unscaledTime - this._timeStampFinished > this._waitBeforeEnd)
				{
					this.End();
				}
				return;
			}
			if (this.cg.alpha < 1f)
			{
				this.cg.alpha += Time.unscaledDeltaTime * this._speedScale * 0.02f;
				return;
			}
			if (this._pauseTimer > 0f)
			{
				this._pauseTimer -= Time.unscaledDeltaTime;
			}
			if (this.tfContainer.rect.width < this._startingPos.width)
			{
				this.tfContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this.tfContainer.rect.width + Time.unscaledDeltaTime * this._speedScale * 20f);
				return;
			}
			this.txtTitle.SetActive(true);
			this._timeStampFinished = Time.unscaledTime;
			this._animate = false;
		}

		[SerializeField]
		private RectTransform tfContainer;

		[SerializeField]
		private GameObject txtTitle;

		[SerializeField]
		private CanvasGroup cg;

		private const float PauseAfterFadeIn = 0.5f;

		private Action _animationFinishedCallback;

		private Action _endCallback;

		private Rect _startingPos;

		private bool _animate;

		private float _speedScale = 100f;

		private float _timeStampFinished;

		private float _pauseTimer;

		private float _waitBeforeEnd = 1f;
	}
}
