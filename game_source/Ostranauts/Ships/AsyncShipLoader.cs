using System;
using System.Collections.Generic;
using Ostranauts.Core;
using UnityEngine;

namespace Ostranauts.Ships
{
	public class AsyncShipLoader : MonoSingleton<AsyncShipLoader>
	{
		private new void Awake()
		{
			base.Awake();
			GameObject gameObject = new GameObject("ShipContainer");
			gameObject.transform.SetParent(base.transform);
			this._shipholder = gameObject.transform;
			gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
		}

		public void LoadBarterZoneShip(JsonShip playerShip)
		{
			if (playerShip == null || this._loadStateDict.ContainsKey(playerShip.strRegID) || playerShip.aZones == null || playerShip.aZones.Length == 0)
			{
				return;
			}
			JsonShip jsonShip = playerShip.Clone();
			GameObject gameObject = new GameObject(jsonShip.strRegID);
			gameObject.transform.SetParent(this._shipholder, false);
			BarterZoneShip barterZoneShip = new BarterZoneShip(gameObject)
			{
				json = jsonShip,
				strRegID = playerShip.strRegID
			};
			this._loadStateDict.Add(barterZoneShip.strRegID, barterZoneShip);
			int iteratorCounter = Mathf.Max(10, jsonShip.aItems.Length / 50);
			this._loadingCoroutine = base.StartCoroutine(barterZoneShip.Init(iteratorCounter));
		}

		public void LoadDockedBarterZoneShips(CondOwner owner)
		{
			if (owner == null)
			{
				return;
			}
			List<string> ownedDockedShips = Ship.GetOwnedDockedShips(owner, CrewSim.shipCurrentLoaded, true);
			foreach (string strRegID in ownedDockedShips)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				if (shipByRegID != null && shipByRegID.LoadState <= Ship.Loaded.Shallow)
				{
					this.LoadBarterZoneShip(shipByRegID.json);
				}
			}
		}

		public void LoadShipPreview(string shipIdentifier)
		{
			if (string.IsNullOrEmpty(shipIdentifier))
			{
				return;
			}
			Ship shipByRegID = CrewSim.system.GetShipByRegID(shipIdentifier);
			JsonShip jsonShip = null;
			if (shipByRegID == null)
			{
				JsonShip ship = DataHandler.GetShip(shipIdentifier);
				if (ship != null)
				{
					jsonShip = ship.Clone();
				}
			}
			else
			{
				jsonShip = shipByRegID.json.Clone();
			}
			if (jsonShip == null || this._loadStateDict.ContainsKey(shipIdentifier))
			{
				return;
			}
			GameObject gameObject = new GameObject(shipIdentifier);
			gameObject.transform.SetParent(this._shipholder, false);
			AsyncPreviewShip asyncPreviewShip = new AsyncPreviewShip(gameObject)
			{
				json = jsonShip,
				strRegID = shipIdentifier
			};
			this._loadStateDict.Add(asyncPreviewShip.strRegID, asyncPreviewShip);
			int iteratorCounter = Mathf.Max(10, jsonShip.aItems.Length / 50);
			this._loadingCoroutine = base.StartCoroutine(asyncPreviewShip.Init(iteratorCounter));
		}

		public void LoadRatingShip(Ship ship, Action<float> progressCallback, GameObject extraPrefab)
		{
			if (ship == null || ship.json == null || this._loadStateDict.ContainsKey(ship.strRegID))
			{
				return;
			}
			JsonShip jsonShip = ship.json.Clone();
			GameObject gameObject = new GameObject(jsonShip.strRegID);
			gameObject.transform.SetParent(this._shipholder, false);
			AsyncRatingShip asyncRatingShip = new AsyncRatingShip(gameObject)
			{
				json = jsonShip,
				strRegID = ship.strRegID
			};
			this._loadStateDict.Add(asyncRatingShip.strRegID, asyncRatingShip);
			int iteratorCounter = Mathf.Max(10, jsonShip.aItems.Length / 50);
			this._loadingCoroutine = base.StartCoroutine(asyncRatingShip.Init(iteratorCounter, progressCallback, extraPrefab));
		}

