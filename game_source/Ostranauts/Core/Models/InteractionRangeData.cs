using System;

namespace Ostranauts.Core.Models
{
	public class InteractionRangeData
	{
		public float MinRange;

		public float MaxRange = float.PositiveInfinity;

		public bool UseLoS = true;
	}
}
