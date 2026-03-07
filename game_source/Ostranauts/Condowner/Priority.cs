using System;

namespace Ostranauts.Condowner
{
	internal class Priority
	{
		public Priority(double fIn, Condition objIn)
		{
			this.fValue = fIn;
			this.objCond = objIn;
		}

		public void Destroy()
		{
			this.objCond = null;
		}

		public override string ToString()
		{
			return this.objCond.ToString();
		}

		public double fValue;

		public Condition objCond;
	}
}
