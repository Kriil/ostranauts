using System;
// FFU_BR_Extended adds extra runtime/data parameters to the `JsonShipSpec` contract.
// These fields back the new wiki-documented parameters such as inventory effects,
// slot ordering, room lookup, and same-ship targeting helpers.
public class patch_JsonShipSpec : JsonShipSpec
{
	public int nIsSameShipCO { get; set; }
	public extern bool orig_Matches(Ship ship, CondOwner coUs = null);
	public bool Matches(Ship ship, CondOwner coUs = null)
	{
		bool flag = this.orig_Matches(ship, coUs);
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = this.nIsSameShipCO != 0;
			if (flag3)
			{
				bool flag4 = coUs == null;
				if (flag4)
				{
					return false;
				}
				bool flag5 = !base.IntMatchesBool(this.nIsSameShipCO, coUs.ship == ship);
				if (flag5)
				{
					return false;
				}
			}
		}
		return flag;
	}
}
