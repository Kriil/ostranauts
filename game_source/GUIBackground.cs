using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIBackground : MonoBehaviour
{
	private void Start()
	{
		this.SetOffset(-1);
	}

	private void Awake()
	{
	}

	private void Update()
	{
		if (this.toggle)
		{
			this.toggle = false;
			this.SetOffset(UnityEngine.Random.Range(0, 1000));
		}
	}

	private void SetOffset(int nSeed = -1)
	{
		int num = UnityEngine.Random.Range(0, 1000);
		int num2 = UnityEngine.Random.state.GetHashCode();
		CondOwner condOwner = null;
		GUIData componentInParent = base.gameObject.GetComponentInParent<GUIData>();
		if (componentInParent != null)
		{
			condOwner = componentInParent.COSelf;
		}
		if (nSeed >= 0)
		{
			UnityEngine.Random.InitState(nSeed);
		}
		else if (condOwner != null && condOwner.strID != null)
		{
			UnityEngine.Random.InitState(condOwner.strID.GetHashCode());
		}
		else
		{
			UnityEngine.Random.InitState(0);
		}
		List<Texture> list = new List<Texture>(this.m_textures);
		foreach (GameObject gameObject in this.m_layers)
		{
			Image component = gameObject.GetComponent<Image>();
			Material material = new Material(component.material);
			if (this.m_textures.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				material.mainTexture = list[index];
				if (this.m_exclusive)
				{
					list.RemoveAt(index);
				}
			}
			if (this.m_colors.Count > 0)
			{
				int index2 = UnityEngine.Random.Range(0, this.m_colors.Count);
				material.color = this.m_colors[index2];
			}
			float num3 = UnityEngine.Random.Range(0f, 1f);
			gameObject.SetActive(num3 > this.m_chanceEmpty);
			float num4 = 1f;
			float num5 = 1f;
			if (this.m_scaleTexture)
			{
				num4 = (float)material.mainTexture.width / 1024f;
				num5 = (float)material.mainTexture.height / 1024f;
			}
			if (this.m_wrappable)
			{
				material.mainTextureOffset = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
				float num6 = UnityEngine.Random.Range(this.m_scaleRange.x / num4, this.m_scaleRange.y / num5);
				float num7 = UnityEngine.Random.Range(this.m_ratioRange.x, this.m_ratioRange.y);
				material.mainTextureScale = new Vector2(num6 * num7, num6);
			}
			component.material = material;
			if (this.m_rotate)
			{
				gameObject.transform.Rotate(new Vector3(0f, 0f, UnityEngine.Random.Range(this.m_rotateRange.x, this.m_rotateRange.y)));
			}
		}
		UnityEngine.Random.InitState(Time.renderedFrameCount + num2);
		for (int i = 0; i < num; i++)
		{
			num2 = (int)UnityEngine.Random.value;
		}
	}

	public bool m_wrappable = true;

	public bool m_scaleTexture = true;

	public bool m_exclusive;

	public bool m_rotate;

	public bool toggle;

	public Vector2 m_scaleRange = new Vector2(0.15f, 1f);

	public Vector2 m_ratioRange = new Vector2(1.7f, 2.2f);

	public Vector2 m_rotateRange = new Vector2(0f, 360f);

	public float m_chanceEmpty = 0.45f;

	public List<Color> m_colors = new List<Color>();

	public List<Texture> m_textures = new List<Texture>();

	public List<GameObject> m_layers = new List<GameObject>();
}
