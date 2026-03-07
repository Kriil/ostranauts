using System;

public class JsonModInfo
{
	public JsonModInfo()
	{
		this.Status = GUIModRow.ModStatus.Loaded;
	}

	public string strName { get; set; }

	public string strAuthor { get; set; }

	public string strModURL { get; set; }

	public string strGameVersion { get; set; }

	public string strModVersion { get; set; }

	public string strNotes { get; set; }

	public GUIModRow.ModStatus Status { get; set; }
}
