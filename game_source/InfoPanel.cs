using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One clickable entry in the Info/tutorial browser. Handles hover coloring,
// click-through to the selected node, and simple layout animation state.
public class InfoPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IEventSystemHandler
{
	// Cached RectTransform used by the info browser layout code.
	public RectTransform rect
	{
		get
		{
			if (!this._rect)
			{
				this._rect = (base.transform as RectTransform);
			}
			return this._rect;
		}
	}

	// Opens this node in the main Info view on left-click.
	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.pointerId == -1)
		{
			this.info.DrawNode(this.node);
			AudioManager.am.PlayAudioEmitterAtVol("UIGameplayNotiOpen", false, false, 0.35f);
		}
	}

	// Applies hover colors and click audio while this node is not the active child.
	public void OnPointerEnter(PointerEventData eventData)
	{
		AudioManager.am.PlayAudioEmitterAtVol("UIGameplayClick", false, false, 0.1f);
		if (this.node != this.info.CurrentChild)
		{
			this.title.color = this.textColorPressed;
			this.bgImage.color = this.bgColorPressed;
			this.arrowImage.color = Info.instance.arrowPressed;
			if (this.isExpandedChild)
			{
				this.title.color = Color.Lerp(this.textColorPressed, Color.black, 0.1f);
			}
		}
	}

	// Restores the appropriate color set based on hover/current selection state.
	public void OnPointerExit(PointerEventData eventData)
	{
		if (this.node != this.info.CurrentChild)
		{
			this.title.color = this.textColorStart;
			this.bgImage.color = this.bgColorStart;
			this.arrowImage.color = Info.instance.arrowStart;
			if (this.isExpandedChild)
			{
				this.title.color = Color.Lerp(this.textColorStart, Color.black, 0.1f);
			}
		}
		else
		{
			this.title.color = this.textColorCurrent;
			this.bgImage.color = this.bgColorCurrent;
			this.arrowImage.color = Info.instance.arrowPressed;
			if (this.isExpandedChild)
			{
				this.title.color = Color.Lerp(this.textColorCurrent, Color.black, 0.1f);
			}
		}
	}

	[NonSerialized]
	public Info info;

	[NonSerialized]
	public InfoNode node;

	public Image arrowImage;

	public TextMeshProUGUI title;

	public Image bgImage;

	public bool expanded;

	public bool isExpandedChild;

	public Color bgColorStart;

	public Color bgColorPressed;

	public Color bgColorCurrent;

	public Color textColorStart;

	public Color textColorPressed;

	public Color textColorCurrent;

	[NonSerialized]
	public Vector3 vel;

	public bool finished;

	public bool started;

	public Vector3 start;

	public Vector3 end;

	public float moveDuration;

	public float time;

	private RectTransform _rect;

	public Action OnMouseDown;
}
