using System;
using System.Collections.Generic;
using UnityEngine;

public class TextSizeManager : MonoBehaviour
{
	private void Start()
	{
		this.Init();
	}

	private void Update()
	{
	}

	public void Init()
	{
		if (TextSizeManager.m_instance == null)
		{
			TextSizeManager.m_instance = this;
			TextSizeManager.m_sizes = new Dictionary<string, float>();
		}
		else
		{
			UnityEngine.Object.Destroy(base.transform.gameObject);
		}
	}

	public static void Load()
	{
	}

	public int GetSize(string key)
	{
		int num = TextSizeManager.m_baseSize;
		if (TextSizeManager.m_sizes.ContainsKey(key) && TextSizeManager.m_sizes[key] != 0f)
		{
			num = MathUtils.RoundToInt((float)num * TextSizeManager.m_sizes[key]);
		}
		if (num < TextSizeManager.m_minSize)
		{
			num = TextSizeManager.m_minSize;
		}
		return num;
	}

	public static TextSizeManager m_instance;

	public static int m_minSize = 9;

	public static int m_baseSize = 16;

	public static Dictionary<string, float> m_sizes;
}
