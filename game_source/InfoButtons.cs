using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoButtons : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler, IEventSystemHandler
{
	private void Awake()
	{
		if (this.buttonType == InfoButtons.InfoButton.Resize)
		{
			this.startHighlightColor = this.mouseoverHighlight.color;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		AudioManager.am.PlayAudioEmitterAtVol("UIGameplayNotiOpen", false, false, 0.15f);
		switch (this.buttonType)
		{
		case InfoButtons.InfoButton.Home:
			Info.instance.NextChild = Info.instance.Index;
			Info.instance.DrawNode(Info.instance.NextChild);
			break;
		case InfoButtons.InfoButton.Next:
			Info.instance.Next();
			break;
		case InfoButtons.InfoButton.Previous:
			Info.instance.Previous();
			break;
		case InfoButtons.InfoButton.Out:
			Info.instance.Out();
			break;
		case InfoButtons.InfoButton.In:
			Info.instance.In();
			break;
		case InfoButtons.InfoButton.LookupRight:
			Info.instance.NextInSeries();
			break;
		case InfoButtons.InfoButton.LookupLeft:
			Info.instance.PrevInSeries();
			break;
		case InfoButtons.InfoButton.CurrentObjective:
			Info.instance.CurrentObjective();
			break;
		case InfoButtons.InfoButton.MuteTutorials:
			Info.instance.MuteTutorials();
			break;
		case InfoButtons.InfoButton.Resize:
			Info.instance.BeginResize();
			break;
		case InfoButtons.InfoButton.HideHierarchy:
			Info.instance.ToggleList();
			break;
		case InfoButtons.InfoButton.ShowHierarchy:
			Info.instance.ToggleList();
			break;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (Info.instance.resizing || Info.instance.draggingWindow)
		{
			return;
		}
		this.mouseoverHighlight.color = this.highlightColor;
		AudioManager.am.PlayAudioEmitterAtVol("UIGameplayClick", false, false, 0.12f);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.mouseoverHighlight.color = this.startHighlightColor;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		InfoButtons.InfoButton infoButton = this.buttonType;
		if (infoButton == InfoButtons.InfoButton.Exit)
		{
			Info.instance.Close();
		}
	}

	public Image mouseoverHighlight;

	public CanvasGroup cg;

	public InfoButtons.InfoButton buttonType;

	[NonSerialized]
	public Color startHighlightColor = Color.black;

	[NonSerialized]
	public Color highlightColor = new Color(0.3f, 0.3f, 0.3f, 1f);

	public enum InfoButton
	{
		Exit,
		Home,
		Next,
		Previous,
		Out,
		In,
		LookupRight,
		LookupLeft,
		CurrentObjective,
		MuteTutorials,
		Resize,
		HideHierarchy,
		ShowHierarchy
	}
}
