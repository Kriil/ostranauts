using System;
using Ostranauts.Events.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class ConfirmationPopupBase : GUIDataWindow
	{
		protected void Awake()
		{
			this.btnClose.onClick.AddListener(new UnityAction(this.OnBtnClose));
			this.btnCloseTop.onClick.AddListener(new UnityAction(this.OnBtnClose));
			this.btnTransactionConfirm.onClick.AddListener(new UnityAction(this.OnBtnPurchase));
			this.txtError.gameObject.SetActive(false);
		}

		private void Update()
		{
			if (this._errorVisibilityTimer > 0f)
			{
				this._errorVisibilityTimer -= Time.deltaTime;
				if (this._errorVisibilityTimer <= 0f)
				{
					this.txtError.gameObject.SetActive(false);
				}
			}
		}

		public virtual void ShowPanel(ShipPurchaseDTO shipDto, double availableFunds)
		{
			base.RegisterWindow();
			this.txtError.gameObject.SetActive(false);
			if (shipDto == null || shipDto.ShipValue == 0.0)
			{
				this.OnBtnClose();
				return;
			}
			this._shipDto = shipDto;
			this._availableFunds = availableFunds;
			this.txtRegId.text = shipDto.RegId;
			this.txtName.text = shipDto.ShipName;
			this.imgMain.texture = shipDto.Image;
			this.imgBackground.gameObject.SetActive(shipDto.IsSilhouette);
		}

		protected void ShowError(string errorMessage)
		{
			if (string.IsNullOrEmpty(errorMessage))
			{
				return;
			}
			this._errorVisibilityTimer = 2f;
			this.txtError.text = errorMessage;
			this.txtError.gameObject.SetActive(true);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
		}

		protected void OnBtnClose()
		{
			base.UnregisterWindow();
			UnityEngine.Object.Destroy(base.gameObject);
		}

		protected virtual void OnBtnPurchase()
		{
			this._shipDto.Callback();
			this.OnBtnClose();
		}

		public override void CloseExternally()
		{
			this.OnBtnClose();
		}

		[Header("Images")]
		[SerializeField]
		private RawImage imgMain;

		[SerializeField]
		private RawImage imgBackground;

		[Header("Main")]
		[SerializeField]
		protected TMP_Text txtRegId;

		[SerializeField]
		protected TMP_Text txtName;

		[SerializeField]
		private Button btnCloseTop;

		[SerializeField]
		private Button btnClose;

		[SerializeField]
		private Button btnTransactionConfirm;

		[SerializeField]
		private TMP_Text txtError;

		protected ShipPurchaseDTO _shipDto;

		protected double _availableFunds;

		private float _errorVisibilityTimer;
	}
}
