using System;
using UnityEngine;
using UnityEngine.UI;

public class GUIBaseAssetLoader : MonoBehaviour
{
	private void Start()
	{
		UnityEngine.Object.DestroyImmediate(base.GetComponent<Image>());
		this.image = base.gameObject.AddComponent<RawImage>();
		Texture2D texture2D = DataHandler.LoadPNG(this.PNGToLoad + ".png", false, false);
		if (texture2D == null)
		{
			return;
		}
		this.image.texture = texture2D;
		this.image.color = new Color32(74, 82, 55, byte.MaxValue);
	}

	private void Update()
	{
	}

	public RawImage image;

	public string PNGToLoad;
}
