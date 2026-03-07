using System;
using UnityEngine;

[Serializable]
public class JsonLight
{
	public string strName { get; set; }

	public string strColor { get; set; }

	public string strImg { get; set; }

	public Vector2 ptPos { get; set; }

	public float fRadius { get; set; }

	public bool bIsNotification { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
