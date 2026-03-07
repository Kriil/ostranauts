using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GUIFontNoAA : MonoBehaviour
{
	private void Start()
	{
		base.GetComponent<Text>().font.material.mainTexture.filterMode = FilterMode.Point;
	}

	private void Update()
	{
	}
}
