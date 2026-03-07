using System;
using System.Collections;
using Ostranauts.Core;
using Ostranauts.UI.MegaToolTip;
using TMPro;
using UnityEngine;

// Small rename popup UI.
// Likely used to rename the currently selected module/ship element through ModuleHost.
public class coRename : MonoBehaviour
{
	// Wires input callbacks once when the UI object is created.
	private void Awake()
	{
		this._inputField.onEndEdit.AddListener(delegate(string A_1)
		{
			this.Rename(this._inputField.text);
			this.HideSelf();
		});
		this._inputField.onDeselect.AddListener(delegate(string A_1)
		{
			this.HideSelf();
		});
		coRename.instance = this;
	}

	// Shows the modal input field and puts the game into typing mode.
	public void ShowSelf()
	{
		this._cg.alpha = 1f;
		this._cg.interactable = true;
		this._cg.blocksRaycasts = true;
		CrewSim.Typing = true;
		this._inputField.Select();
	}

	// Hides the popup, exits typing mode, then refreshes the quick bar after a short delay.
	public void HideSelf()
	{
		this._cg.interactable = false;
		this._cg.alpha = 0f;
		this._cg.blocksRaycasts = false;
		CrewSim.Typing = false;
		base.StartCoroutine(this.DelayedRefresh());
	}

	public static void HideInstance()
	{
		if (coRename.instance == null)
		{
			return;
		}
		coRename.instance.HideSelf();
	}

	public static void ShowInstance(string currentName = "")
	{
		if (coRename.instance == null)
		{
			return;
		}
		if (currentName != null)
		{
			coRename.instance._inputField.text = currentName;
		}
		coRename.instance.ShowSelf();
	}

	// Forwards the final text to the active ModuleHost rename target.
	public void Rename(string newName)
	{
		ModuleHost.Rename(newName);
	}

	private IEnumerator DelayedRefresh()
	{
		yield return new WaitForSecondsRealtime(0.1f);
		MonoSingleton<GUIQuickBar>.Instance.Refresh(false);
		yield break;
	}

	public CanvasGroup _cg;

	public TMP_InputField _inputField;

	private static coRename instance;
}
