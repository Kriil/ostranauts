using System;
using System.Text;

namespace Ostranauts.Social.Models
{
	public class Bribe : SocialStakes
	{
		public Bribe()
		{
			this.strContext = "ACTPoliceShakedownThem";
			JsonContext context = DataHandler.GetContext(this.strContext);
			if (context != null)
			{
				this.defaultTitle = context.strTitle;
				this.defaultBody = context.strMainText;
			}
		}

		public override void UpdateUs(CondOwner objThem)
		{
			if (objThem == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(this.defaultBody);
			double condAmount = objThem.GetCondAmount("PoliceBribe");
			if (condAmount > 0.0)
			{
				stringBuilder.Append("Bribe Amount: $");
				stringBuilder.AppendLine(condAmount.ToString("#.00"));
			}
			this._strMTTInfo = stringBuilder.ToString();
		}

		public override void UpdateThem(Interaction ia)
		{
			if (ia != null && ia.strName == "SOCPoliceShakedownBasicBribeAccept" && ia.objUs.GetCondAmount("PoliceBribe") > 0.0)
			{
				LedgerLI li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PoliceBribe"), "Bribe", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
				Ledger.AddLI(li);
				ia.objUs.ZeroCondAmount("PoliceBribe");
				this._strMTTInfo = null;
			}
		}
	}
}