		public void SaveChangedCO(string shipReg)
		{
			IAsyncLoadable asyncLoadable;
			if (!this._loadStateDict.TryGetValue(shipReg, out asyncLoadable) || !(asyncLoadable is BarterZoneShip) || !asyncLoadable.FullyLoaded)
			{
				return;
			}
			Ship ship = CrewSim.system.dictShips[shipReg];
			if (ship != null)
			{
				asyncLoadable.SaveChangedCOs(ref ship);
			}
		}

		public void SaveAsyncShips<T>()
		{
			foreach (KeyValuePair<string, IAsyncLoadable> keyValuePair in this._loadStateDict)
			{
				if (keyValuePair.Value is T)
				{
					this.SaveChangedCO(keyValuePair.Key);
				}
			}
		}

		public void Unload(string shipReg = null)
		{
			if (this._loadingCoroutine != null)
			{
				base.StopCoroutine(this._loadingCoroutine);
			}
			this._loadingCoroutine = null;
			if (string.IsNullOrEmpty(shipReg))
			{
				this.DestroyAllShips();
			}
			else
			{
				this.DestroyShip(shipReg);
			}
		}

		public void Unload(Ship targetship)
		{
			if (targetship == null || targetship.json == null)
			{
				return;
			}
			List<string> list = new List<string>();
			list.Add(targetship.strRegID);
			if (targetship.json.aDocked != null && targetship.json.aDocked.Length != 0)
			{
				list.AddRange(targetship.json.aDocked);
			}
			foreach (string shipReg in list)
			{
				this.Unload(shipReg);
			}
		}

		private void DestroyAllShips()
		{
			foreach (KeyValuePair<string, IAsyncLoadable> keyValuePair in this._loadStateDict)
			{
				if (keyValuePair.Value != null)
				{
					keyValuePair.Value.Destroy(true);
				}
			}
			this._loadStateDict.Clear();
		}

		private bool DestroyShip(string shipReg)
		{
			if (string.IsNullOrEmpty(shipReg))
			{
				return false;
			}
			IAsyncLoadable asyncLoadable;
			if (this._loadStateDict.TryGetValue(shipReg, out asyncLoadable))
			{
				asyncLoadable.Destroy(true);
				this._loadStateDict.Remove(shipReg);
				return true;
			}
			return false;
		}

		public bool GetShip(string shipReg, out Ship ship)
		{
			ship = null;
			IAsyncLoadable asyncLoadable;
			if (this._loadStateDict.TryGetValue(shipReg, out asyncLoadable))
			{
				if (!asyncLoadable.FullyLoaded)
				{
					return false;
				}
				ship = (asyncLoadable as Ship);
				if (ship != null)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsShipLoading(string shipReg)
		{
			IAsyncLoadable asyncLoadable;
			return this._loadStateDict.TryGetValue(shipReg, out asyncLoadable);
		}

		public List<Ship> GetLoadedShips()
		{
			List<Ship> list = new List<Ship>();
			foreach (KeyValuePair<string, IAsyncLoadable> keyValuePair in this._loadStateDict)
			{
				if (keyValuePair.Value != null && keyValuePair.Value.FullyLoaded)
				{
					list.Add(keyValuePair.Value as Ship);
				}
			}
			return list;
		}

		public Room GetRoomAtWorldCoords(Vector2 vMouse)
		{
			foreach (Ship ship in this.GetLoadedShips())
			{
				if (ship != null)
				{
					Room roomAtWorldCoords = ship.GetRoomAtWorldCoords1(vMouse, false);
					if (roomAtWorldCoords != null)
					{
						return roomAtWorldCoords;
					}
				}
			}
			return null;
		}

		public static readonly string ASYNCLAYERNAME = "Ship Offscreen";

		public static readonly string DEFAULTLAYERNAME = "Default";

		public static readonly Vector3 SPAWNOFFSET = new Vector3(200f, 200f, 0f);

		private Dictionary<string, IAsyncLoadable> _loadStateDict = new Dictionary<string, IAsyncLoadable>();

		private bool _loaded;

		private const int ITERATORBASE = 50;

		private Transform _shipholder;

		private Coroutine _loadingCoroutine;
	}
}
