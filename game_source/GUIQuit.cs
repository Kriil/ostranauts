using System;
using TMPro;
using UnityEngine;

public class GUIQuit : MonoBehaviour
{
	private void Awake()
	{
		this.Init();
	}

	private void Init()
	{
		this.m_canvasGroup.alpha = 0f;
		this.m_canvasGroup.interactable = false;
		this.m_canvasGroup.blocksRaycasts = false;
		this.m_quitConfirm.text = "Confirm";
		this.m_quitDeny.text = "Return";
		this.m_quitText.text = "Quit Game?";
	}

	public void LangSet()
	{
		if (!DataHandler.bLoaded)
		{
			return;
		}
		this.m_quitConfirm.text = DataHandler.GetString("GUI_QUIT_CONFIRM", false);
		this.m_quitDeny.text = DataHandler.GetString("GUI_QUIT_DENY", false);
		this.m_quitText.text = DataHandler.GetString("GUI_QUIT_DESCRIPTION", false);
		this.m_lang = true;
	}

	public bool Shown
	{
		get
		{
			return this.m_shown;
		}
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void ToggleMenu(bool forceClose = false)
	{
		this.m_shown = (!forceClose && !this.m_shown);
		this.m_canvasGroup.alpha = (float)((!this.m_shown) ? 0 : 1);
		this.m_canvasGroup.interactable = this.m_shown;
		this.m_canvasGroup.blocksRaycasts = this.m_shown;
		if (!this.m_lang)
		{
			this.LangSet();
		}
	}

	public void Accept()
	{
		if (this.m_shown)
		{
			this.QuitGame();
		}
	}

	[SerializeField]
	private TextMeshProUGUI m_quitText;

	[SerializeField]
	private TextMeshProUGUI m_quitConfirm;

	[SerializeField]
	private TextMeshProUGUI m_quitDeny;

	[SerializeField]
	private CanvasGroup m_canvasGroup;

	private bool m_shown;

	private bool m_lang;
}
