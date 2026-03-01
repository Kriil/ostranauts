using System;

// Small advertisement or notice payload.
// Likely used for ambient UI listings, bulletin boards, or rotating flavor text.
public class JsonAd
{
	// `strName` is the internal ad id/title; `strDesc` is the display body text.
	public string strName { get; set; }

	public string strDesc { get; set; }

	// Keeps dropdowns/debug output compact by showing the ad id.
	public override string ToString()
	{
		return this.strName;
	}
}
