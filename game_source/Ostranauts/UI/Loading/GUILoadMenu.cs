using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Loading
{
	// Load-game menu UI.
	// This lists existing saves, lets the player open or change the save folder,
	// and publishes the selected save through `OnLoadSelected`.
	public class GUILoadMenu : GUISaveLoadBase
	{
		// Registers menu-specific buttons on top of the shared save/load base setup.
		private new void Awake()
		{
			base.Awake();
			if (GUILoadMenu.OnLoadSelected == null)
			{
				GUILoadMenu.OnLoadSelected = new LoadSelectedEvent();
			}
			this.btnOpenFolder.onClick.AddListener(new UnityAction(this.OpenSaveFolder));
			this.btnChangePath.onClick.AddListener(new UnityAction(this.OnChangePath));
			this.btnPath.onClick.AddListener(new UnityAction(this.OnChangePath));
			this.btnReset.onClick.AddListener(new UnityAction(this.OnReset));
		}

		// Resets the save path back to persistentDataPath and refreshes the slot list.
		private void OnReset()
		{
			LoadManager.OnPathChanged.Invoke(Application.persistentDataPath);
			base.StopAllCoroutines();
			this.ResetState();
			base.StartCoroutine(this.PopulateGUI());
		}

		// Opens the current save folder in Explorer.
		private void OpenSaveFolder()
		{
			string str = MonoSingleton<LoadManager>.Instance.SavesPath.Replace("/", "\\");
			Process.Start("explorer.exe", "/select," + str);
		}

		// Lets the player pick a custom save root and then repopulates the list.
		private void OnChangePath()
		{
			string[] array = StandaloneFileBrowser.OpenFolderPanel("Select Folder", MonoSingleton<LoadManager>.Instance.BasePath.Replace("/", "\\"), false);
			if (array != null)
			{
				base.StopAllCoroutines();
				string[] array2 = array;
				int num = 0;
				if (num < array2.Length)
				{
					string arg = array2[num];
					LoadManager.OnPathChanged.Invoke(arg);
					this.ResetState();
					base.StartCoroutine(this.PopulateGUI());
				}
			}
		}

		// Warns that very old saves may not load cleanly with the current build.
		private string CreateSaveWarning()
		{
			if (CrewSim.aReqVersion == null)
			{
				return "CAUTION: Older saves may experience problems.";
			}
			string text = "CAUTION: Saves older than v";
			for (int i = 0; i < CrewSim.aReqVersion.Length; i++)
			{
				text += CrewSim.aReqVersion[i];
				if (i != CrewSim.aReqVersion.Length - 1)
				{
					text += ".";
				}
			}
			return text + " may experience problems.";
		}

		// Destroys current list entries before rebuilding the menu state.
		private void ResetState()
		{
			if (this._dictGameObjects == null)
			{
				return;
			}
			foreach (KeyValuePair<SaveInfo, LoadListEntry> keyValuePair in this._dictGameObjects)
			{
				UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			}
			this._dictGameObjects.Clear();
		}

		// Builds the loadable save list ordered by newest timestamp first.
		protected override IEnumerator PopulateGUI()
		{
			this.txtPath.text = MonoSingleton<LoadManager>.Instance.BasePath;
			string availableSpace = base.GetAvailableSpaceWarning();
			if (!string.IsNullOrEmpty(availableSpace))
			{
				TMP_Text tmp_Text = this.txtPath;
				tmp_Text.text = tmp_Text.text + " | " + base.GetAvailableSpaceWarning();
			}
			this.txtSaveWarning.text = this.CreateSaveWarning();
			List<SaveInfo> saveInfos = MonoSingleton<LoadManager>.Instance.GetSaveInfos();
			if (saveInfos == null || !saveInfos.Any<SaveInfo>())
			{
				yield break;
			}
			int maxInstantiatesPCall = 5;
			foreach (SaveInfo saveInfo in from x in saveInfos
			orderby x.EpochTimeStamp descending
			select x)
			{
				LoadListEntry loadListEntry = UnityEngine.Object.Instantiate<GameObject>(this.LoadListEntryPrefab, this.tfListParent).GetComponent<LoadListEntry>();
				loadListEntry.Setup(saveInfo, GUISaveLoadEntryMode.Loading);
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

		public static LoadSelectedEvent OnLoadSelected;

		[SerializeField]
		private TMP_Text txtSaveWarning;

		[SerializeField]
		private Transform tfListParent;

		[SerializeField]
		private Button btnOpenFolder;

		[SerializeField]
		private Button btnChangePath;

		[SerializeField]
		private Button btnPath;

		[SerializeField]
		private Button btnReset;

		[SerializeField]
		private TMP_Text txtPath;
	}
}
