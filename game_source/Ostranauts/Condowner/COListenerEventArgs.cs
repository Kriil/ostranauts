using System;

namespace Ostranauts.Condowner
{
	public class COListenerEventArgs : EventArgs
	{
		public COListenerEventArgs(CondOwner CO, Condition cond)
		{
			this.CO = CO;
			this.cond = cond;
		}

		public readonly CondOwner CO;

		public readonly Condition cond;
	}
}
