using System;
using System.Collections.Generic;
using TMPro;

public class GUITicket : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.lblValid = base.transform.Find("lblValid").GetComponent<TextMeshProUGUI>();
		this.lblDate = base.transform.Find("lblDate").GetComponent<TextMeshProUGUI>();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		double num = 0.0;
		string s = null;
		if (!this.dictPropMap.TryGetValue("fEpochExpire", out s) || !double.TryParse(s, out num))
		{
			if (this.COSelf.HasCond("IsHoursLeft"))
			{
				Condition condition = this.COSelf.mapConds["IsHoursLeft"];
				num = (condition.fCount - 1.0) * 3600.0;
				double tickerTimeleft = this.COSelf.GetTickerTimeleft("PermitOKLGSalvage");
				num += tickerTimeleft * 3600.0;
				num += StarSystem.fEpoch;
			}
			else
			{
				num = StarSystem.fEpoch - (double)MathUtils.Rand(3600f, 31556926f, MathUtils.RandType.Flat, null);
			}
		}
		base.SetPropMapData("fEpochExpire", num.ToString());
		this.lblDate.text = MathUtils.GetUTCFromS(num);
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	private TMP_Text lblValid;

	private TMP_Text lblDate;
}
