using System;
using System.Collections;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	// One objective card in the tracker/tutorial list. Handles text, icon state,
	// hover highlighting, focus clicks, and tutorial visibility.
	public class ObjectivePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		// Exposes the bound objective model for parent tracker code.
		public Objective Objective
		{
			get
			{
				return this._objective;
			}
		}

		// Cached RectTransform used for tracker layout and animation.
		public RectTransform rectTransform
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

		// Unity setup: caches default colors and ensures the shared highlight/focus
		// events exist before panels start listening.
		protected virtual void Awake()
		{
			if (this.border)
			{
				this._borderStartColor = this.border.color;
			}
			else
			{
				this._borderStartColor = Color.HSVToRGB(0f, 0f, 1f);
			}
			if (ObjectivePanel.OnHighlightObjective == null)
			{
				ObjectivePanel.OnHighlightObjective = new OnObjectiveUpdatedEvent();
			}
			if (ObjectivePanel.OnFocusObjective == null)
			{
				ObjectivePanel.OnFocusObjective = new OnObjectiveFocusEvent();
			}
			if (this._cg && base.transform.parent)
			{
				this._cgparent = base.transform.parent.GetComponentInParent<CanvasGroup>();
			}
		}

		// Hooks tutorial-visibility updates after the tracker event is available.
		private void Start()
		{
			if (ObjectiveTracker.OnShowTutorialToggled == null)
			{
				ObjectiveTracker.OnShowTutorialToggled = new OnShowTutorialsEvent();
			}
			ObjectiveTracker.OnShowTutorialToggled.AddListener(new UnityAction<bool>(this.ShowTutorial));
		}

		// Handles hover highlight pulsing, link hover detection, mouse actions,
		// and border/canvas-group sync each frame.
		private void Update()
		{
			if (this._timer > 0f && this._imgHighlight != null)
			{
				float a = Mathf.PingPong(Time.time, 0.4f);
				this._imgHighlight.color = new Color(1f, 1f, 1f, a);
			}
			if (this.bMouseOver)
			{
				if (!this.CheckLinks(this._txtDescription) && this.strLastHighlightCO != null)
				{
					this._objective.Highlight = true;
					this.strLastHighlightCO = null;
				}
				this.MouseInput();
			}
			this.HighlightBorder();
			this.SyncCGState();
		}

		// Left click focuses the objective target; right click closes the panel.
		protected void MouseInput()
		{
			if (Input.GetMouseButtonUp(0))
			{
				EventSystem.current.SetSelectedGameObject(null);
				CondOwner cofocus = this._objective.COFocus;
				if (!string.IsNullOrEmpty(this.strLastHighlightCO))
				{
					DataHandler.mapCOs.TryGetValue(this.strLastHighlightCO, out cofocus);
				}
				this.FocusObjective(cofocus);
			}
			else if (Input.GetMouseButtonUp(1))
			{
				if (this.scrollRect && (double)this.scrollRect.verticalNormalizedPosition > 0.8)
				{
					this.scrollRect.verticalNormalizedPosition = 1f;
					this.scrollRect.StartCoroutine(this.RepositionScrollRect());
				}
				ObjectiveTracker.OnObjectiveClosed.Invoke(this._objective);
				AudioManager.am.PlayAudioEmitter(this._strAudioClose, false, false);
				CrewSim.bJustClickedInput = true;
			}
		}

		// Small coroutine used to restore the scroll position after closing.
		protected IEnumerator RepositionScrollRect()
		{
			yield return null;
			this.scrollRect.verticalNormalizedPosition = 1f;
			yield break;
		}

		// Removes shared-event listeners when the panel is destroyed.
		private void OnDestroy()
		{
			ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent.RemoveListener(new UnityAction<ObjectivePanel>(this.DelayedVisibilitySinglePanel));
			ObjectiveTracker.OnShowTutorialToggled.RemoveListener(new UnityAction<bool>(this.ShowTutorial));
		}

		// Clears world highlighting when the panel is hidden.
		private void OnDisable()
		{
			if (this._objective != null && this._objective.CO != null)
			{
				this._objective.CO.Highlight = false;
			}
		}

		// Binds the UI to one Objective model, including title/desc, icon art, and
		// the default focus link used by link-click helpers.
		public void SetData(Objective obj, bool showClose = true)
		{
			this._objective = obj;
			this._txtTitle.text = obj.strDisplayName;
			this._txtDescription.text = obj.strDisplayDesc;
			this._txtDescription.GetComponent<LinkOpener>().strDefaultURL = "coid:" + ((!(obj.COFocus != null)) ? string.Empty : obj.COFocus.strID);
			if (this._tutorialInfoIcon && !obj.bTutorial)
			{
				this._tutorialInfoIcon.SetActive(false);
			}
			string str = (!(obj.CO != null)) ? string.Empty : obj.CO.strPortraitImg;
			if (obj.COFocus != null)
			{
				str = obj.COFocus.strPortraitImg;
			}
			Texture2D texture = DataHandler.LoadPNG(str + ".png", false, false);
			this.ResizeTexture(this._imgIcon, texture);
			this.SetVisibility(showClose, obj.bTutorial && !ObjectiveTracker.ShowTutorials);
		}

		// Pulses the border while hovered, then eases it back to the base color.
		public void HighlightBorder()
		{
			if (!this.border)
			{
				return;
			}
			if (this.bMouseOver)
			{
				float num = Mathf.PingPong(Time.unscaledTime * 0.5f, 1f) + 0.5f;
				this.border.color = new Color(num, num, num, 1f);
			}
			else if (this.border.color != this._borderStartColor)
			{
				float r = this.border.color.r;
				float num2 = Mathf.Sign(r - this._borderStartColor.r);
				float num3 = r - Time.unscaledDeltaTime * 3f * num2;
				Color color = new Color(num3, num3, num3);
				if (Mathf.Sign(num3 - this._borderStartColor.r) != num2)
				{
					this.border.color = this._borderStartColor;
				}
				else
				{
					this.border.color = color;
				}
			}
		}

		public void ResizeForText()
		{
			float num = 250f;
			if (this.scrollRect)
			{
				num = (this.scrollRect.transform as RectTransform).sizeDelta.x;
			}
			bool flag = this._imgIcon.transform.parent.parent == base.transform;
			float b = (float)((!flag) ? 0 : 32);
			float num2 = 6f;
			float num3 = 6f;
			float num4 = 5f;
			float num5 = 2.5f;
			float num6 = 2f * num3 + 2f * num5 - 1f;
			float num7 = 2f * num2 - 0.5f;
			float num8 = 2f * num2;
			float num9 = 0f;
			if (this.showInfoIcon)
			{
				num7 += 50f;
				num9 -= 25f;
			}
			if (flag)
			{
				num7 += (this._imgIcon.transform.parent as RectTransform).sizeDelta.x;
				num9 += (this._imgIcon.transform.parent as RectTransform).sizeDelta.x / 2f + num4 / 2f;
			}
			float num10 = num;
			this._txtDescription.rectTransform.sizeDelta = new Vector2(num10 - num7, this._txtDescription.rectTransform.sizeDelta.y);
			this._txtTitle.rectTransform.sizeDelta = new Vector2(num10 - num8, this._txtTitle.rectTransform.sizeDelta.y);
			this._txtDescription.ForceMeshUpdate();
			this._txtTitle.ForceMeshUpdate();
			float num11 = num6 + this._txtTitle.preferredHeight + Mathf.Max(this._txtDescription.preferredHeight, b);
			if (flag)
			{
				this._imgIcon.transform.parent.localPosition = new Vector3(-num10 / 2f + (this._imgIcon.transform.parent as RectTransform).sizeDelta.x / 2f + 6f, num11 / 2f - this._txtTitle.preferredHeight - 10f - (this._imgIcon.transform.parent as RectTransform).sizeDelta.y / 2f);
			}
			this._txtTitle.rectTransform.localPosition = new Vector2(-num10 / 2f + this._txtTitle.rectTransform.sizeDelta.x / 2f + num2 + 1f, num11 / 2f - num3 - this._txtTitle.rectTransform.sizeDelta.y / 2f);
			this._txtDescription.rectTransform.localPosition = new Vector2(num9 + 1f, num11 / 2f - num3 - num5 - this._txtTitle.preferredHeight - this._txtDescription.rectTransform.sizeDelta.y / 2f);
			if (this.showInfoIcon && this._tutorialInfoIcon != null)
			{
				this._tutorialInfoIcon.transform.localPosition = new Vector3(this._tutorialInfoIcon.transform.localPosition.x, this._txtDescription.rectTransform.localPosition.y);
			}
			this.rectTransform.sizeDelta = new Vector2(num, num11);
			this._layoutElement.preferredHeight = num11;
			this.height = num11;
		}

		public IEnumerator Embiggen()
		{
			if (!this._layoutElement)
			{
				this._layoutElement = base.GetComponent<LayoutElement>();
			}
			float t = 0f;
			while (t < 1f)
			{
				this._layoutElement.preferredHeight = Mathf.SmoothStep(0f, this.height, t);
				t += Time.deltaTime * 5f;
				yield return null;
			}
			this._layoutElement.preferredHeight = this.height;
			if (this._cg)
			{
				this._cg.alpha = 1f;
			}
			this.ShowTutorial(ObjectiveTracker.ShowTutorials);
			yield break;
		}

		public virtual void SetData(Objective obj, float timer)
		{
			this.SetData(obj, false);
			this._realTimeStart = Time.realtimeSinceStartup;
			this._gameTimeStart = StarSystem.fEpoch;
			this._timer = timer;
			base.StartCoroutine(this.SelfDestruct());
		}

		public void SetData(Objective obj, UnityAction closeButtonCallback, bool flipImage)
		{
			this.SetData(obj, true);
			if (closeButtonCallback != null)
			{
				this._btnClose.onClick.RemoveAllListeners();
				this._btnClose.onClick.AddListener(closeButtonCallback);
				this._btnClose.onClick.AddListener(delegate()
				{
					AudioManager.am.PlayAudioEmitter(this._strAudioClose, false, false);
				});
			}
			if (flipImage)
			{
				this._btnClose.GetComponent<Image>().sprite = this._deleteImage;
			}
		}

		protected void SetVisibility(bool showClose, bool bTutorialDisable)
		{
			bool showTutorials = ObjectiveTracker.ShowTutorials;
			if (!showClose)
			{
				this._btnClose.gameObject.SetActive(false);
				this._btnClose.onClick.RemoveAllListeners();
			}
			if (this._cg != null)
			{
				if (showClose && !ObjectiveTracker.MuteObjectives)
				{
					this._cg.alpha = 0f;
					this._cg.blocksRaycasts = false;
					this._cg.interactable = false;
					if (bTutorialDisable)
					{
						this.ShowTutorial(false);
					}
					else
					{
						ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent.AddListener(new UnityAction<ObjectivePanel>(this.DelayedVisibilitySinglePanel));
						base.transform.SetParent(null);
					}
				}
				else
				{
					this._cg.alpha = 1f;
					this._cg.blocksRaycasts = true;
					this._cg.interactable = true;
					this.ShowTutorial(showTutorials);
				}
			}
		}

		public void CompleteObjective()
		{
			this._txtTitle.text = "Objective complete";
			string text = this._objective.strDisplayDescComplete;
			if (string.IsNullOrEmpty(text))
			{
				text = this._objective.strDisplayName;
			}
			this._txtDescription.text = text;
			this.showInfoIcon = false;
			if (this._tutorialInfoIcon != null && this._tutorialInfoIcon.gameObject.activeSelf)
			{
				this._tutorialInfoIcon.gameObject.SetActive(false);
			}
			this.ResizeForText();
			if (this._objective.InfoNodeToOpen != null && Info.instance.currentObjectiveNode == this._objective.InfoNodeToOpen)
			{
				Info.instance.currentObjectiveNode = null;
			}
			this._realTimeStart = Time.realtimeSinceStartup;
			this._timer = 2.5f;
			base.StartCoroutine(this.SelfDestruct());
		}

		public void Extend(float time)
		{
			this._timer = time;
			this._realTimeStart = Time.realtimeSinceStartup;
			this._gameTimeStart = StarSystem.fEpoch;
		}

		private void ShowTutorial(bool show)
		{
			if (this._objective == null || !this._objective.bTutorial || this._cg == null || this._layoutElement == null)
			{
				return;
			}
			this._layoutElement.ignoreLayout = !show;
			this._cg.alpha = (float)((!show) ? 0 : 1);
			this._cg.interactable = show;
			this._cg.blocksRaycasts = show;
		}

		protected void SyncCGState()
		{
			if (this._cg == null || this._cgparent == null)
			{
				return;
			}
			if (this._cgparent.alpha < 1f)
			{
				this._cg.blocksRaycasts = false;
				this._cg.interactable = false;
			}
			else if (this._cg.alpha > 0f)
			{
				this._cg.blocksRaycasts = true;
				this._cg.interactable = true;
			}
			if (this.isFlyIn)
			{
				this._cg.blocksRaycasts = false;
				this._cg.interactable = false;
			}
		}

		protected bool CheckLinks(TMP_Text txt)
		{
			int num = TMP_TextUtilities.FindIntersectingLink(txt, Input.mousePosition, CrewSim.objInstance.UICamera);
			bool result = false;
			if (num != -1)
			{
				TMP_LinkInfo tmp_LinkInfo = txt.textInfo.linkInfo[num];
				string linkID = tmp_LinkInfo.GetLinkID();
				if (!string.IsNullOrEmpty(linkID) && linkID.IndexOf("coid:") == 0)
				{
					CondOwner condOwner = null;
					DataHandler.mapCOs.TryGetValue(linkID.Replace("coid:", string.Empty), out condOwner);
					if (condOwner != null)
					{
						if (this.strLastHighlightCO != condOwner.strID)
						{
							foreach (string key in this._objective.aLinkedCOs)
							{
								CondOwner condOwner2 = null;
								if (DataHandler.mapCOs.TryGetValue(key, out condOwner2) && condOwner2 != condOwner && condOwner2.HighlightObjective)
								{
									condOwner2.HighlightObjective = false;
								}
							}
							if (this._objective.COFocus != null && this._objective.COFocus.HighlightObjective)
							{
								this._objective.COFocus.HighlightObjective = false;
							}
							condOwner.HighlightObjective = true;
							this.strLastHighlightCO = condOwner.strID;
							AudioManager.am.PlayAudioEmitter("UIObjectiveLinkMouseover", false, false);
						}
						result = true;
					}
				}
			}
			return result;
		}

		private void ResizeTexture(RawImage image, Texture2D texture)
		{
			float aspectRatio = (float)texture.width / (float)texture.height;
			image.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
			image.texture = texture;
		}

		protected IEnumerator SelfDestruct()
		{
			while (this._realTimeStart + this._timer > Time.realtimeSinceStartup || this._gameTimeStart + (double)this._timer > StarSystem.fEpoch)
			{
				yield return null;
			}
			UnityEngine.Object.Destroy(base.gameObject);
			yield break;
		}

		protected void DelayedVisibilitySinglePanel(ObjectivePanel objectivePanel)
		{
			if (objectivePanel.sidebarCounterpartToFlyin == base.transform)
			{
				base.transform.SetParent(this.scrollRect.content);
				this._cg.alpha = 0f;
				ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent.RemoveListener(new UnityAction<ObjectivePanel>(this.DelayedVisibilitySinglePanel));
				base.StartCoroutine(this.Embiggen());
				if (objectivePanel.Objective.InfoNodeToOpen != null)
				{
					Info.instance.currentObjectiveNode = objectivePanel.Objective.InfoNodeToOpen;
				}
				if (!ObjectiveTracker.MuteInfoModalTutorials)
				{
					if (objectivePanel.Objective.strDisplayName == "Unpause World")
					{
						Info.instance.OpenToNode("TutorialObjectives");
						return;
					}
					if (objectivePanel.Objective.InfoNodeToOpen != null)
					{
						Info.instance.OpenToNode(objectivePanel.Objective.InfoNodeToOpen);
						CrewSim.TriggerAutoPause(null);
					}
				}
			}
		}

		protected void FocusObjective(CondOwner co)
		{
			if (ObjectivePanel.OnFocusObjective != null)
			{
				ObjectivePanel.OnFocusObjective.Invoke(this.Objective);
			}
			if (co == null || co.ship == null || co.ship.LoadState < Ship.Loaded.Edit)
			{
				return;
			}
			CrewSim.objInstance.CamCenter(co);
		}

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			this.bMouseOver = true;
			if (this.sidebar)
			{
				this.sidebar.mousedOverPanel = this;
			}
			if (this.bg)
			{
				this.bg.color = this._bgColorMouseover;
			}
			if (this._cg != null && this._cg.alpha < 1f)
			{
				return;
			}
			if (this._objective == null || this._objective.CO == null)
			{
				return;
			}
			this._objective.Highlight = true;
			ObjectivePanel.OnHighlightObjective.Invoke(this._objective);
			AudioManager.am.PlayAudioEmitterAtVol("UIObjectiveLinkMouseover", false, false, 0.6f);
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			if (this.sidebar && this.sidebar.mousedOverPanel == this)
			{
				this.sidebar.mousedOverPanel = null;
			}
			this.bMouseOver = false;
			if (this.bg)
			{
				this.bg.color = this._bgStartColor;
			}
			if (this._objective != null && this._objective.CO != null)
			{
				this._objective.Highlight = false;
			}
		}

		private Camera WorldCam
		{
			get
			{
				if (this._cam == null)
				{
					this._cam = base.GetComponentInParent<Canvas>().worldCamera;
				}
				return this._cam;
			}
		}

		public static OnObjectiveUpdatedEvent OnHighlightObjective;

		public static OnObjectiveFocusEvent OnFocusObjective;

		[Header("Required Parameters")]
		[SerializeField]
		protected TextMeshProUGUI _txtTitle;

		[SerializeField]
		protected TextMeshProUGUI _txtDescription;

		[SerializeField]
		public GameObject _tutorialInfoIcon;

		[SerializeField]
		private RawImage _imgIcon;

		[SerializeField]
		protected Button _btnClose;

		[SerializeField]
		private LayoutElement _layoutElement;

		[SerializeField]
		public CanvasGroup _cg;

		[SerializeField]
		private RawImage bg;

		[SerializeField]
		private Image border;

		[SerializeField]
		private Image _imgHighlight;

		[SerializeField]
		private Sprite _deleteImage;

		[SerializeField]
		protected string _strAudioClose;

		protected Camera _cam;

		protected string strLastHighlightCO;

		protected bool bMouseOver;

		protected int initialCanvasSortOrder;

		protected Objective _objective;

		protected float _timer;

		protected double _gameTimeStart;

		protected float _realTimeStart;

		protected bool _desiredCGState;

		protected CanvasGroup _cgparent;

		protected Color _borderStartColor;

		protected Color _borderColorMouseover = Color.HSVToRGB(0f, 0f, 1f);

		private Color _bgStartColor = Color.HSVToRGB(0f, 0f, 0.043137256f);

		private Color _bgColorMouseover = Color.HSVToRGB(0f, 0f, 0.09803922f);

		[HideInInspector]
		public float headOfQueueTimer;

		[HideInInspector]
		public Transform sidebarCounterpartToFlyin;

		[HideInInspector]
		public bool isFlyIn;

		[HideInInspector]
		public bool showInfoIcon;

		[NonSerialized]
		public ScrollRect scrollRect;

		[NonSerialized]
		public ObjectivesHost sidebar;

		private RectTransform _rect;

		public float height;
	}
}
