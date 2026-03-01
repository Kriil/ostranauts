using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.Comms;
using UnityEngine;

namespace Ostranauts.ShipGUIs.MFD
{
	public sealed class MFDShipSelect : MFDPage
	{
		public MFDShipSelect(int range = 300000)
		{
			if (GUIOrbitDraw.crossHairInfo != null)
			{
				this._orbitdrawShipSelection = GUIOrbitDraw.crossHairInfo._strRegID;
			}
			this._displayMode = new MFDShipSelect.DisplayMode(GUIDockSys.instance.MFDDisplayMode);
			this._contacts = this.GetShipsInCommRange(range, this._displayMode.ShowDerelicts);
			this.PopulateMFD();
		}

		private void PopulateMFD()
		{
			this.MoveSelectedShipToTop();
			List<string> list = new List<string>();
			foreach (ShipDist sd in this._contacts)
			{
				string text = this._displayMode.GetDisplayName(sd.si);
				text = text.Replace("|", " | ");
				list.Add(sd.fDist.ToString("F1") + "km");
				if (!string.IsNullOrEmpty(this._orbitdrawShipSelection) && this._orbitdrawShipSelection == sd.strRegID)
				{
					list.Add(string.Concat(new string[]
					{
						"<color=",
						this.GetContactColor(sd),
						"><color=#FF0000>[</color>",
						text,
						"<color=#FF0000>]</color></color>"
					}));
				}
				else
				{
					list.Add(string.Concat(new string[]
					{
						"<color=",
						this.GetContactColor(sd),
						">",
						text,
						"</color>"
					}));
				}
			}
			Tuple<List<string>, List<string>> tuple = base.StringListToPageLayout(list, this._currentsubPage, true);
			this.Title = ((this._contacts.Count != 0) ? "SELECT TARGET" : "NO TARGETS IN RANGE");
			tuple.Item1 = this.AddModeSelect(tuple.Item1);
			this.Left = tuple.Item1;
			this.Right = tuple.Item2;
			base.UpdateDisplay();
		}

		private void MoveSelectedShipToTop()
		{
			if (string.IsNullOrEmpty(this._orbitdrawShipSelection) || this._contacts == null || this._contacts.Count <= 1)
			{
				return;
			}
			ShipDist item = this._contacts.Find((ShipDist x) => x.strRegID == this._orbitdrawShipSelection);
			if (!string.IsNullOrEmpty(item.strRegID))
			{
				this._contacts.Remove(item);
				this._contacts.Insert(0, item);
			}
		}

		private List<string> AddModeSelect(List<string> leftStrings)
		{
			while (leftStrings.Count < 10)
			{
				leftStrings.Add(string.Empty);
			}
			leftStrings.Add(this._displayMode.GetCurrentModeLabel());
			leftStrings.Add("TOGGLE MODES");
			return leftStrings;
		}

		private string GetContactColor(ShipDist sd)
		{
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strID);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(sd.strRegID);
			if (shipByRegID == null)
			{
				return string.Empty;
			}
			Color color;
			if (shipsForOwner.Contains(sd.strRegID))
			{
				color = GUIOrbitDraw.clrBlue01;
			}
			else if (shipByRegID.IsStation(false))
			{
				color = GUIOrbitDraw.clrWhite01;
			}
			else if (!shipByRegID.bXPDRAntenna || shipByRegID.IsDerelict())
			{
				color = GUIOrbitDraw.clrWhite02;
			}
			else if (shipByRegID.IsLocalAuthority)
			{
				color = GUIOrbitDraw.clrLocalAuthority;
			}
			else
			{
				AIType shipType = AIShipManager.GetShipType(shipByRegID);
				if (shipType == AIType.HaulerDeployer || shipType == AIType.HaulerRetriever || shipType == AIType.HaulerCargo)
				{
					color = GUIOrbitDraw.clrHauler;
				}
				else
				{
					color = GUIOrbitDraw.clrOrange01;
				}
			}
			return "#" + ColorUtility.ToHtmlStringRGBA(color);
		}

		public override void OnUIRefresh(ShipMessage shipMessage)
		{
			if (string.IsNullOrEmpty(this._orbitdrawShipSelection))
			{
				if (GUIOrbitDraw.crossHairInfo == null)
				{
					return;
				}
				this._orbitdrawShipSelection = GUIOrbitDraw.crossHairInfo._strRegID;
				this.PopulateMFD();
			}
			else
			{
				if (GUIOrbitDraw.crossHairInfo != null && GUIOrbitDraw.crossHairInfo._strRegID == this._orbitdrawShipSelection)
				{
					return;
				}
				this._orbitdrawShipSelection = ((GUIOrbitDraw.crossHairInfo != null) ? GUIOrbitDraw.crossHairInfo._strRegID : null);
				this.PopulateMFD();
			}
		}

