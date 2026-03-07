using System;

[Serializable]
public class JsonItemAnimation
{
	public string strName { get; set; }

	public int nFrameCount { get; set; }

	public string strFrameRate { get; set; }

	public bool bLoop { get; set; }

	public bool bRandomStartingFrame { get; set; }

	public int nSheetColumns { get; set; }

	public int nSheetRows { get; set; }
}
