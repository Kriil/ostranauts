using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Vectrosity;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class DebugDraw
	{
		public DebugDraw(string name, ShipSitu targetSitu, VectorLine lineBody)
		{
			this._displayName = name;
			this.TargetSitu = targetSitu;
			this._removalTimestamp = StarSystem.fEpoch + 3000.0;
			this.LineBody = lineBody;
		}

		public DebugDraw(string name, ShipSitu targetSitu, bool isPrediction, VectorLine lineBody) : this(name, targetSitu, lineBody)
		{
			if (!isPrediction)
			{
				return;
			}
			this.IsPrediction = isPrediction;
			this._removalTimestamp = StarSystem.fEpoch + 3000.0;
			this.LineBody = new VectorLine("Course Vector", new List<Vector2>(new Vector2[]
			{
				default(Vector2),
				new Vector2(1f, 1f)
			}), 1.5f, LineType.Discrete, Joins.Weld);
			this.LineBody.color = Color.red;
		}

		public ShipSitu TargetSitu { get; private set; }

		public bool IsPrediction { get; private set; }

		public void Destroy()
		{
			VectorLine.Destroy(ref this.LineBody);
			this.tfLabel = null;
			this.txtLabel = null;
			UnityEngine.Object.Destroy(this.goLabel);
		}

		public bool Evaluate()
		{
			if (this._removalTimestamp - StarSystem.fEpoch < 0.0 || this.TargetSitu == null)
			{
				this.Destroy();
				return true;
			}
			return false;
		}

		public void MarkForRemoval()
		{
			this._removalTimestamp = 0.0;
		}

		public void SetupLabel(Transform tfOrbitLabel, GameObject goOrbitPanel)
		{
			this.tfLabel = UnityEngine.Object.Instantiate<Transform>(tfOrbitLabel, goOrbitPanel.transform).GetComponent<RectTransform>();
			this.txtLabel = this.tfLabel.GetComponent<TMP_Text>();
			this.txtLabel.text = this._displayName;
			this.LineBody.SetCanvas(goOrbitPanel, false);
			this.txtLabel.color = this.LineBody.color;
			this.goLabel = this.tfLabel.gameObject;
		}

		public static readonly int SIZE = 100;

		public VectorLine LineBody;

		public RectTransform tfLabel;

		public GameObject goLabel;

		private TMP_Text txtLabel;

		public string _displayName = string.Empty;

		private double _removalTimestamp;
	}
}
