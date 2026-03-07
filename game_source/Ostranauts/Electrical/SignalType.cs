using System;

namespace Ostranauts.Electrical
{
	[Serializable]
	public enum SignalType
	{
		None,
		Off,
		On,
		Toggle,
		Cycle,
		Connect,
		Disconnect
	}
}
