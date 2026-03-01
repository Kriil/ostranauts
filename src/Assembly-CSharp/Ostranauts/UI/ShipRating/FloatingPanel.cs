using System;
using Ostranauts.Core.Models;
using Ostranauts.TargetVisualization;
using Ostranauts.Tools.ExtensionMethods;
using TMPro;
using UnityEngine;

namespace Ostranauts.UI.ShipRating
{
	public class FloatingPanel : MonoBehaviour
	{
		private void Awake()
		{
			this.rectTransform = base.GetComponent<RectTransform>();
			this.rectTransform.localScale = new Vector3(this._scale, this._scale, 1f);
		}

		public void SetData(string textToDisplay, Transform other)
		{
			this.txtFloatUI.text = textToDisplay;
			Transform closestPoint = this.GetClosestPoint(other.position);
			this._vUs = closestPoint.transform.position.ToVector2();
			this._vThem = other.transform.position.ToVector2();
			this.targetLine.SetData(other, closestPoint);
			this.container.SetActive(false);
		}

		public bool IsIntersecting(Vector3 origin, Vector3 target)
		{
			return MathUtils.AreLinesIntersecting(this._vUs, this._vThem, origin.ToVector2(), target.ToVector2());
		}

		private Transform GetClosestPoint(Vector3 position)
		{
			Vector3 vector = new Vector3(position.x, position.y, this.top.position.z);
			Tuple<Transform, float> tuple = new Tuple<Transform, float>(this.top, Vector3.Distance(vector, this.top.position));
			this.GetCloserOne(ref tuple, this.bottom, vector);
			this.GetCloserOne(ref tuple, this.left, vector);
			this.GetCloserOne(ref tuple, this.right, vector);
			return tuple.Item1;
		}

		private void GetCloserOne(ref Tuple<Transform, float> closestPair, Transform point, Vector3 target)
		{
			float num = Vector3.Distance(target, point.position);
			if (num < closestPair.Item2)
			{
				closestPair.Item2 = num;
				closestPair.Item1 = point;
			}
		}

		public void Hide()
		{
			this.container.SetActive(false);
		}

		public void Show(float orthoSize)
		{
			this.container.SetActive(true);
			float t = Mathf.InverseLerp(20f, 50f, orthoSize);
			this._scale = Mathf.Lerp(0.07f, 0.25f, t);
			this.rectTransform.localScale = new Vector3(this._scale, this._scale, 1f);
		}

		[SerializeField]
		private TMP_Text txtFloatUI;

		[SerializeField]
		private TargetLine targetLine;

		[SerializeField]
		private GameObject container;

		[Header("Edge points")]
		[SerializeField]
		private Transform top;

		[SerializeField]
		private Transform bottom;

		[SerializeField]
		private Transform left;

		[SerializeField]
		private Transform right;

		private float _scale = 0.08f;

		private RectTransform rectTransform;

		private Vector2 _vUs;

		private Vector2 _vThem;
	}
}
