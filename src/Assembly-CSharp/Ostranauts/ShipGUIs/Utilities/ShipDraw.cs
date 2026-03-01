using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.NavStation;
using TMPro;
using UnityEngine;
using Vectrosity;

namespace Ostranauts.ShipGUIs.Utilities
{
	// Nav/overview drawing helper for one ship. Owns label groups, body/path lines,
	// silhouette toggles, and extra projection lines used by ship GUIs.
	public class ShipDraw
	{
		// Binds the draw helper to one runtime Ship instance.
		public ShipDraw(Ship ship)
		{
			this.ship = ship;
		}

		// Returns whichever label group is currently selected as active by the shared
		// nav-draw label mode.
		public OrbitDrawLabelGroup LabelActive
		{
			get
			{
				if (ShipDraw.ActiveLabel == 1)
				{
					this.lastActive = this.LabelID;
					return this.LabelID;
				}
				if (ShipDraw.ActiveLabel == 2)
				{
					this.lastActive = this.LabelName;
					return this.LabelName;
				}
				return this.lastActive;
			}
		}

		public float fRadiusM
		{
			get
			{
				return (float)this.ship.objSS.Size;
			}
		}

		public float fRadiusAU
		{
			get
			{
				return this.ship.objSS.GetRadiusAU();
			}
		}

		// Registers an extra projection/path line for later cleanup.
		public void AddProjection(VectorLine vl)
		{
			if (this.aProjs == null)
			{
				this.aProjs = new List<VectorLine>();
			}
			this.aProjs.Add(vl);
		}

		public List<VectorLine> GetProjection()
		{
			return this.aProjs;
		}

		// Swaps between the symbolic line art and the cached silhouette outline.
		public void ToggleSilhouetteDrawMode(bool showSilhouette)
		{
			if (showSilhouette && this.silhouetteDrawPoints != null)
			{
				if (this.symbolDrawPoints == null || this.symbolDrawPoints.Count == 0)
				{
					this.symbolDrawPoints = this.lineBody.points2;
					this.symbolLineType = this.lineBody.lineType;
				}
				this.lineBody.lineType = LineType.Continuous;
				this.lineBody.points2 = this.silhouetteDrawPoints;
			}
			else if (this.symbolDrawPoints != null)
			{
				if (this.symbolLineType != this.lineBody.lineType)
				{
					this.lineBody.lineType = this.symbolLineType;
				}
				this.lineBody.points2 = this.symbolDrawPoints;
			}
		}

		// Releases labels, vector lines, and cached draw data for this ship.
		public void Destroy()
		{
			this.shipInfo = null;
			if (this.symbolDrawPoints != null)
			{
				this.symbolDrawPoints.Clear();
				this.symbolDrawPoints = null;
			}
			if (this.silhouetteDrawPoints != null)
			{
				this.silhouetteDrawPoints.Clear();
				this.silhouetteDrawPoints = null;
			}
			this.cgID = null;
			this.cgName = null;
			this.txtLabelName = null;
			if (this.lastActive != null)
			{
				this.lastActive.Destroy();
				this.lastActive = null;
			}
			if (this.LabelID != null)
			{
				this.LabelID.Destroy();
				this.LabelID = null;
			}
			if (this.LabelName != null)
			{
				this.LabelName.Destroy();
				this.LabelName = null;
			}
			this.ship = null;
			VectorLine.Destroy(ref this.lineBody);
			VectorLine.Destroy(ref this.linePath);
			if (this.lineStatus != null)
			{
				VectorLine.Destroy(ref this.lineStatus);
			}
			if (this.lineNoWakeRange != null)
			{
				VectorLine.Destroy(ref this.lineNoWakeRange);
			}
			this.tfLabelID = null;
			this.txtLabelID = null;
			UnityEngine.Object.Destroy(this.goLabel);
			UnityEngine.Object.Destroy(this.tfLabelName.gameObject);
			if (this.aProjs != null)
			{
				VectorLine.Destroy(this.aProjs);
				this.aProjs = null;
			}
		}

		public void SetShipInfo(ShipInfo si)
		{
			this.shipInfo = si;
			if (si.Known && this.ship.IsFlyingDark())
			{
				this.sDisplayName = "(" + si.publicName + ")";
				this.sDisplayRegID = "(" + si.strRegID + ")";
			}
			else
			{
				this.sDisplayName = si.publicName;
				this.sDisplayRegID = si.strRegID;
			}
			if (si.isTutorialDerelict)
			{
				this.sDisplayName = "(TUTORIAL DERELICT)";
				this.sDisplayRegID = "(TUTORIAL DERELICT)";
			}
		}

