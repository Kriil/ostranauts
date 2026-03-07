using System;

[Serializable]
public class JsonRoom
{
	public string strID { get; set; }

	public bool bVoid { get; set; }

	public int[] aTiles { get; set; }

	public string roomSpec { get; set; }

	public double roomValue { get; set; }

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strID,
			"; tiles: ",
			this.aTiles.Length,
			"; void: ",
			this.bVoid
		});
	}
}
