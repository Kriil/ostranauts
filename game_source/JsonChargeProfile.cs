using System;

// Charge/consumable usage profile for powered tools or battery-like interactions.
// This appears to describe which condition is consumed, what damage is applied,
// and what replacement item trigger is required when the charge is used.
public class JsonChargeProfile
{
	// `strName` is the profile id; `strCondName` is likely the tracked charge condition.
	public string strName { get; set; }

	public string strCondName { get; set; }

	public float fCondAmount { get; set; }

	public float fDmgAmountUs { get; set; }

	public float fDmgAmountCharge { get; set; }

	public string strItemCT { get; set; }

	// Quantity and behavior flags for how charge items are consumed.
	public int nItemAmount { get; set; }

	public bool bSkipRemove { get; set; }

	public bool bUseContained { get; set; }

	public bool bUseSelf { get; set; }

	// Resolves and caches the item condition trigger referenced by `strItemCT`.
	// This likely points at a `data/condtrigs` id used to find compatible charge items.
	public CondTrigger CTItem()
	{
		if (this._ctItem != null)
		{
			return this._ctItem;
		}
		this._ctItem = DataHandler.GetCondTrigger(this.strItemCT);
		return this._ctItem;
	}

	private CondTrigger _ctItem;
}
