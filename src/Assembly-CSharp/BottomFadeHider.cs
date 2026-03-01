using System;
using UnityEngine;

public class BottomFadeHider : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	public void Fade(Vector2 amount)
	{
		if (amount.y > 0f)
		{
			base.gameObject.SetActive(true);
		}
		else
		{
			base.gameObject.SetActive(false);
		}
	}

	public void Fade(float amount)
	{
		if (amount > 0f)
		{
			base.gameObject.SetActive(true);
		}
		else
		{
			base.gameObject.SetActive(false);
		}
	}
}
