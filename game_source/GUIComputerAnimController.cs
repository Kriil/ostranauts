using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIComputerAnimController : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public void Init(GUIComputer cm)
	{
		this.rect = base.GetComponent<RectTransform>();
		this.computerManager = cm;
		this.computerManager.rectControllers.Add(this.rect, this);
		this.cg = base.GetComponent<CanvasGroup>();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (this.animState == ComputerAnimState.BOUNCING)
		{
			this.animState = ComputerAnimState.NONE;
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
		ComputerAnimState computerAnimState = this.animState;
		switch (computerAnimState)
		{
		case ComputerAnimState.NONE:
			break;
		default:
			if (computerAnimState == ComputerAnimState.BOUNCING)
			{
				this.computerManager.animatingRectControllers[this.rect] = this;
				Vector3 normalized = (this.destination - base.transform.localPosition).normalized;
				base.transform.localPosition += (this.destination - base.transform.localPosition).normalized * this.moveSpeed * Time.deltaTime;
				if ((this.destination - base.transform.localPosition).magnitude < 1f)
				{
					base.transform.localPosition = this.destination;
					this.destination = new Vector3(UnityEngine.Random.Range(-800f, 800f), UnityEngine.Random.Range(-500f, 500f));
				}
			}
			break;
		case ComputerAnimState.MOVETO:
			this.computerManager.animatingRectControllers[this.rect] = this;
			base.transform.localPosition += (this.destination - base.transform.localPosition).normalized * this.moveSpeed * Time.deltaTime;
			if ((this.destination - base.transform.localPosition).magnitude < 1f)
			{
				base.transform.localPosition = this.destination;
				this.computerManager.NotifyAnimationComplete(this.rect);
			}
			break;
		case ComputerAnimState.LERPTO:
			this.computerManager.animatingRectControllers[this.rect] = this;
			base.transform.localPosition += (this.destination - base.transform.localPosition) * this.lerpRatio;
			if ((this.destination - base.transform.localPosition).magnitude < 1f)
			{
				base.transform.localPosition = this.destination;
				this.computerManager.NotifyAnimationComplete(this.rect);
			}
			break;
		case ComputerAnimState.BLINK:
			this.computerManager.animatingRectControllers[this.rect] = this;
			if (this.cg.alpha == 1f)
			{
				this.blinkOn = false;
			}
			else if (this.cg.alpha == 0f)
			{
				this.blinkOn = true;
			}
			if (this.blinkOn)
			{
				this.cg.alpha += (1f + this.cg.alpha) * 0.9f * Time.deltaTime;
				if (this.cg.alpha > 1f)
				{
					this.cg.alpha = 1f;
				}
			}
			else
			{
				this.cg.alpha -= (1f + this.cg.alpha) * 0.9f * Time.deltaTime;
				if ((double)this.cg.alpha < 0.01)
				{
					this.cg.alpha = 0f;
				}
			}
			break;
		case ComputerAnimState.COUNTDOWN:
			this.computerManager.animatingRectControllers[this.rect] = this;
			this.countdown -= Time.deltaTime;
			if (this.countdown <= 0f)
			{
				CountdownEnd countdownEnd = this.countdownEnd;
				if (countdownEnd == CountdownEnd.SHOW)
				{
					this.cg.alpha = 1f;
				}
				this.computerManager.NotifyAnimationComplete(this.rect);
			}
			break;
		}
	}

	public GUIComputer computerManager;

	public ComputerAnimState animState;

	public RectTransform rect;

	public CanvasGroup cg;

	public float moveSpeed;

	public float lerpRatio;

	public bool blinkOn;

	public float blinkPauseTop;

	public float blinkPauseBottom;

	public Vector3 destination;

	public float countdown;

	public CountdownEnd countdownEnd;
}
