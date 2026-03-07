using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIQuickActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void Awake()
	{
		this._bmpReplyIcon.SetActive(false);
	}

	public Interaction IA
	{
		get
		{
			return this._ia;
		}
		set
		{
			this._ia = value;
			this.strIANameLast = ((this._ia == null) ? null : this._ia.strName);
		}
	}

	public void SetIsReply(bool isReply)
	{
		this._bmpReplyIcon.SetActive(isReply);
	}

	public Color Color
	{
		set
		{
			this._txt.color = value;
			this._txtKey.color = value;
			this._bmp.color = value;
			this._bmpIcon.color = value;
		}
	}

	public string ActionGroup
	{
		get
		{
			return (this._ia == null) ? string.Empty : this._ia.strActionGroup;
		}
	}

	public Sprite Icon
	{
		set
		{
			this._bmpIcon.sprite = value;
		}
	}

	public string Text
	{
		get
		{
			return this._txt.text;
		}
		set
		{
			if (this._txt.text != value)
			{
				this._txt.text = value;
			}
		}
	}

	public string HotKey
	{
		get
		{
			return this._txtKey.text;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				this._cgKey.alpha = 0f;
			}
			else
			{
				if (this._txtKey.text != value)
				{
					this._txtKey.text = value;
				}
				this._cgKey.alpha = 1f;
			}
		}
	}

	public bool Clickable
	{
		get
		{
			return this.bClickable;
		}
		set
		{
			this.bClickable = value;
			if (this.bClickable)
			{
				this._cg.alpha = 1f;
			}
			else
			{
				this._cg.alpha = 0.5f;
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		CrewSim.objInstance.tooltip.SetTooltipIA(this._ia, GUITooltip.TooltipWindow.QAB);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		CrewSim.objInstance.tooltip.SetTooltipIA(null, GUITooltip.TooltipWindow.QAB);
	}

	private Interaction _ia;

	public string strIANameLast;

	private bool bClickable = true;

	[SerializeField]
	private TMP_Text _txt;

	[SerializeField]
	private TMP_Text _txtKey;

	[SerializeField]
	private Image _bmp;

	[SerializeField]
	private Image _bmpIcon;

	[SerializeField]
	private GameObject _bmpReplyIcon;

	[SerializeField]
	private CanvasGroup _cg;

	[SerializeField]
	private CanvasGroup _cgKey;
}