		private List<ShipDist> GetShipsInCommRange(int maxRange, bool showDerelicts)
		{
			List<ShipDist> list = new List<ShipDist>();
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			base.ShipUs.objSS.UpdateTime(StarSystem.fEpoch, false);
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(base.ShipUs.objSS, StarSystem.fEpoch, false);
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				if (ship != base.ShipUs && !ship.bDestroyed && (!ship.HideFromSystem || allDockedShips.Contains(ship)) && !ship.IsStationHidden(false) && ship.Classification != Ship.TypeClassification.Waypoint)
				{
					ship.objSS.UpdateTime(StarSystem.fEpoch, false);
					double rangeToCollisionKM = CollisionManager.GetRangeToCollisionKM(base.ShipUs, ship);
					if (rangeToCollisionKM <= (double)maxRange)
					{
						if (nearestBO == null || !StarSystem.IsLOSBlockedByBO(nearestBO, base.ShipUs, ship))
						{
							ShipInfo shipInfo = ShipInfo.GetShipInfo(base.ShipUs, ship, GUIDockSys.DictGPM);
							if (showDerelicts || (shipInfo.Known && (!ship.IsDerelict() || allDockedShips.Contains(ship))))
							{
								list.Add(new ShipDist
								{
									strRegID = ship.strRegID,
									fDist = rangeToCollisionKM,
									si = shipInfo
								});
							}
						}
					}
				}
			}
			return (from x in list
			orderby x.fDist
			select x).ToList<ShipDist>();
		}

		private List<ShipDist> GetShips(List<string> contacts)
		{
			List<ShipDist> list = new List<ShipDist>();
			foreach (string strRegID in contacts)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				if (shipByRegID != base.ShipUs && !shipByRegID.bDestroyed && !shipByRegID.HideFromSystem && !shipByRegID.IsStationHidden(false))
				{
					ShipInfo shipInfo = ShipInfo.GetShipInfo(base.ShipUs, shipByRegID, GUIDockSys.DictGPM);
					ShipDist item = new ShipDist
					{
						strRegID = shipByRegID.strRegID,
						si = shipInfo
					};
					list.Add(item);
				}
			}
			return list;
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if (btnIndex == 4 || btnIndex == 10)
			{
				if (this._contacts.Count > 8)
				{
					if (btnIndex == 4)
					{
						this._currentsubPage--;
					}
					else
					{
						this._currentsubPage++;
					}
					this._currentsubPage = Mathf.Clamp(this._currentsubPage, 0, Mathf.FloorToInt((float)this._contacts.Count / 8f));
					this.PopulateMFD();
				}
			}
			else if (btnIndex == 5)
			{
				bool flag = this._displayMode.ToggleMode();
				GUIDockSys.instance.MFDDisplayMode = this._displayMode.CurrentMode;
				if (flag)
				{
					return new MFDShipSelect(300000);
				}
				this.PopulateMFD();
			}
			else
			{
				if (btnIndex == this._mainMenuButton)
				{
					return new MFDMainMenu();
				}
				return new MFDComms(this._contacts[base.PageSelectionToIndex(btnIndex, this._currentsubPage)].strRegID, false, true);
			}
			return this;
		}

		private List<ShipDist> _contacts = new List<ShipDist>();

		private int _currentsubPage;

		private string _orbitdrawShipSelection;

		private MFDShipSelect.DisplayMode _displayMode;

		private class DisplayMode
		{
			public DisplayMode(int gpmModeSetting)
			{
				if (gpmModeSetting < this._modes.Length)
				{
					this.CurrentMode = gpmModeSetting;
				}
			}

			public int CurrentMode { get; private set; }

			public bool ShowDerelicts
			{
				get
				{
					return this.CurrentMode < 2;
				}
			}

			public string GetCurrentModeLabel()
			{
				return string.Concat(new object[]
				{
					"Mode [",
					this.CurrentMode,
					"] ",
					this._modes[this.CurrentMode]
				});
			}

			public bool ToggleMode()
			{
				this.CurrentMode++;
				if (this.CurrentMode < this._modes.Length && this.CurrentMode != 2)
				{
					return false;
				}
				if (this.CurrentMode >= this._modes.Length)
				{
					this.CurrentMode = 0;
				}
				return true;
			}

			public string GetDisplayName(ShipInfo si)
			{
				return (this.CurrentMode != 0 && this.CurrentMode != 2) ? si.publicName : si.strRegID;
			}

			private string[] _modes = new string[]
			{
				"CALL | Derelicts: ON",
				"NAME | Derelicts: ON",
				"CALL | Derelicts: OFF",
				"NAME | Derelicts: OFF"
			};
		}
	}
}
