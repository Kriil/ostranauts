using System;
using UnityEngine;

namespace Ostranauts.Electrical
{
	public struct ElectricalSignal
	{
		public ElectricalSignal(string origin, SignalType signal, bool callbac = false)
		{
			this.originID = origin;
			this.epoch = StarSystem.fEpoch;
			this.callback = callbac;
			this.signalType = signal;
		}

		public override string ToString()
		{
			string text = string.Empty;
			text = text + this.originID + "#";
			string str = text;
			int num = (int)this.signalType;
			text = str + num.ToString() + "#";
			text = text + this.epoch.ToString("0.00") + "#";
			return text + this.callback.ToString().ToLower();
		}

		public static ElectricalSignal FromString(string inputString)
		{
			string origin = string.Empty;
			SignalType signal = SignalType.None;
			double num = 0.0;
			bool callbac = false;
			string[] array = inputString.Split(new char[]
			{
				'#'
			});
			if (array.Length >= 4)
			{
				origin = array[0];
				signal = (SignalType)int.Parse(array[1]);
				num = double.Parse(array[2]);
				callbac = bool.Parse(array[3]);
			}
			else
			{
				Debug.LogWarning("Improperly saved electrical signal, not enough inputs!");
				ConsoleToGUI.instance.LogInfo(inputString);
			}
			return new ElectricalSignal(origin, signal, callbac)
			{
				epoch = num
			};
		}

		public string originID;

		public double epoch;

		public bool callback;

		public SignalType signalType;
	}
}
