using System;
using UnityEngine;

namespace Ostranauts.TargetVisualization
{
	[RequireComponent(typeof(LineRenderer))]
	public class TargetLine : MonoBehaviour
	{
		private void Awake()
		{
			this._lineRenderer = base.GetComponent<LineRenderer>();
		}

		private void FixedUpdate()
		{
			if (this._origin == null || this._target == null)
			{
				return;
			}
			if (this._attackMode && (this._ia == null || this._ia.objUs == null || !this._ia.objUs.bAlive || this._ia.objUs.HasCond("Unconscious")))
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			Vector3 vector = new Vector3(this._origin.position.x, this._origin.position.y, -5f);
			Vector3 a = new Vector3(this._target.position.x, this._target.position.y, -5f);
			Vector3 vector2 = (a - vector).normalized * 0.15f;
			Vector3 b = new Vector3(vector2.y, -vector2.x, -5f);
			this._lineRenderer.SetPosition(0, vector + b);
			this._lineRenderer.SetPosition(1, a + b);
		}

		public void SetData(Interaction ia)
		{
			if (ia == null || ia.objUs == null || ia.objThem == null)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			this._ia = ia;
			this._origin = ia.objUs.transform;
			this._target = ia.objThem.transform;
		}

		public void SetData(Transform other, Transform origin)
		{
			this._attackMode = false;
			this._origin = origin;
			this._target = other;
		}

		private const float _zPosition = -5f;

		private const float _offset = 0.15f;

		private LineRenderer _lineRenderer;

		private Transform _origin;

		private Transform _target;

		private Interaction _ia;

		private bool _attackMode = true;
	}
}
