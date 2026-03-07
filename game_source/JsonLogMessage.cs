using System;

// Serializable message-log entry for a CondOwner's history feed.
// Likely used for crew/item logs shown in UI and persisted in JsonCondOwnerSave.
[Serializable]
public class JsonLogMessage
{
	// `strName` appears to be the message/template key, while `strMessage` is the rendered text.
	public string strName { get; set; }

	public string strMessage { get; set; }

	public string strColor { get; set; }

	public string strOwner { get; set; }

	public double fTime { get; set; }

	public string strThem { get; set; }

	// Creates a detached copy so log UIs/saves can duplicate entries safely.
	public JsonLogMessage Clone()
	{
		return new JsonLogMessage
		{
			strName = this.strName,
			strMessage = this.strMessage,
			strColor = this.strColor,
			strOwner = this.strOwner,
			fTime = this.fTime
		};
	}
}
