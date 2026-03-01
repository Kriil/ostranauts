using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Parallax.UI.Portrait
{
	public class PortraitContainer : MonoBehaviour
	{
		public string CoId { get; set; }

		private void CreateTexture()
		{
			this._portraitTexture = new RenderTexture(572, 715, 32, RenderTextureFormat.ARGB32);
			this._portraitTexture.dimension = TextureDimension.Tex2D;
			this._portraitTexture.autoGenerateMips = false;
			this._portraitTexture.Create();
			this._portraitCamera.targetTexture = this._portraitTexture;
		}

		private void OnDestroy()
		{
			this._portraitTexture.Release();
		}

		public Texture SetFaceAnim(CondOwner co, bool force = false)
		{
			if (this._portraitTexture == null)
			{
				this.CreateTexture();
			}
			this.CoId = co.strID;
			this._faceAnim.SetFace(co, force);
			return this._portraitTexture;
		}

		public void SetTransform(Vector3? vMousePosNorm)
		{
			Vector3 position = this._faceAnim.transform.position;
			if (vMousePosNorm == null)
			{
				position.x = this._faceAnim.fFaceOffsetX;
				this._faceAnim.transform.position = position;
			}
			else
			{
				this._faceAnim.transform.position = new Vector3(this._faceAnim.fFaceOffsetX + vMousePosNorm.Value.x, position.y, position.z);
			}
		}

		public void UpdateFace(string strCond, bool bRemove)
		{
			this._faceAnim.RecordCond(strCond, bRemove);
		}

		[SerializeField]
		private FaceAnim2 _faceAnim;

		[SerializeField]
		private Camera _portraitCamera;

		private RenderTexture _portraitTexture;
	}
}
