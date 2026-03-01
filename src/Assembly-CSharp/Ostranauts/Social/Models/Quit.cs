using System;
using System.Collections.Generic;
using System.Text;

namespace Ostranauts.Social.Models
{
	public class Quit : SocialStakes
	{
		public Quit()
		{
			this.strContext = "ACTQuitNegotiationUs";
			this._quitting = true;
		}

		public override void UpdateUs(Interaction ia)
		{
			if (ia == null)
			{
				return;
			}
			this.UpdateUs(ia.objThem);
		}

		public override void UpdateUs(CondOwner objThem)
		{
			if (objThem == null)
			{
				return;
			}
			if (this._startSalary < 0.0)
			{
				this._startSalary = objThem.GetCondAmount("PaySalary");
			}
			if (this._startDeathPay < 0.0)
			{
				this._startDeathPay = objThem.GetCondAmount("PayDeath");
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("One-Time Bonus: $");
			stringBuilder.AppendLine(objThem.GetCondAmount("PaySigning").ToString("#.00"));
			stringBuilder.Append("New Salary/day: $");
			stringBuilder.Append(objThem.GetCondAmount("PaySalary").ToString("#.00"));
			stringBuilder.AppendLine("/day");
			stringBuilder.Append("New Death Pay: $");
			stringBuilder.AppendLine(objThem.GetCondAmount("PayDeath").ToString("#.00"));
			this._strMTTInfo = stringBuilder.ToString();
		}

		public override void UpdateThem(Interaction ia)
		{
			if (this._startSalary < 0.0)
			{
				this._startSalary = ia.objUs.GetCondAmount("PaySalary");
			}
			if (this._startDeathPay < 0.0)
			{
				this._startDeathPay = ia.objUs.GetCondAmount("PayDeath");
			}
			if (ia != null && (ia.strName == "SOCHireQuitsStays" || ia.strName == "SOCHireQuitsStaysThreat" || ia.strName == "SOCHireQuitsStaysResponsibility") && this._quitting)
			{
				this._quitting = false;
				string strMsg = ia.objUs.strName + " remains a member of " + ia.objUs.Company.strName + ".";
				ia.objThem.LogMessage(strMsg, "Neutral", ia.objUs.strName);
				ia.objUs.LogMessage(strMsg, "Neutral", ia.objUs.strName);
				LedgerLI li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PaySigning"), "One-Time Bonus", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
				Ledger.AddLI(li);
				if (this._startSalary < 0.0)
				{
					List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(ia.objUs.strName, ia.objThem.strName, null, true, false);
					foreach (LedgerLI ledgerLI in unpaidLIs)
					{
						if (ledgerLI.strDesc.Contains("Salary"))
						{
							Ledger.RemoveLI(ledgerLI);
						}
					}
					li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PaySalary"), "Salary", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.Daily);
					Ledger.AddLI(li);
				}
				if (this._startDeathPay < 0.0)
				{
					double num = ia.objUs.GetCondAmount("PayDeath") - this._startDeathPay;
					if (num > 0.0)
					{
						li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)num, "Death Pay Adjustment", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
					}
					else
					{
						li = new LedgerLI(ia.objThem.strName, ia.objUs.strName, (float)(-(float)num), "Death Pay Adjustment", GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
						ia.objUs.AddCondAmount(GUIFinance.strCondCurr, -num, 0.0, 0f);
					}
					Ledger.AddLI(li);
				}
			}
		}

		private bool _quitting;

		private double _startSalary = -1.0;

		private double _startDeathPay = -1.0;
	}
}
