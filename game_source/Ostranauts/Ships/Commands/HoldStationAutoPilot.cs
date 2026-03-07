using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class HoldStationAutoPilot : BaseCommand
	{
		public HoldStationAutoPilot(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
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
			Ship shipStationKeepingTarget = base.ShipUs.shipStationKeepingTarget;
			if (shipStationKeepingTarget == null || shipStationKeepingTarget.objSS == null || base.ShipUs == null)
			{
				return CommandCode.Cancelled;
			}
			if (shipStationKeepingTarget.bDestroyed)
			{
				base.ShipUs.shipStationKeepingTarget = CrewSim.system.GetShipByRegID(shipStationKeepingTarget.strRegID);
				if (base.ShipUs.shipStationKeepingTarget == null)
				{
					return CommandCode.Cancelled;
				}
				shipStationKeepingTarget = base.ShipUs.shipStationKeepingTarget;
			}
			else if (base.ShipUs.IsDocked() || shipStationKeepingTarget.HideFromSystem)
			{
				return CommandCode.Finished;
			}
			GUIOrbitDraw guiorbitDraw = null;
			GUIDockSys ds = null;
			Dictionary<string, string> dictionary = null;
			if (CrewSim.goUI != null)
			{
				guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
				ds = CrewSim.goUI.GetComponent<GUIDockSys>();
			}
			if (guiorbitDraw != null)
			{
				dictionary = guiorbitDraw.GetPropMap();
			}
			if (this.PauseDuringPlayerInput(guiorbitDraw, ds))
			{
				return CommandCode.Ongoing;
			}
			float num = 0f;
			float num2 = 0f;
			float num3 = (float)(shipStationKeepingTarget.objSS.vVelX - base.ShipUs.objSS.vVelX);
			float num4 = (float)(shipStationKeepingTarget.objSS.vVelY - base.ShipUs.objSS.vVelY);
			float num5 = (float)(shipStationKeepingTarget.objSS.vPosx - base.ShipUs.objSS.vPosx);
			float num6 = (float)(shipStationKeepingTarget.objSS.vPosy - base.ShipUs.objSS.vPosy);
			float num7 = Mathf.Cos(base.ShipUs.objSS.fRot);
			float num8 = Mathf.Sin(base.ShipUs.objSS.fRot);
			float num9 = 0f;
			num9 += Mathf.Atan2(num5 * num7 + num6 * num8, -num5 * num8 + num6 * num7) * (-0.2f + -0.3f / Time.timeScale);
			num9 -= base.ShipUs.objSS.fW * 0.3f;
			float num10 = 1f / Time.timeScale;
			float num11 = MathUtils.Clamp(num9, -num10, num10);
			float num12 = Mathf.Abs(num3);
			float num13 = Mathf.Abs(num4);
			if ((double)num12 <= 1E-11 && (double)num13 <= 1E-11 && Mathf.Abs(num11) <= 0.05f && base.ShipUs.objSS.fW == 0f)
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Ongoing;
			}
			if ((double)num12 > 1E-11 && num12 > num13)
			{
				num = ((num3 >= 0f) ? 1f : -1f);
			}
			else
			{
				base.ShipUs.objSS.vVelX = shipStationKeepingTarget.objSS.vVelX;
			}
			if ((double)num13 > 1E-11 && num13 > num12)
			{
				num2 = ((num4 >= 0f) ? 1f : -1f);
			}
			else
			{
				base.ShipUs.objSS.vVelY = shipStationKeepingTarget.objSS.vVelY;
			}
			float num14 = num * num7 + num2 * num8;
			float num15 = -(num * num8 - num2 * num7);
			if (dictionary != null)
			{
				string s;
				this._engineMode = ((!dictionary.TryGetValue("nKnobEngineMode", out s)) ? 1 : int.Parse(s));
				this._throttleSld = ((!dictionary.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s));
			}
			base.ShipUs.Maneuver(num14 * this._throttleSld, num15 * this._throttleSld, num11 * this._throttleSld, 0, CrewSim.TimeElapsedScaled(), (Ship.EngineMode)this._engineMode);
			return CommandCode.Ongoing;
		}

		private bool PauseDuringPlayerInput(GUIOrbitDraw od, GUIDockSys ds)
		{
			if ((od != null && od.PlayerThrusting) || (ds != null && ds.PlayerThrusting))
			{
				this._timeStampLastThrust = StarSystem.fEpoch;
				return true;
			}
			return StarSystem.fEpoch - this._timeStampLastThrust < 2.0;
		}

		private readonly IAICharacter _ai;

		private double _timeStampLastThrust;

		private int _engineMode;

		private float _throttleSld;
	}
}
