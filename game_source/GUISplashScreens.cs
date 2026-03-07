using System;
using TMPro;
using UnityEngine;

public class GUISplashScreens : MonoBehaviour
{
	private void Awake()
	{
	}

	private void Update()
	{
		if (!this.bInit && DataHandler.bLoaded)
		{
			this.txtTitle.text = DataHandler.GetString("GUI_CONTENT_WARNING_TITLE", false);
			this.txtBody.text = DataHandler.GetString("GUI_CONTENT_WARNING_BODY", false);
			this.bInit = true;
		}
	}

	private bool bInit;

	public TMP_Text txtTitle;

	public TMP_Text txtBody;
}
