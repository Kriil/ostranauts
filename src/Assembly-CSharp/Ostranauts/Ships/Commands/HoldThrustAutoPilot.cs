using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class HoldThrustAutoPilot : BaseCommand
	{
		public HoldThrustAutoPilot(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
			this._holdthrustTimeStamp = StarSystem.fEpoch;
			GUIOrbitDraw guiorbitDraw = null;
			if (CrewSim.goUI != null)
			{
				guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
			}
			if (guiorbitDraw != null)
			{
				Dictionary<string, string> propMap = guiorbitDraw.GetPropMap();
				string s;
				this._engineMode = ((!propMap.TryGetValue("nKnobEngineMode", out s)) ? 1 : int.Parse(s));
				this._throttleSld = ((!propMap.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s));
			}
		}

		public override string[] SaveData
		{
			get
			{
				string text = this._engineMode.ToString();
				string text2 = this._throttleSld.ToString();
				return new string[]
				{
					text,
					text2
				};
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				string text = value.FirstOrDefault<string>();
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				int.TryParse(text, out this._engineMode);
				if (value.Length >= 2)
				{
					string text2 = value[1];
					if (string.IsNullOrEmpty(text2))
					{
						return;
					}
					float.TryParse(text2, out this._throttleSld);
				}
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs == null || base.ShipUs.bDestroyed)
			{
				return CommandCode.Cancelled;
			}
			GUIOrbitDraw guiorbitDraw = null;
			Dictionary<string, string> dictionary = null;
			if (CrewSim.goUI != null)
			{
				guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
			}
			if (guiorbitDraw != null)
			{
				if (guiorbitDraw.PlayerThrusting)
				{
					return CommandCode.Ongoing;
				}
				dictionary = guiorbitDraw.GetPropMap();
				if (!guiorbitDraw.HoldingThrustActive && StarSystem.fEpoch - this._holdthrustTimeStamp > 1.0)
				{
					return CommandCode.Finished;
				}
			}
			if (dictionary != null)
			{
				string s;
				this._engineMode = ((!dictionary.TryGetValue("nKnobEngineMode", out s)) ? 1 : int.Parse(s));
				this._throttleSld = ((!dictionary.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s));
			}
			base.ShipUs.Maneuver(0f, 1f * this._throttleSld, 0f, 0, CrewSim.TimeElapsedScaled(), (Ship.EngineMode)this._engineMode);
			return CommandCode.Ongoing;
		}

		private readonly IAICharacter _ai;

		private double _timeStampLastThrust;

		private int _engineMode;

		private float _throttleSld;

		private double _holdthrustTimeStamp;
	}
}
