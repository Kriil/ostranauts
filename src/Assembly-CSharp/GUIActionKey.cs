using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIActionKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IEventSystemHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
		if (!CrewSim.Typing)
		{
			if (Input.GetKeyDown(KeyCode.Mouse1))
			{
				if (this.command.currentCombos.Count > 0)
				{
					this.keySelector.RemoveCombo(this.command.currentCombos.Count - 1, this);
				}
				this.SetKeyText(this.command.KeyName);
			}
			else
			{
				CrewSim.Typing = true;
				base.StartCoroutine("ListenForKeyChange");
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		this.highlight.alpha = 0.05f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.highlight.alpha = 0f;
	}

	private void Awake()
	{
		this.keySelector = base.transform.parent.GetComponent<GUIActionKeySelector>();
		this.actionLabel = base.transform.Find("UserActionLabel").GetComponent<TextMeshProUGUI>();
		this.input = base.transform.Find("KeyBackgroundImage/Input").GetComponent<TextMeshProUGUI>();
		this.highlight = base.transform.Find("Highlight").GetComponent<CanvasGroup>();
		this.highlight.alpha = 0f;
	}

	public void SetKeyText(string keyName)
	{
		this.input.text = keyName.Replace("Alpha", string.Empty);
	}

	public void Reset()
	{
		if (this.keySelector == null)
		{
			return;
		}
		while (this.command.currentCombos.Count > 0)
		{
			this.keySelector.RemoveCombo(this.command.currentCombos.Count - 1, this);
		}
		this.SetKeyText(this.command.KeyName);
	}

	public IEnumerator ListenForKeyChange()
	{
		List<KeyCode> pressedKeys = new List<KeyCode>();
		this.SetKeyText(string.Empty);
		yield return null;
		bool keyChanged = false;
		List<KeyCode> KeyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToList<KeyCode>();
		KeyCodes.Remove(KeyCode.Mouse0);
		KeyCodes.Remove(KeyCode.Mouse1);
		while (!keyChanged)
		{
			if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
			{
				this.SetKeyText(this.command.KeyName);
				keyChanged = true;
			}
			else
			{
				foreach (KeyCode keyCode in KeyCodes)
				{
					if (Input.GetKeyDown(keyCode))
					{
						pressedKeys.Add(keyCode);
					}
					if (Input.GetKeyUp(keyCode) && pressedKeys.Contains(keyCode))
					{
						keyChanged = true;
						break;
					}
				}
				string keyname = string.Empty;
				foreach (KeyCode keyCode2 in pressedKeys)
				{
					keyname += keyCode2.ToString();
					keyname += " + ";
				}
				if (keyname == string.Empty)
				{
					this.SetKeyText("<i>type keys to setup. or click to cancel</i>");
				}
				else
				{
					this.SetKeyText(keyname);
				}
				yield return null;
			}
		}
		this.AttemptKeyChange(pressedKeys);
		yield break;
	}

	public void AttemptKeyChange(List<KeyCode> combo)
	{
		if (!CrewSim.Typing)
		{
			this.SetKeyText(this.command.KeyName);
			return;
		}
		CrewSim.Typing = false;
		if (combo.Count > 0)
		{
			this.keySelector.AddCombo(combo, this);
			this.SetKeyText(this.command.KeyName);
		}
	}

	public GUIActionKeySelector keySelector;

	public TextMeshProUGUI actionLabel;

	public TextMeshProUGUI keyLabel;

	public CanvasGroup highlight;

	public TextMeshProUGUI input;

	public Command command;

	public bool runOnFrame;
}
