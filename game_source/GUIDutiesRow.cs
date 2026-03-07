using System;
using TMPro;
using UnityEngine;

public class GUIDutiesRow : MonoBehaviour
{
	private void Awake()
	{
		this.aItems = new GUIDutiesItem[JsonCompanyRules.aDutiesNew.Length - 1];
		this.txtName = base.transform.Find("txtName").GetComponent<TMP_Text>();
	}

	public void SetOwner(string strName, JsonCompanyRules jRules)
	{
		this.jRules = jRules;
		this.txtName.text = strName;
		Transform parent = base.transform.Find("pnlItems");
		GUIDutiesItem original = Resources.Load<GUIDutiesItem>("GUIShip/GUIDuties/GUIDutiesItem");
		bool flag = this.aItems[0] == null;
		for (int i = 0; i < this.aItems.Length; i++)
		{
			if (flag)
			{
				this.aItems[i] = UnityEngine.Object.Instantiate<GUIDutiesItem>(original, parent);
				this.aItems[i].txtLabel.text = i.ToString();
				this.aItems[i].onChange = new Action<int, int>(this.UpdatePriority);
			}
			this.aItems[i].nItemID = i + 1;
			this.aItems[i].SetPriority(jRules.aDutyLvls[this.aItems[i].nItemID]);
		}
	}

	private void UpdatePriority(int nItemID, int nPriority)
	{
		if (this.jRules != null)
		{
			this.jRules.aDutyLvls[nItemID] = nPriority;
		}
	}

	public GUIDutiesItem[] aItems;

	public TMP_Text txtName;

	public JsonCompanyRules jRules;
}
