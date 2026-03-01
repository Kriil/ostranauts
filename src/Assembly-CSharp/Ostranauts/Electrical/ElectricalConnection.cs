using System;
using UnityEngine;

namespace Ostranauts.Electrical
{
	[Serializable]
	public struct ElectricalConnection
	{
		public ElectricalConnection(string origin, SignalType signal, string nick = "", bool status = true)
		{
			this.originID = origin;
			this.signalType = signal;
			this.switchStatus = status;
			this.nickName = nick;
		}

		public static ElectricalConnection FromString(string inputString)
		{
			string origin = string.Empty;
			SignalType signal = SignalType.None;
			bool status = false;
			string nick = string.Empty;
			string[] array = inputString.Split(new char[]
			{
				'#'
			});
			if (array.Length >= 3)
			{
				origin = array[0];
				signal = (SignalType)int.Parse(array[1]);
				status = bool.Parse(array[2]);
				if (array.Length >= 4)
				{
					nick = array[3];
				}
			}
			else
			{
				Debug.LogWarning("Improperly saved electrical connection, not enough inputs!");
			}
			ElectricalConnection result = new ElectricalConnection(origin, signal, nick, status);
			return result;
		}

		public override string ToString()
		{
			string text = string.Empty;
			text = text + this.originID + "#";
			string str = text;
			int num = (int)this.signalType;
			text = str + num.ToString() + "#";
			text = text + this.switchStatus.ToString().ToLower() + "#";
			return text + this.nickName;
		}

		public string originID;

		public SignalType signalType;

		public bool switchStatus;

		public string nickName;
	}
}
