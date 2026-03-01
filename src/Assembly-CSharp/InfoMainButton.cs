using System;
using Ostranauts.Objectives;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoMainButton : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler, IEventSystemHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
		InfoMainButton.InfoButton function = this.Function;
		if (function != InfoMainButton.InfoButton.MainUI)
		{
			if (function == InfoMainButton.InfoButton.TutorialOpen)
			{
				this.TutorialOpen();
			}
		}
		else
		{
			this.MainUI();
		}
	}

	public void TutorialOpen()
	{
		if (!this.panel)
		{
			return;
		}
		if (this.panel.Objective == null)
		{
			return;
		}
		if (this.panel.Objective.bTutorial && this.panel.Objective.InfoNodeToOpen != null)
		{
			Info.instance.OpenToNode(this.panel.Objective.InfoNodeToOpen);
		}
	}

	public void MainUI()
	{
		if (!DataHandler.bLoaded)
		{
			return;
		}
		if (!Info.instance.triggerDataHandlerInit)
		{
			if (Info.instance.displayed)
			{
				AudioManager.am.PlayAudioEmitterAtVol("UIGameplayNotiClose", false, false, 0.22f);
			}
			else
			{
				AudioManager.am.PlayAudioEmitterAtVol("UIGameplayNotiOpen", false, false, 0.22f);
			}
		}
		Info.instance.MainButtonOpenClose();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		this.imageBG.color = Color.white;
		if (!Info.instance.triggerDataHandlerInit)
		{
			AudioManager.am.PlayAudioEmitterAtVol("UIGameplayClick", false, false, 0.35f);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.imageBG.color = this.BGStart;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}

	private void Awake()
	{
		this.iconStart = this.imageIcon.color;
		this.BGStart = this.imageBG.color;
	}

	private void Update()
	{
	}

	public Image imageBG;

	public Image imageIcon;

	public InfoMainButton.InfoButton Function;

	public ObjectivePanel panel;

	[NonSerialized]
	public Color BGStart;

	[NonSerialized]
	public Color iconStart;

	public enum InfoButton
	{
		MainUI,
		TutorialOpen
	}
}
