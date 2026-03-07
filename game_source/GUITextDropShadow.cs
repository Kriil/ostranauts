using System;
using TMPro;
using UnityEngine;

public class GUITextDropShadow : MonoBehaviour
{
	private void Awake()
	{
		this.textShadow = base.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		this.textBase = base.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public TextMeshProUGUI textShadow;

	public TextMeshProUGUI textBase;
}
