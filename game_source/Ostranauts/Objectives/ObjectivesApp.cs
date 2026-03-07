using System;
using UnityEngine;

namespace Ostranauts.Objectives
{
	public class ObjectivesApp : MonoBehaviour
	{
		private void Awake()
		{
			Transform transform = this._cgSettings.transform.Find("chkMuteObjectives");
			if (transform != null)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(transform.gameObject, this._cgSettings.transform);
				UnityEngine.Object.Destroy(gameObject.GetComponent<MuteToggle>());
				RectTransform rectTransform = gameObject.transform as RectTransform;
				rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, rectTransform.anchorMin.y - 0.1f);
				rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, rectTransform.anchorMax.y - 0.1f);
				gameObject.AddComponent<InfoToggle>();
			}
		}

		private void Start()
		{
			this.SetPage(ObjectivesAppPage.Current);
		}

		private void Update()
		{
			if (this._lerping)
			{
				this._lerpTime += Time.unscaledDeltaTime * this._lerpSpeed;
				if (this._lerpTime > 1f)
				{
					this._lerpTime = 1f;
					this._lerping = false;
				}
				this._imgHighlight.pivot = new Vector2(Mathf.Lerp(this._anchorStart, this._anchorGoal, this._lerpTime), 0.5f);
			}
		}

		public void SetPage(ObjectivesAppPage page)
		{
			if (this._page != page)
			{
				AudioManager.am.PlayAudioEmitter("UIObjectiveSwitchTabs", false, false);
			}
			this._page = page;
			switch (page)
			{
			case ObjectivesAppPage.Current:
				this._cgCurrent.alpha = 1f;
				this._cgCurrent.blocksRaycasts = true;
				this._cgCurrent.interactable = true;
				this._cgFinished.alpha = 0f;
				this._cgFinished.blocksRaycasts = false;
				this._cgFinished.interactable = false;
				this._cgSettings.alpha = 0f;
				this._cgSettings.blocksRaycasts = false;
				this._cgSettings.interactable = false;
				this._lerping = true;
				this._lerpTime = 0f;
				this._anchorStart = this._imgHighlight.pivot.x;
				this._anchorGoal = 0f;
				break;
			case ObjectivesAppPage.Finished:
				this._cgCurrent.alpha = 0f;
				this._cgCurrent.blocksRaycasts = false;
				this._cgCurrent.interactable = false;
				this._cgFinished.alpha = 1f;
				this._cgFinished.blocksRaycasts = true;
				this._cgFinished.interactable = true;
				this._cgSettings.alpha = 0f;
				this._cgSettings.blocksRaycasts = false;
				this._cgSettings.interactable = false;
				this._lerping = true;
				this._lerpTime = 0f;
				this._anchorStart = this._imgHighlight.pivot.x;
				this._anchorGoal = 0.5f;
				break;
			case ObjectivesAppPage.Settings:
				this._cgCurrent.alpha = 0f;
				this._cgCurrent.blocksRaycasts = false;
				this._cgCurrent.interactable = false;
				this._cgFinished.alpha = 0f;
				this._cgFinished.blocksRaycasts = false;
				this._cgFinished.interactable = false;
				this._cgSettings.alpha = 1f;
				this._cgSettings.blocksRaycasts = true;
				this._cgSettings.interactable = true;
				this._lerping = true;
				this._lerpTime = 0f;
				this._anchorStart = this._imgHighlight.pivot.x;
				this._anchorGoal = 1f;
				break;
			}
		}

		public ObjectivesAppPage AppPage
		{
			get
			{
				return this._page;
			}
		}

		[SerializeField]
		private ObjectivesAppPage _page;

		[SerializeField]
		private CanvasGroup _cgCurrent;

		[SerializeField]
		private CanvasGroup _cgFinished;

		[SerializeField]
		private CanvasGroup _cgSettings;

		[SerializeField]
		private RectTransform _imgHighlight;

		[SerializeField]
		private float _anchorStart;

		[SerializeField]
		private float _anchorGoal;

		[SerializeField]
		private float _lerpTime;

		[SerializeField]
		private float _lerpSpeed = 2f;

		[SerializeField]
		private bool _lerping;
	}
}
