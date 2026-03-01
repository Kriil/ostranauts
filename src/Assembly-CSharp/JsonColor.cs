using System;
using UnityEngine;

[Serializable]
// Named RGBA color entry used by data-driven UI, overlays, and item tinting.
// Likely loaded into a color registry so JSON can refer to colors by `strName`.
public class JsonColor
{
	// Registry key for the color entry.
	public string strName { get; set; }

	public int nR { get; set; }

	public int nG { get; set; }

	public int nB { get; set; }

	public int nA { get; set; }

	// Converts 0-255 channel integers into a Unity Color.
	public Color GetColor()
	{
		Color result = new Color((float)this.nR / 255f, (float)this.nG / 255f, (float)this.nB / 255f, (float)this.nA / 255f);
		return result;
	}

	// Keeps logs/debug output readable by returning the color id.
	public override string ToString()
	{
		return this.strName;
	}
}
