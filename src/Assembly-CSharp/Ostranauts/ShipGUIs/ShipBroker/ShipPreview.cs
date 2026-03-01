using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Rendering;
using Ostranauts.Ships;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class ShipPreview : GUIDataWindow
	{
		private void Awake()
		{
			GUIShipBroker.OnVisitShip.AddListener(new UnityAction<string>(this.OnShowPreview));
			GUIShipBroker.OnCloseMenu.AddListener(new UnityAction(this.OnCloseShipVisit));
			this.btnClose.onClick.AddListener(new UnityAction(this.OnCloseShipVisit));
			this.renderTexture.Release();
		}

		private void OnDestroy()
		{
			GUIShipBroker.OnVisitShip.RemoveListener(new UnityAction<string>(this.OnShowPreview));
			GUIShipBroker.OnCloseMenu.RemoveListener(new UnityAction(this.OnCloseShipVisit));
		}

		public override void CloseExternally()
		{
			this.OnCloseShipVisit();
		}

		private void Update()
		{
			if ((double)this.cgSearchSubScreen.alpha >= 0.9)
			{
				this.PlayTextFieldAnimation();
			}
			if (!this._camFeed)
			{
				return;
			}
			Vector3 vector = this.CastRenderImgCoordsToWorldCoords(this.renderRectTransform, this._renderImageSize, this.renderRectTransform.position.z);
			MonoSingleton<GUIItemList>.Instance.CalculatedMousePosition = vector;
			Camera screenShotCam = CrewSim.objInstance.ScreenShotCam;
			this.ScrollPosition(screenShotCam, vector);
			this.ClampPosition(screenShotCam.transform);
		}

		private void OnShowPreview(string shipIdentifier)
		{
			if (string.IsNullOrEmpty(shipIdentifier))
			{
				return;
			}
			base.RegisterWindow();
			this._loadedAsyncShips.Add(shipIdentifier);
			MonoSingleton<AsyncShipLoader>.Instance.LoadShipPreview(shipIdentifier);
			this.cgSearchSubScreen.alpha = 0f;
			this.txtConnecting.text = "Connecting..";
			this._duration = 0f;
			this.statusContainer.SetActive(false);
			this._renderImageSize = this.renderRectTransform.rect.size;
			CanvasManager.ShowCanvasGroup(this.cg);
			base.StartCoroutine(this.ShowShipWhenLoaded(shipIdentifier));
			base.StartCoroutine(this.ShowLoadingAnimation(true));
		}

		private void OnCloseShipVisit()
		{
			base.UnregisterWindow();
			this.renderTexture.Release();
			this._camFeed = false;
			CrewSim.objInstance.SwitchActiveCamera(true);
			CanvasManager.HideCanvasGroup(this.cg);
			MonoSingleton<GUIItemList>.Instance.DisableAsyncMode();
			foreach (string shipReg in this._loadedAsyncShips)
			{
				MonoSingleton<AsyncShipLoader>.Instance.Unload(shipReg);
			}
			this._loadedAsyncShips.Clear();
			this.renderRectTransform.GetComponent<RawImage>().enabled = false;
		}

		private Vector3 CastRenderImgCoordsToWorldCoords(RectTransform imgRect, Vector2 imageSize, float z)
		{
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(imgRect, Input.mousePosition, CrewSim.objInstance.UICamera, out vector);
			float x = (imageSize.x / 2f + vector.x) / imageSize.x * (float)CrewSim.objInstance.ActiveCam.pixelWidth;
			float y = (imageSize.y / 2f + vector.y) / imageSize.y * (float)CrewSim.objInstance.ActiveCam.pixelHeight;
			return Crt.ConvertToCrtCoords(x, y, z);
		}

		private void PlayTextFieldAnimation()
		{
			this._duration += Time.unscaledDeltaTime;
			float num = UnityEngine.Random.Range(1.5f, 2.5f);
			if (this._duration - num >= 0f)
			{
				this._duration -= num;
				TMP_Text tmp_Text = this.txtConnecting;
				tmp_Text.text += ".";
			}
		}

		private void EnablePreviewCamera(Ship asyncShip)
		{
			this._yBound = asyncShip.nRows / 2;
			this._xBound = asyncShip.nCols / 2;
			this._centerPos = new Vector2(asyncShip.vShipPos.x + (float)this._xBound, asyncShip.vShipPos.y - (float)this._yBound);
			Camera camera = CrewSim.objInstance.SwitchActiveCamera(false);
			camera.GetComponent<GameRenderer>().SetupForShipPreview(this._centerPos, this.renderTexture);
		}

		private void ScrollPosition(Camera screenShotCam, Vector2 projectedMousePosition)
		{
			if (GUIActionKeySelector.commandPanCameraLeft.Held || (projectedMousePosition.x > 0f && (double)projectedMousePosition.x < (double)screenShotCam.pixelWidth * 0.2))
			{
				screenShotCam.transform.Translate(Vector3.left * (this._camScrollingSpeed * Time.unscaledDeltaTime));
			}
			if (GUIActionKeySelector.commandPanCameraRight.Held || ((double)projectedMousePosition.x > (double)screenShotCam.pixelWidth * 0.8 && projectedMousePosition.x < (float)screenShotCam.pixelWidth))
			{
				screenShotCam.transform.Translate(Vector3.right * (this._camScrollingSpeed * Time.unscaledDeltaTime));
			}
			if (GUIActionKeySelector.commandPanCameraUp.Held || ((double)projectedMousePosition.y > (double)screenShotCam.pixelHeight * 0.8 && projectedMousePosition.y < (float)screenShotCam.pixelHeight))
			{
				screenShotCam.transform.Translate(Vector3.up * (this._camScrollingSpeed * Time.unscaledDeltaTime));
			}
			if (GUIActionKeySelector.commandPanCameraDown.Held || (projectedMousePosition.y > 0f && (double)projectedMousePosition.y < (double)screenShotCam.pixelHeight * 0.2))
			{
				screenShotCam.transform.Translate(Vector3.down * (this._camScrollingSpeed * Time.unscaledDeltaTime));
			}
		}

		private void ClampPosition(Transform camTransform)
		{
			Vector3 position = camTransform.position;
			if (camTransform.position.x < this._centerPos.x - (float)this._xBound)
			{
				position.x = this._centerPos.x - (float)this._xBound;
			}
			if (camTransform.position.x > this._centerPos.x + (float)this._xBound)
			{
				position.x = this._centerPos.x + (float)this._xBound;
			}
			if (camTransform.position.y < this._centerPos.y - (float)this._yBound)
			{
				position.y = this._centerPos.y - (float)this._yBound;
			}
			if (camTransform.position.y > this._centerPos.y + (float)this._yBound)
			{
				position.y = this._centerPos.y + (float)this._yBound;
			}
			camTransform.position = position;
		}

		private IEnumerator ShowLoadingAnimation(bool fadeUp)
		{
			float timePassed = 0f;
			float duration = (!fadeUp) ? 0f : 1.4f;
			while (duration > 0f)
			{
				duration -= Time.deltaTime;
				yield return null;
			}
			duration = 0.4f;
			float targetAlpha = (float)((!fadeUp) ? 0 : 1);
			while (this.cgSearchSubScreen.alpha != targetAlpha)
			{
				timePassed += Time.deltaTime;
				float blend = Mathf.Clamp01(timePassed / duration);
				float start = (float)((!fadeUp) ? 1 : 0);
				float end = (float)((!fadeUp) ? 0 : 1);
				this.cgSearchSubScreen.alpha = Mathf.Lerp(start, end, blend);
				yield return null;
			}
			this.statusContainer.SetActive(!fadeUp);
			yield break;
		}

		private IEnumerator ShowShipWhenLoaded(string regId)
		{
			Ship asyncShip = null;
			yield return new WaitUntil(() => MonoSingleton<AsyncShipLoader>.Instance.GetShip(regId, out asyncShip));
			this.EnablePreviewCamera(asyncShip);
			MonoSingleton<GUIItemList>.Instance.ShowAsyncShipTooltip();
			this._camFeed = true;
			this.renderRectTransform.GetComponent<RawImage>().enabled = true;
			this._renderImageSize = this.renderRectTransform.rect.size;
			yield return this.ShowLoadingAnimation(false);
			yield break;
		}

		[SerializeField]
		private CanvasGroup cg;

		[SerializeField]
		private RenderTexture renderTexture;

		[SerializeField]
		private RectTransform renderRectTransform;

		[SerializeField]
		private Button btnClose;

		[SerializeField]
		private CanvasGroup cgSearchSubScreen;

		[SerializeField]
		private TMP_Text txtConnecting;

		[SerializeField]
		private GameObject statusContainer;

		private readonly List<string> _loadedAsyncShips = new List<string>();

		private bool _camFeed;

		private float _duration;

		private Vector2 _renderImageSize = Vector2.zero;

		private int _xBound = 75;

		private int _yBound = 75;

		private Vector2 _centerPos;

		private float _camScrollingSpeed = 6f;
	}
}
