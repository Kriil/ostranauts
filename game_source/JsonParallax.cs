using System;
using Parallax;

public class JsonParallax
{
	public string strName { get; set; }

	public int nLayers { get; set; }

	public float fLayerScaleFactor { get; set; }

	public float fDistortion { get; set; }

	public float fRateX { get; set; }

	public float fRateY { get; set; }

	public string strPattern { get; set; }

	public string strLootSpriteList { get; set; }

	public string[] aSunLights { get; set; }

	public Pattern Pattern()
	{
		if (!Enum.IsDefined(typeof(Pattern), this.strPattern))
		{
			return Parallax.Pattern.RoundRobin;
		}
		return (Pattern)Enum.Parse(typeof(Pattern), this.strPattern);
	}
}
