using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Loading
{
	// Shared base for the save-slot and load-slot menus.
	// This owns the common list-entry bookkeeping, delete-confirmation flow,
	// and refresh path when LoadManager says the save metadata changed.
	public class GUISaveLoadBase : MonoBehaviour
	{
		// Registers shared events and hooks the back button / LoadManager refresh event.
		protected void Awake()
		{
			if (GUISaveLoadBase.OnConfirmDeletion == null)
			{
				GUISaveLoadBase.OnConfirmDeletion = new LoadSelectedEvent();
			}
			if (GUISaveLoadBase.OnDeleteSave == null)
			{
				GUISaveLoadBase.OnDeleteSave = new LoadSelectedEvent();
			}
			this._btnBack.onClick.AddListener(new UnityAction(this.OnBackPressed));
			GUISaveLoadBase.OnDeleteSave.AddListener(new UnityAction<SaveInfo>(this.OnDelete));
			GUISaveLoadBase.OnConfirmDeletion.AddListener(new UnityAction<SaveInfo>(this.OnShowConfirmationDialogue));
			LoadManager.OnSaveInfoUpdated.AddListener(new UnityAction(this.OnLoadManagerUpdate));
		}

		// Populates the slot list once the panel is live.
		private IEnumerator Start()
		{
			yield return this.PopulateGUI();
			yield break;
		}

		// Allows Escape to close the panel through the shared keybinding command.
		private void Update()
		{
			if (GUIActionKeySelector.commandEscape != null && GUIActionKeySelector.commandEscape.Down)
			{
				this.OnBackPressed();
			}
		}

		// Removes listeners and stops any in-flight list population coroutine.
		protected void OnDestroy()
		{
			GUISaveLoadBase.OnDeleteSave.RemoveListener(new UnityAction<SaveInfo>(this.OnDelete));
			GUISaveLoadBase.OnConfirmDeletion.RemoveListener(new UnityAction<SaveInfo>(this.OnShowConfirmationDialogue));
			LoadManager.OnSaveInfoUpdated.RemoveListener(new UnityAction(this.OnLoadManagerUpdate));
			base.StopAllCoroutines();
		}

		// Overridden by the concrete save/load menus to build their slot lists.
		protected virtual IEnumerator PopulateGUI()
		{
			yield break;
		}

		// Clears and rebuilds the visible slot list after save metadata changes.
		private void OnLoadManagerUpdate()
		{
			base.StopAllCoroutines();
			foreach (KeyValuePair<SaveInfo, LoadListEntry> keyValuePair in this._dictGameObjects)
			{
				if (keyValuePair.Value != null)
				{
					UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
				}
			}
			this._dictGameObjects.Clear();
			base.StartCoroutine(this.PopulateGUI());
		}

		// Formats a simple free-space warning for the current save device.
		protected string GetAvailableSpaceWarning()
		{
			long availableSpace = MonoSingleton<LoadManager>.Instance.GetAvailableSpace();
			if (availableSpace < 0L)
			{
				return string.Empty;
			}
			if (availableSpace > 1024L)
			{
				return (availableSpace / 1024L).ToString("F2") + " GB free";
			}
			return availableSpace.ToString("N0") + " MB free | <color=red>LOW DISK SPACE!</color>";
		}

		// Default close behavior for the save/load popup.
		private void OnBackPressed()
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}

		// Removes one slot entry after deletion.
		private void OnDelete(SaveInfo saveInfo)
		{
			LoadListEntry loadListEntry;
			if (this._dictGameObjects.TryGetValue(saveInfo, out loadListEntry))
			{
				this._dictGameObjects.Remove(saveInfo);
				UnityEngine.Object.Destroy(loadListEntry.gameObject);
			}
		}

		// Shared delete-confirmation modal.
		private void OnShowConfirmationDialogue(SaveInfo saveInfo)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.ConfirmationDialoguePrefab, base.transform);
			Color clrBg = new Color(0.21960784f, 0.26666668f, 0.3372549f);
			Color clrFg = new Color(0.3647059f, 0.38039216f, 0.41568628f);
			Color clrFont = new Color(0.7019608f, 0.7607843f, 0.8156863f);
			gameObject.GetComponent<GUIConfirmationDialogue>().Setup(DataHandler.GetString("GUI_CONFIRM_DELETE", false) + saveInfo.SaveName, delegate()
			{
				GUISaveLoadBase.OnDeleteSave.Invoke(saveInfo);
			}, clrBg, clrFg, clrFont);
		}

		public static LoadSelectedEvent OnConfirmDeletion;

		public static LoadSelectedEvent OnDeleteSave;

		[SerializeField]
		protected GameObject LoadListEntryPrefab;

		[SerializeField]
		protected GameObject ConfirmationDialoguePrefab;

		[SerializeField]
		private Button _btnBack;

		protected Dictionary<SaveInfo, LoadListEntry> _dictGameObjects = new Dictionary<SaveInfo, LoadListEntry>();
	}
}
