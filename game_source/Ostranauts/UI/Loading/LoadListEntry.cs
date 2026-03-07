using System;
using System.Collections;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Loading
{
	public class LoadListEntry : MonoBehaviour
	{
		private void Awake()
		{
			this._pnlCrewOverlay.SetActive(false);
			foreach (RawImage rawImage in this._imgCrew)
			{
				rawImage.gameObject.SetActive(false);
			}
		}

		private void OnDestroy()
		{
			if (LoadManager.SaveInfoImagesLoadedEvent != null)
			{
				LoadManager.SaveInfoImagesLoadedEvent.RemoveListener(new UnityAction<SaveInfo>(this.OnSaveInfoImagesLoaded));
			}
		}

		private void OnLoadClicked()
		{
			if (GUILoadMenu.OnLoadSelected == null)
			{
				return;
			}
			base.StartCoroutine(this.DelayOneFrameForAudio());
		}

		private void OnSaveInfoImagesLoaded(SaveInfo saveInfo)
		{
			if (this._saveInfo == null || saveInfo == null || this._saveInfo != saveInfo)
			{
				return;
			}
			this.UpdateImages(this._saveInfo);
		}

		private IEnumerator DelayOneFrameForAudio()
		{
			yield return null;
			CrewSim.bPauseLock = false;
			GUILoadMenu.OnLoadSelected.Invoke(this._saveInfo);
			yield break;
		}

		private void OnOverwriteClicked()
		{
			if (GUISaveMenu.OnOverwriteConfirm == null)
			{
				return;
			}
			GUISaveMenu.OnOverwriteConfirm.Invoke(this._saveInfo);
		}

		private void OnDeleteClicked()
		{
			if (GUISaveLoadBase.OnConfirmDeletion == null)
			{
				return;
			}
			GUISaveLoadBase.OnConfirmDeletion.Invoke(this._saveInfo);
		}

		public void Setup(SaveInfo saveInfo, GUISaveLoadEntryMode mode)
		{
			if (saveInfo == null)
			{
				return;
			}
			this._saveInfo = saveInfo;
			this._txtPlayerName.text = saveInfo.PlayerName + " of the " + saveInfo.ShipName;
			LoadManager.SaveInfoImagesLoadedEvent.AddListener(new UnityAction<SaveInfo>(this.OnSaveInfoImagesLoaded));
			if (saveInfo.Texture != null || saveInfo.CrewPortraits != null)
			{
				this.UpdateImages(saveInfo);
			}
			if (mode == GUISaveLoadEntryMode.Loading)
			{
				this._btnMaster.GetComponentInChildren<TextMeshProUGUI>().text = "Load";
				this._btnMaster.onClick.AddListener(new UnityAction(this.OnLoadClicked));
			}
			else
			{
				this._btnMaster.GetComponentInChildren<TextMeshProUGUI>().text = "Overwrite";
				this._btnMaster.onClick.AddListener(new UnityAction(this.OnOverwriteClicked));
			}
			this._btnDelete.onClick.AddListener(new UnityAction(this.OnDeleteClicked));
			this._txtsaveDate.text = saveInfo.Timestamp;
			this._txtPlayTime.text = saveInfo.TotalPlayTime;
			this._txtSaveName.text = saveInfo.SaveName;
			this._txtVersion.text = saveInfo.Version;
		}

		private void UpdateImages(SaveInfo saveInfo)
		{
			this._imgPlayerPortrait.texture = saveInfo.Texture;
			if (saveInfo.ScreenShot == null)
			{
				this._imgBackgroundScreencap.gameObject.SetActive(false);
			}
			else
			{
				this._imgBackgroundScreencap.texture = saveInfo.ScreenShot;
				RectTransform component = this._imgBackgroundScreencap.GetComponent<RectTransform>();
				int num = this._saveInfo.ScreenShot.width / this._saveInfo.ScreenShot.height;
				component.sizeDelta = new Vector2(component.sizeDelta.x, (float)num * component.sizeDelta.x);
			}
			if (saveInfo.CrewPortraits != null)
			{
				int num2 = 0;
				while (num2 < this._imgCrew.Length && num2 < saveInfo.CrewPortraits.Count)
				{
					this._imgCrew[num2].texture = saveInfo.CrewPortraits[num2];
					this._imgCrew[num2].gameObject.SetActive(true);
					num2++;
				}
				if (saveInfo.CrewPortraits.Count > this._imgCrew.Length)
				{
					this._pnlCrewOverlay.SetActive(true);
					this._txtPlus.text = "+" + (saveInfo.CrewPortraits.Count - 2);
				}
			}
		}

		[SerializeField]
		private Button _btnMaster;

		[SerializeField]
		private Button _btnDelete;

		[SerializeField]
		private TextMeshProUGUI _txtPlayerName;

		[SerializeField]
		private TextMeshProUGUI _txtSaveName;

		[SerializeField]
		private TextMeshProUGUI _txtsaveDate;

		[SerializeField]
		private TextMeshProUGUI _txtPlayTime;

		[SerializeField]
		private TextMeshProUGUI _txtVersion;

		[SerializeField]
		private RawImage _imgPlayerPortrait;

		[SerializeField]
		private RawImage _imgBackgroundScreencap;

		[SerializeField]
		private TextMeshProUGUI _txtPlus;

		[SerializeField]
		private GameObject _pnlCrewOverlay;

		[SerializeField]
		private RawImage[] _imgCrew;

		private SaveInfo _saveInfo;
	}
}
