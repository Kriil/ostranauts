using System;
using UnityEngine;

public static class ShaderSetup
{
	public static void SetupMaterialWithBlendMode(Material material, ShaderSetup.BlendMode blendMode)
	{
		switch (blendMode)
		{
		case ShaderSetup.BlendMode.Opaque:
			material.SetOverrideTag("RenderType", string.Empty);
			material.SetInt("_SrcBlend", 1);
			material.SetInt("_DstBlend", 0);
			material.SetInt("_ZWrite", 1);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = -1;
			break;
		case ShaderSetup.BlendMode.Cutout:
			material.SetOverrideTag("RenderType", "TransparentCutout");
			material.SetInt("_SrcBlend", 1);
			material.SetInt("_DstBlend", 0);
			material.SetInt("_ZWrite", 1);
			material.EnableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 2450;
			break;
		case ShaderSetup.BlendMode.Fade:
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3000;
			break;
		case ShaderSetup.BlendMode.Transparent:
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", 1);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3000;
			break;
		}
	}

	public static Texture2D NormalPNGtoDXTnm(Texture2D texIn)
	{
		Texture2D texture2D = new Texture2D(texIn.width, texIn.height, TextureFormat.ARGB32, false);
		texture2D.name = "NormalPNGtoDXTnm " + texIn.name;
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Color color = default(Color);
		for (int i = 0; i < texIn.width; i++)
		{
			for (int j = 0; j < texIn.height; j++)
			{
				color.r = 1f - texIn.GetPixel(i, j).g;
				color.g = color.r;
				color.b = color.r;
				color.a = texIn.GetPixel(i, j).r;
				texture2D.SetPixel(i, j, color);
			}
		}
		texture2D.Apply();
		return texture2D;
	}

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,
		Transparent
	}
}
