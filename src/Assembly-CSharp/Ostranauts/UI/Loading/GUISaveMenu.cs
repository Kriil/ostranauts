using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Loading
{
	// Save-game menu UI.
	// This shows existing saves, captures a new save name, and routes create/
	// overwrite actions into the events consumed by LoadManager and other systems.
	public class GUISaveMenu : GUISaveLoadBase
	{
		// Registers the save-specific events and binds create/overwrite handlers.
		private new void Awake()
		{
			base.Awake();
			if (GUISaveMenu.OnOverwriteSelected == null)
			{
				GUISaveMenu.OnOverwriteSelected = new OverwriteSaveEvent();
			}
			if (GUISaveMenu.OnOverwriteConfirm == null)
			{
				GUISaveMenu.OnOverwriteConfirm = new OverwriteSaveEvent();
			}
			if (GUISaveMenu.OnCreateSave == null)
			{
				GUISaveMenu.OnCreateSave = new CreateNewSaveEvent();
			}
			this._btnCreate.onClick.AddListener(new UnityAction(this.OnCreateNewSave));
			GUISaveMenu.OnOverwriteConfirm.AddListener(new UnityAction<SaveInfo>(this.OnShowConfirmationDialogue));
			this._inputField.onSubmit.AddListener(delegate(string A_1)
			{
				this.OnCreateNewSave();
			});
		}

		// Removes the overwrite-confirm listener added in Awake.
		private new void OnDestroy()
		{
			base.OnDestroy();
			GUISaveMenu.OnOverwriteConfirm.RemoveListener(new UnityAction<SaveInfo>(this.OnShowConfirmationDialogue));
		}

		// Overwrite confirmation modal; starts the delayed overwrite once confirmed.
		private void OnShowConfirmationDialogue(SaveInfo saveInfo)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.ConfirmationDialoguePrefab, base.transform);
			Color clrBg = new Color(0.21960784f, 0.26666668f, 0.3372549f);
			Color clrFg = new Color(0.44705883f, 0.4745098f, 0.5137255f);
			Color clrFont = new Color(0.7019608f, 0.7607843f, 0.8156863f);
			gameObject.GetComponent<GUIConfirmationDialogue>().Setup(DataHandler.GetString("GUI_CONFIRM_OVERWRITE", false) + saveInfo.SaveName, delegate()
			{
				MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SAVING", false), DataHandler.GetString("LOAD_WAIT", false));
				this._delayedInvoke = this.StartCoroutine(this.DelayedOverwrite(saveInfo));
			}, clrBg, clrFg, clrFont);
		}

		// Builds the save-slot list and shows the latest save timestamp/screenshot.
		protected override IEnumerator PopulateGUI()
		{
			List<SaveInfo> saveInfos = MonoSingleton<LoadManager>.Instance.GetSaveInfos();
			if (saveInfos == null || !saveInfos.Any<SaveInfo>())
			{
				yield break;
			}
			if (MonoSingleton<LoadManager>.Instance.LastSaveTimestamp != 0.0)
			{
				this._txtLastSaveTime.text = TimeUtils.FromUnixTimeSeconds(MonoSingleton<LoadManager>.Instance.LastSaveTimestamp).ToString();
			}
			this._imgScreencap.texture = FaceAnim2.GetPNG(CrewSim.coPlayer);
			int maxInstantiatesPCall = 5;
			foreach (SaveInfo saveInfo in from x in saveInfos
			orderby x.EpochTimeStamp descending
			select x)
			{
				LoadListEntry loadListEntry = UnityEngine.Object.Instantiate<GameObject>(this.LoadListEntryPrefab, this._tfListParent).GetComponent<LoadListEntry>();
				loadListEntry.Setup(saveInfo, GUISaveLoadEntryMode.Saving);
				this._dictGameObjects.Add(saveInfo, loadListEntry);
				maxInstantiatesPCall--;
				if (maxInstantiatesPCall <= 0)
				{
					maxInstantiatesPCall = 5;
					yield return null;
				}
			}
			yield break;
		}

		// Validates the entered save name, then either creates a new slot or asks for overwrite confirmation.
		private void OnCreateNewSave()
		{
			if (this._delayedInvoke != null)
			{
				return;
			}
			if (this._inputField.text == string.Empty)
			{
				this._inputField.text = CrewSim.coPlayer.strName.ToLower() + "_" + TimeUtils.GetCurrentEpochTimeSeconds();
			}
			this._inputField.text = DataHandler.ConvertStringToFileSafe(this._inputField.text);
			SaveInfo arg;
			if (MonoSingleton<LoadManager>.Instance.DoesSaveExist(this._inputField.text.ToLower(), out arg))
			{
				GUISaveMenu.OnOverwriteConfirm.Invoke(arg);
				return;
			}
			MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SAVING", false), DataHandler.GetString("LOAD_WAIT", false));
			this._delayedInvoke = base.StartCoroutine(this.DelayedInvoke());
		}

		// Delays one frame before firing the create-save event.
		private IEnumerator DelayedInvoke()
		{
			yield return null;
			GUISaveMenu.OnCreateSave.Invoke(this._inputField.text.ToLower());
			this._inputField.text = string.Empty;
			this._delayedInvoke = null;
			yield break;
		}

		// Delays one frame before firing the overwrite-save event.
		private IEnumerator DelayedOverwrite(SaveInfo saveInfo)
		{
			yield return null;
			GUISaveMenu.OnOverwriteSelected.Invoke(saveInfo);
			this._inputField.text = string.Empty;
			this._delayedInvoke = null;
			yield break;
		}

		public static CreateNewSaveEvent OnCreateSave;

		public static OverwriteSaveEvent OnOverwriteSelected;

		public static OverwriteSaveEvent OnOverwriteConfirm;

		[SerializeField]
		private Button _btnCreate;

		[SerializeField]
		private RawImage _imgScreencap;

		[SerializeField]
		private TMP_InputField _inputField;

		[SerializeField]
		private TMP_Text _txtLastSaveTime;

		[SerializeField]
		private Transform _tfListParent;

		private Coroutine _delayedInvoke;
	}
}
