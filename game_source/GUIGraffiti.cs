using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIGraffiti : MonoBehaviour
{
	private void Start()
	{
		if (base.transform.childCount == 0)
		{
			this.SetUp();
		}
	}

	private void Awake()
	{
		if (base.transform.childCount == 0)
		{
			this.SetUp();
		}
	}

	private void Update()
	{
		if (this.toggle)
		{
			this.toggle = false;
			this.SetUp();
		}
	}

	public void SetUp()
	{
		int num = UnityEngine.Random.Range(0, 1000);
		int num2 = UnityEngine.Random.state.GetHashCode();
		int num3 = 0;
		if (CrewSim.shipCurrentLoaded != null)
		{
			num3 = CrewSim.shipCurrentLoaded.strRegID.GetHashCode();
		}
		if (CrewSim.coPlayer != null)
		{
			num3 += CrewSim.coPlayer.strName.GetHashCode();
		}
		num3 += base.gameObject.name.GetHashCode();
		num3 += this.m_seed;
		UnityEngine.Random.InitState(num3);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform);
		Image component = gameObject.GetComponent<Image>();
		component.sprite = this.m_sprites[UnityEngine.Random.Range(0, this.m_sprites.Count)];
		component.preserveAspect = true;
		RectTransform component2 = gameObject.GetComponent<RectTransform>();
		component2.sizeDelta = new Vector2(UnityEngine.Random.Range(this.m_sizeMin.x, this.m_sizeMax.x), UnityEngine.Random.Range(this.m_sizeMin.y, this.m_sizeMax.y));
		Vector2 a = new Vector2(1f - component2.sizeDelta.x, 1f - component2.sizeDelta.y);
		Vector2 vector = new Vector2(UnityEngine.Random.Range(0f, a.x), UnityEngine.Random.Range(0f, a.y));
		component2.anchorMin = vector;
		component2.anchorMax = new Vector2(1f, 1f) - (a - vector);
		component2.Rotate(0f, 0f, UnityEngine.Random.Range(0f, 360f));
		UnityEngine.Random.InitState(Time.renderedFrameCount + num2);
		for (int i = 0; i < num; i++)
		{
			num2 = (int)UnityEngine.Random.value;
		}
	}

	public bool toggle;

	public int m_seed;

	public Vector2 m_sizeMin = new Vector2(1f, 1f);

	public Vector2 m_sizeMax = new Vector2(1f, 1f);

	public Vector2 m_pos = new Vector2(0f, 0f);

	public float m_rot;

	public List<Sprite> m_sprites = new List<Sprite>();

	public GameObject m_prefab;
}