		public void SetupLabel(Transform tfOrbitLabel, Transform tfOrbitalPanel)
		{
			this.tfLabelID = (UnityEngine.Object.Instantiate<Transform>(tfOrbitLabel, tfOrbitalPanel).transform as RectTransform);
			this.txtLabelID = this.tfLabelID.GetComponent<TextMeshProUGUI>();
			this.cgID = this.tfLabelID.GetComponent<CanvasGroup>();
			this.tfLabelName = (UnityEngine.Object.Instantiate<Transform>(tfOrbitLabel, tfOrbitalPanel).transform as RectTransform);
			this.txtLabelName = this.tfLabelName.GetComponent<TextMeshProUGUI>();
			this.cgName = this.tfLabelName.GetComponent<CanvasGroup>();
			this.LabelID = new OrbitDrawLabelGroup
			{
				shipDraw = this,
				active = true,
				cg = this.cgID,
				label = this.txtLabelID,
				labelRect = this.tfLabelID
			};
			this.LabelName = new OrbitDrawLabelGroup
			{
				shipDraw = this,
				active = false,
				cg = this.cgName,
				label = this.txtLabelName,
				labelRect = this.tfLabelName
			};
			this.txtLabelID.text = this.sDisplayRegID;
			this.txtLabelID.color = this.lineBody.color;
			this.goLabel = this.tfLabelID.gameObject;
			this.txtLabelName.text = this.sDisplayName;
			this.txtLabelName.color = this.lineBody.color;
			this.txtLabelID.rectTransform.sizeDelta = new Vector2(this.txtLabelID.preferredWidth, this.txtLabelID.rectTransform.sizeDelta.y);
			this.txtLabelID.ForceMeshUpdate();
			this.txtLabelID.rectTransform.sizeDelta = new Vector2(this.txtLabelID.rectTransform.sizeDelta.x, this.txtLabelID.preferredHeight);
			int activeLabel = ShipDraw.ActiveLabel;
			if (activeLabel != 0)
			{
				if (activeLabel != 1)
				{
					if (activeLabel == 2)
					{
						this.LabelID.cg.alpha = 0f;
						this.LabelName.cg.alpha = 1f;
					}
				}
				else
				{
					this.LabelID.cg.alpha = 1f;
					this.LabelName.cg.alpha = 0f;
				}
			}
			else
			{
				this.LabelID.cg.alpha = 0f;
				this.LabelName.cg.alpha = 0f;
			}
			Vector2 preferredValues = this.txtLabelName.GetPreferredValues(this.sDisplayName, 75f, 17f);
			float y = preferredValues.y;
			if (preferredValues.y > 17f)
			{
				string[] array = this.sDisplayName.Split(new char[]
				{
					' '
				});
				string text = array[0];
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Length > text.Length)
					{
						text = array[i];
					}
				}
				preferredValues = this.txtLabelName.GetPreferredValues(text, 75f, 17f);
				preferredValues = new Vector2(preferredValues.x + 5f, this.txtLabelName.GetPreferredValues(this.sDisplayName, preferredValues.x, 17f).y);
			}
			this.txtLabelName.rectTransform.sizeDelta = preferredValues;
		}

		public void ToggleStatusSymbol()
		{
			if (this.ship.IsAIShip && this.ship.shipScanTarget != null && this.ship.shipScanTarget.IsPlayerShip() && !this.ship.shipScanTarget.IsStation(false) && this.ship.NavAIManned)
			{
				this.SetTracking();
				return;
			}
			if (this.lineStatus != null)
			{
				VectorLine.Destroy(ref this.lineStatus);
				this.lineStatus = null;
			}
		}

		private void SetTracking()
		{
			string text = "Tracking";
			if (this.lineStatus != null && this.lineStatus.name == text)
			{
				return;
			}
			if (this.lineStatus != null)
			{
				VectorLine.Destroy(ref this.lineStatus);
			}
			this.lineStatus = NavIcon.SetupVectorLine(text, new Color(0.94921875f, 0.24609375f, 0.1796875f, 0.9f), GUIRenderTargets.goLines.transform.Find("pnlOrbits").gameObject, NavIcon.Bracket(this.fRadiusM));
		}

		public static bool blink;

		public static int KnobState;

		public static int ActiveLabel;

		public Ship ship;

		public ShipInfo shipInfo;

		public VectorLine lineBody;

		public VectorLine lineStatus;

		public VectorLine linePath;

		public VectorLine lineNoWakeRange;

		public List<Vector2> symbolDrawPoints;

		private LineType symbolLineType;

		public List<Vector2> silhouetteDrawPoints;

		public RectTransform tfLabelID;

		public RectTransform tfLabelName;

		public CanvasGroup cgID;

		public CanvasGroup cgName;

		public GameObject goLabel;

		private List<VectorLine> aProjs;

		public TextMeshProUGUI txtLabelID;

		public TextMeshProUGUI txtLabelName;

		public bool DoNotRotate;

		public Vector3 desiredCanvasPosFromSitu;

		public Vector3 lastDrawnLabelPos;

		public Vector3 sharedLabelVelocity;

		public Vector3 sharedLabelOffsetTarget;

		public Vector3 sharedLabelOffsetCurrent;

		public float sharedAlphaTarget;

		public float sharedAlphaCurrent;

		public float sharedAlphaVelocity;

		private OrbitDrawLabelGroup lastActive;

		public OrbitDrawLabelGroup LabelID;

		public OrbitDrawLabelGroup LabelName;

		public string sDisplayName = string.Empty;

		public string sDisplayRegID = string.Empty;

		public float fCanvasX;

		public float fCanvasY;
	}
}
