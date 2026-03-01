using System;
using UnityEngine;
using UnityEngine.UI;

public class DatafileRow : MonoBehaviour
{
	private void Awake()
	{
	}

	public void SetFileIcon(Image bmp)
	{
		this.bmpIcon.sprite = bmp.sprite;
	}

	public void Tint(Color color)
	{
		this.bmpIconBG.color = color;
	}

	public override string ToString()
	{
		return this.strName;
	}

	public string strName;

	public string strCOID;

	public Toggle chk;

	public Image bmpIcon;

	public Image bmpIconBG;
}
