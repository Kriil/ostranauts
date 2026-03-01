using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Reusable rotary control widget. Used by ship panels to cycle through a small
// set of states via clicks or drag "twiddling".
public class GUIKnob : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
{
	// Loads the initial sprite for the current knob state.
	private void Awake()
	{
		this.bmp = base.GetComponent<Image>();
		this.bmp.sprite = this.aStates[this.nState];
	}

	// Unclear: reserved update hook; this widget currently reacts only to pointer input.
	private void Update()
	{
	}

	// Resets the temporary drag-tilt visual back to neutral.
	public void ResetImageTilt()
	{
		this.bmp.gameObject.transform.rotation = Quaternion.Euler(new Vector2(0f, 0f));
	}

	// Drag loop: tracks mouse movement and turns the knob once the drag crosses
	// the configured switch threshold.
	public IEnumerator detectKnobTwiddle()
	{
		while (Input.GetMouseButton(0))
		{
			float fDX = Input.mousePosition.x - this.fStartDragX;
			float fDY = Input.mousePosition.y - this.fStartDragY;
			if (Mathf.Abs(fDX) >= 0.08f || Mathf.Abs(fDY) >= 0.08f)
			{
				this.bTwiddled = true;
				float num = -fDX - fDY;
				this.bmp.gameObject.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -fDX - fDY));
				if (num < -this.fSwitchoverPoint)
				{
					this.ResetImageTilt();
					this.RotateCW();
					this.fStartDragX = Input.mousePosition.x;
					this.fStartDragY = Input.mousePosition.y;
				}
				else if (num > this.fSwitchoverPoint)
				{
					this.ResetImageTilt();
					this.RotateCCW();
					this.fStartDragX = Input.mousePosition.x;
					this.fStartDragY = Input.mousePosition.y;
				}
			}
			yield return null;
		}
		yield break;
	}

	// Starts drag tracking so the knob can be twiddled instead of clicked.
	public void OnPointerDown(PointerEventData eventData)
	{
		this.bTwiddled = false;
		this.fStartDragX = Input.mousePosition.x;
		this.fStartDragY = Input.mousePosition.y;
		base.StartCoroutine("detectKnobTwiddle");
	}

	// Applies a click-turn when no drag occurred, otherwise just clears the tilt.
	public void OnPointerUp(PointerEventData eventData)
	{
		if (!this.bTwiddled)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				this.RotateCCW();
				return;
			}
			if (eventData.button == PointerEventData.InputButton.Right)
			{
				this.RotateCW();
				return;
			}
		}
		else
		{
			this.ResetImageTilt();
		}
	}

	// Click handling is performed in OnPointerUp; this interface hook is unused.
	public void OnPointerClick(PointerEventData eventData)
	{
	}

	private void RotateCW()
	{
		this.State++;
	}

	private void RotateCCW()
	{
		this.State--;
	}

	// Optional callback invoked whenever the state changes.
	public Action<int> Callback
	{
		get
		{
			return this.actCallback;
		}
		set
		{
			this.actCallback = value;
		}
	}

	// Changes state without playing the change sound.
	public void SetStateSilent(int state)
	{
		this.playAudio = false;
		this.State = state;
		this.playAudio = true;
	}

	// Current discrete knob state, clamped/wrapped to the available sprites.
	public int State
	{
		get
		{
			return this.nState;
		}
		set
		{
			int num = this.nState;
			this.nState = value;
			if (this.nState > this.aStates.Length - 1)
			{
				if (this.bWrap)
				{
					this.nState = 0;
				}
				else
				{
					this.nState = this.aStates.Length - 1;
				}
			}
			if (this.nState < 0)
			{
				if (this.bWrap)
				{
					this.nState = this.aStates.Length - 1;
				}
				else
				{
					this.nState = 0;
				}
			}
			this.bmp.sprite = this.aStates[this.nState];
			if (this.nState != num)
			{
				if (this.actCallback != null)
				{
					this.actCallback(this.nState);
				}
				if (this.strAudioEmitterChange != null && this.playAudio)
				{
					AudioManager.am.PlayAudioEmitter(this.strAudioEmitterChange, false, false);
				}
			}
		}
	}

	public Sprite[] aStates;

	public bool bWrap;

	public string strAudioEmitterChange = "ShipUIKnobFocus";

	private int nState;

	private Image bmp;

	private float fSwitchoverPoint = 30f;

	private Action<int> actCallback;

	private float fStartDragX;

	private float fStartDragY;

	private bool bTwiddled;

	private bool playAudio;
}
