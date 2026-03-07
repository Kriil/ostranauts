using System;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.Ships.Comms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.MFD
{
	public class GUIMFDPageHost : MonoBehaviour
	{
		private void Awake()
		{
			if (GUIMFDDisplay.OnUpdateMFD == null)
			{
				GUIMFDDisplay.OnUpdateMFD = new OnUpdateMFDEvent();
			}
			if (GUIMFDPageHost.OnRequestMFDChange == null)
			{
				GUIMFDPageHost.OnRequestMFDChange = new OnRequestMFDChangeEvent();
			}
			for (int i = 0; i < this.btnSoftKeys.Length; i++)
			{
				int btnNr = i;
				this.btnSoftKeys[i].onClick.AddListener(delegate()
				{
					this.OnButtonDown(btnNr);
				});
			}
		}

		private void Start()
		{
			MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
			StarSystem.OnNewShipCommsMessage.AddListener(new UnityAction<ShipMessage>(this.RefreshUI));
			GUIMFDPageHost.OnRequestMFDChange.AddListener(new UnityAction<MFDPage>(this.OnMFDPageRequest));
			this._currentPage = new MFDMainMenu();
		}

		private void Update()
		{
			if ((double)Time.unscaledTime - this._timeLastUpdated < 1.0)
			{
				return;
			}
			this._timeLastUpdated = (double)Time.unscaledTime;
			this.RefreshUI(null);
		}

		private void OnDestroy()
		{
			StarSystem.OnNewShipCommsMessage.RemoveListener(new UnityAction<ShipMessage>(this.RefreshUI));
			GUIMFDPageHost.OnRequestMFDChange.RemoveListener(new UnityAction<MFDPage>(this.OnMFDPageRequest));
		}

		private void OnMFDPageRequest(MFDPage mfdPage)
		{
			if (mfdPage == null)
			{
				return;
			}
			this._currentPage = mfdPage;
		}

		private void RefreshUI(ShipMessage shipMessage = null)
		{
			Ship ship = null;
			if (CrewSim.GetSelectedCrew() != null)
			{
				ship = CrewSim.GetSelectedCrew().ship;
			}
			if (ship == null || ship.bDestroyed || ship.objSS == null)
			{
				return;
			}
			if (ship.objSS.bIsBO)
			{
				this._currentPage = new MFDError();
			}
			if (this._currentPage != null)
			{
				this._currentPage.OnUIRefresh(shipMessage);
			}
		}

		private void OnButtonDown(int btnNr)
		{
			this._currentPage = this._currentPage.OnButtonDown(btnNr);
		}

		public static OnRequestMFDChangeEvent OnRequestMFDChange = new OnRequestMFDChangeEvent();

		[SerializeField]
		private Button[] btnSoftKeys;

		private double _timeLastUpdated;

		private MFDPage _currentPage;
	}
}
