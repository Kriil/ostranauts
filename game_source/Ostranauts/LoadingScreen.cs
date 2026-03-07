using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ostranauts
{
	public class LoadingScreen : MonoBehaviour
	{
		private void Awake()
		{
			LoadingScreen.Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(this);
			this.imgLoadBackground.AssignBackground();
			this.tipLoadTip.AssignTip();
		}

		public void Start()
		{
			if (!CrewSim.bShipEditTest)
			{
				SceneManager.LoadSceneAsync("CrewSim");
			}
		}

		public static void SetProgressBar(float totalAmount, string textToDisplay = null)
		{
			if (LoadingScreen.Instance == null || LoadingScreen.Instance.imgProgressBar == null)
			{
				return;
			}
			LoadingScreen.Instance.imgProgressBar.fillAmount = totalAmount;
			LoadingScreen.Instance.fProgress = totalAmount;
			if (string.IsNullOrEmpty(textToDisplay))
			{
				return;
			}
			LoadingScreen.Instance.txtLoadingText.text = textToDisplay;
		}

		public static void DestroyLoadingInstance()
		{
			if (LoadingScreen.Instance == null || LoadingScreen.Instance.gameObject == null)
			{
				return;
			}
			if (SceneManager.sceneCount > 0)
			{
				for (int i = 0; i < SceneManager.sceneCount; i++)
				{
					if (SceneManager.GetSceneAt(i).name == "Loading")
					{
						SceneManager.UnloadSceneAsync("Loading");
					}
				}
			}
			LoadingScreen.Instance.gameObject.SetActive(false);
			UnityEngine.Object.Destroy(LoadingScreen.Instance.gameObject);
		}

		public static float GetProgress()
		{
			if (LoadingScreen.Instance == null)
			{
				return 0f;
			}
			return LoadingScreen.Instance.Progress;
		}

		public float Progress
		{
			get
			{
				return this.fProgress;
			}
			set
			{
				Debug.LogWarning("Cannot set LoadingScreen progress through LoadingScreen.Progress! Use SetProgressBar() function instead.");
			}
		}

		public static LoadingScreen Instance;

		[SerializeField]
		private LoadBackground imgLoadBackground;

		[SerializeField]
		private LoadTip tipLoadTip;

		[SerializeField]
		private Image imgProgressBar;

		[SerializeField]
		private TMP_Text txtLoadingText;

		[SerializeField]
		private float fProgress;
	}
}
