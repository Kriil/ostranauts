using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Components;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Ships;
using Ostranauts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.ShipRating
{
	public class GUIShipRating : GUIData
	{
		private new void Awake()
		{
			base.Awake();
			this.btnRateShip.onClick.AddListener(new UnityAction(this.OnRateShipPressed));
			this.btnLeft.onClick.AddListener(new UnityAction(this.OnLeftArrowPressed));
			this.btnRight.onClick.AddListener(new UnityAction(this.OnRightArrowPressed));
			this.chkToggle.OnClick.AddListener(new UnityAction<bool>(this.OnToggleView));
		}

		private void OnToggleView(bool showRooms)
		{
			if (this._ownedShips == null || this._ownedShips.Count <= this._currentlySelected)
			{
				return;
			}
			string regId = this._ownedShips[this._currentlySelected];
			this.SetShipTexture(regId, showRooms);
		}

		private void OnLeftArrowPressed()
		{
			if (this._ownedShips == null || this._ownedShips.Count <= 1)
			{
				return;
			}
			this._currentlySelected--;
			if (this._currentlySelected < 0)
			{
				this._currentlySelected = this._ownedShips.Count - 1;
			}
			this.txtRegId.text = this._ownedShips[this._currentlySelected];
			this.UpdatePreview(this._ownedShips[this._currentlySelected]);
		}

		private void OnRightArrowPressed()
		{
			if (this._ownedShips == null || this._ownedShips.Count <= 1)
			{
				return;
			}
			this._currentlySelected++;
			if (this._currentlySelected >= this._ownedShips.Count)
			{
				this._currentlySelected = 0;
			}
			this.txtRegId.text = this._ownedShips[this._currentlySelected];
			this.UpdatePreview(this._ownedShips[this._currentlySelected]);
		}

		private string DebugString(string regID)
		{
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(this._coSelf.strID);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(regID);
			if (shipByRegID == null || shipsForOwner.Contains(regID))
			{
				return regID;
			}
			return string.Concat(new string[]
			{
				shipByRegID.make,
				"-",
				shipByRegID.model,
				"(",
				shipByRegID.strRegID,
				")"
			});
		}

		private void UpdatePreview(string regID)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(regID);
			this.imgShip.gameObject.SetActive(false);
			this.textContainer.SetActive(false);
			this.chkToggle.Reset();
			if (this._ratedShipsDict.ContainsKey(regID) && shipByRegID.HasRating())
			{
				this.SetShipTexture(shipByRegID.strRegID, false);
				this.SetShipStrings(shipByRegID);
				this.btnRateShip.gameObject.SetActive(false);
			}
			else
			{
				this.txtRating.text = string.Empty;
				this.btnRateShip.gameObject.SetActive(true);
			}
		}

		private void SetShipStrings(Ship ship)
		{
			this.textContainer.SetActive(true);
			this.txtRating.text = ship.GetRatingString();
			this.txtPublicName.text = ship.publicName;
			this.txtModel.text = ship.model;
			this.txtMake.text = ship.make;
			this.txtDimensions.text = ship.dimensions;
		}

		private void OnRateShipPressed()
		{
			if (this._ownedShips == null || this._ownedShips.Count == 0)
			{
				return;
			}
			MonoSingleton<AsyncShipLoader>.Instance.Unload(this._lastAsyncShipId);
			string strRegID = this._ownedShips[this._currentlySelected];
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID == null)
			{
				return;
			}
			this._lastAsyncShipId = shipByRegID.strRegID;
			MonoSingleton<AsyncShipLoader>.Instance.LoadRatingShip(shipByRegID, new Action<float>(this.UpdateLoadingBar), this.prefabFloatingText);
			base.StartCoroutine(this.GenerateRating(shipByRegID));
			this.btnRateShip.gameObject.SetActive(false);
		}

		private IEnumerator GenerateRating(Ship ship)
		{
			Ship asyncShip = null;
			yield return new WaitUntil(() => MonoSingleton<AsyncShipLoader>.Instance.GetShip(ship.strRegID, out asyncShip));
			RectTransform refer = this.imgShip.GetComponentInParent<RectTransform>();
			yield return MonoSingleton<ScreenshotUtil>.Instance.GetAsyncScreenShot(asyncShip, this._ratedShipsDict, refer.rect.size);
			string[] rating = asyncShip.CalculateRating();
			ship.UpdateRating(rating);
			this.SetShipTexture(asyncShip.strRegID, false);
			this.SetShipStrings(ship);
			this.btnRateShip.interactable = true;
			this.UpdateLoadingBar(-1f);
			MonoSingleton<AsyncShipLoader>.Instance.Unload(ship.strRegID);
			yield break;
		}

		private bool SetShipTexture(string regId, bool showRooms)
		{
			Tuple<Texture2D, Texture2D> tuple;
			if (!this._ratedShipsDict.TryGetValue(regId, out tuple))
			{
				return false;
			}
			Texture2D texture2D = (!showRooms) ? tuple.Item1 : tuple.Item2;
			this.imgShip.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture2D.width / (float)texture2D.height;
			this.imgShip.texture = texture2D;
			this.imgShip.gameObject.SetActive(true);
			return true;
		}

		private void UpdateLoadingBar(float progress)
		{
			this.goLoadingBar.SetActive(progress >= 0f);
			this.imgLoadingFill.fillAmount = progress;
		}

		private void SetData(CondOwner coSelf, CondOwner coVendor)
		{
			if (coSelf == null || coVendor == null)
			{
				return;
			}
			this._coSelf = coSelf;
			this._coVendor = coVendor;
			this._ownedShips = Ship.GetOwnedDockedShips(this._coSelf, this._coVendor);
			if (this._ownedShips == null || this._ownedShips.Count == 0)
			{
				this.txtRegId.text = "none";
				this.btnRateShip.interactable = false;
				return;
			}
			this.UpdateLoadingBar(-1f);
			this.txtRegId.text = this._ownedShips.FirstOrDefault<string>();
			this._currentlySelected = 0;
			this.textContainer.SetActive(false);
			this.imgShip.gameObject.SetActive(false);
			this.btnRateShip.interactable = true;
			this.txtRating.text = string.Empty;
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			if (coSelf == null || coSelf.GetInteractionCurrent() == null)
			{
				return;
			}
			this.SetData(coSelf.GetInteractionCurrent().objThem, coSelf);
		}

		public override void UpdateUI()
		{
			if (this.COSelf == null || this.COSelf.GetInteractionCurrent() == null)
			{
				return;
			}
			this.SetData(this.COSelf.GetInteractionCurrent().objThem, this.COSelf);
		}

		public override void SaveAndClose()
		{
			MonoSingleton<AsyncShipLoader>.Instance.Unload(this._lastAsyncShipId);
			base.StopAllCoroutines();
			base.SaveAndClose();
		}

		[Header("General")]
		[SerializeField]
		private Button btnRateShip;

		[SerializeField]
		private ToggleSideSwitch chkToggle;

		[SerializeField]
		private RawImage imgShip;

		[SerializeField]
		private GameObject prefabFloatingText;

		[Header("Ship switch")]
		[SerializeField]
		private Button btnLeft;

		[SerializeField]
		private Button btnRight;

		[SerializeField]
		private TMP_Text txtRegId;

		[Header("Loading bar")]
		[SerializeField]
		private GameObject goLoadingBar;

		[SerializeField]
		private Image imgLoadingFill;

		[Header("Texts")]
		[SerializeField]
		private GameObject textContainer;

		[SerializeField]
		private TMP_Text txtRating;

		[SerializeField]
		private TMP_Text txtPublicName;

		[SerializeField]
		private TMP_Text txtModel;

		[SerializeField]
		private TMP_Text txtMake;

		[SerializeField]
		private TMP_Text txtDimensions;

		private Dictionary<string, Tuple<Texture2D, Texture2D>> _ratedShipsDict = new Dictionary<string, Tuple<Texture2D, Texture2D>>();

		private List<string> _ownedShips = new List<string>();

		private int _currentlySelected;

		private CondOwner _coSelf;

		private CondOwner _coVendor;

		private string _lastAsyncShipId;

		private bool _debug = true;
	}
}
