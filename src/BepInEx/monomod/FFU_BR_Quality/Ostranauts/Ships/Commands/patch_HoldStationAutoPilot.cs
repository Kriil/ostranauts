using System;
using System.Collections.Generic;
using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Ships.AIPilots.Interfaces;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class patch_HoldStationAutoPilot : HoldStationAutoPilot
	{
		[MonoModIgnore]
		public patch_HoldStationAutoPilot(IAICharacter pilot) : base(pilot)
		{
		}
		public CommandCode RunCommand()
		{
			Ship shipStationKeepingTarget = base.ShipUs.shipStationKeepingTarget;
			bool flag = shipStationKeepingTarget == null || shipStationKeepingTarget.objSS == null || base.ShipUs == null;
			CommandCode result;
			if (flag)
			{
				result = 8;
			}
			else
			{
				bool bDestroyed = shipStationKeepingTarget.bDestroyed;
				if (bDestroyed)
				{
					base.ShipUs.shipStationKeepingTarget = CrewSim.system.GetShipByRegID(shipStationKeepingTarget.strRegID);
					bool flag2 = base.ShipUs.shipStationKeepingTarget == null;
					if (flag2)
					{
						return 8;
					}
					shipStationKeepingTarget = base.ShipUs.shipStationKeepingTarget;
				}
				else
				{
					bool flag3 = (base.ShipUs.IsDocked() && (!FFU_BR_Defs.TowBraceAllowsKeep || !base.ShipUs.TowBraceSecured())) || shipStationKeepingTarget.HideFromSystem;
					if (flag3)
					{
						return 1;
					}
				}
				GUIOrbitDraw guiorbitDraw = null;
				GUIDockSys guidockSys = null;
				Dictionary<string, string> dictionary = null;
				bool flag4 = CrewSim.goUI != null;
				if (flag4)
				{
					guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
					guidockSys = CrewSim.goUI.GetComponent<GUIDockSys>();
				}
				bool flag5 = guiorbitDraw != null;
				if (flag5)
				{
					dictionary = guiorbitDraw.GetPropMap();
				}
				bool flag6 = base.PauseDuringPlayerInput(guiorbitDraw, guidockSys);
				if (flag6)
				{
					result = 4;
				}
				else
				{
					float num = 0f;
					float num2 = 0f;
					float num3 = (float)(shipStationKeepingTarget.objSS.vVelX - base.ShipUs.objSS.vVelX);
					float num4 = (float)(shipStationKeepingTarget.objSS.vVelY - base.ShipUs.objSS.vVelY);
					float num5 = (float)(shipStationKeepingTarget.objSS.vPosx - base.ShipUs.objSS.vPosx);
					float num6 = (float)(shipStationKeepingTarget.objSS.vPosy - base.ShipUs.objSS.vPosy);
					float num7 = Mathf.Cos(base.ShipUs.objSS.fRot);
					float num8 = Mathf.Sin(base.ShipUs.objSS.fRot);
					float num9 = 0f;
					num9 += Mathf.Atan2(num5 * num7 + num6 * num8, (0f - num5) * num8 + num6 * num7) * (-0.2f + -0.3f / Time.timeScale);
					num9 -= base.ShipUs.objSS.fW * 0.3f;
					float num10 = 1f / Time.timeScale;
					float num11 = MathUtils.Clamp(num9, 0f - num10, num10);
					float num12 = Mathf.Abs(num3);
					float num13 = Mathf.Abs(num4);
					bool flag7 = (double)num12 <= 1E-11 && (double)num13 <= 1E-11 && Mathf.Abs(num11) <= 0.05f && base.ShipUs.objSS.fW == 0f;
					if (flag7)
					{
						base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, 1);
						result = 4;
					}
					else
					{
						bool flag8 = (double)num12 > 1E-11 && num12 > num13;
						if (flag8)
						{
							num = ((num3 >= 0f) ? 1f : -1f);
						}
						else
						{
							base.ShipUs.objSS.vVelX = shipStationKeepingTarget.objSS.vVelX;
						}
						bool flag9 = (double)num13 > 1E-11 && num13 > num12;
						if (flag9)
						{
							num2 = ((num4 >= 0f) ? 1f : -1f);
						}
						else
						{
							base.ShipUs.objSS.vVelY = shipStationKeepingTarget.objSS.vVelY;
						}
						float num14 = num * num7 + num2 * num8;
						float num15 = 0f - (num * num8 - num2 * num7);
						bool flag10 = dictionary != null;
						if (flag10)
						{
							string s;
							this._engineMode = ((!dictionary.TryGetValue("nKnobEngineMode", out s)) ? 1 : int.Parse(s));
							this._throttleSld = ((!dictionary.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s));
						}
						base.ShipUs.Maneuver(num14 * this._throttleSld, num15 * this._throttleSld, num11 * this._throttleSld, 0, CrewSim.TimeElapsedScaled(), this._engineMode);
						result = 4;
					}
				}
			}
			return result;
		}
	}
}
