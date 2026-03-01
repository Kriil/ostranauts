using System;
using Ostranauts.Utils.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModFlightDynamics : NavModBase
	{
		private new void Awake()
		{
			base.Awake();
			this._colorAoA = this.imgAngleOfAttack.color;
		}

		protected override void UpdateUI()
		{
			if (this.COSelf == null || this.COSelf.ship == null)
			{
				return;
			}
			ShipSitu objSS = this.COSelf.ship.objSS;
			Vector2 vector = default(Vector2);
			BodyOrbit bodyOrbit = null;
			BodyOrbit greatestGravBO = CrewSim.system.GetGreatestGravBO(objSS, StarSystem.fEpoch, ref vector, ref bodyOrbit);
			double magnitude = MathUtils.GetMagnitude(greatestGravBO.vPos, objSS.vPos);
			JsonAtmosphere atmosphereAtDistance = greatestGravBO.GetAtmosphereAtDistance(magnitude);
			float num = atmosphereAtDistance.fH2 + atmosphereAtDistance.fHe2 + atmosphereAtDistance.fCH4 + atmosphereAtDistance.fCO2 + atmosphereAtDistance.fN2 + atmosphereAtDistance.fO2 + atmosphereAtDistance.fH2O + atmosphereAtDistance.fNH3 + atmosphereAtDistance.fH2SO4;
			Point a = objSS.vVel - greatestGravBO.vVel;
			Point directionVector = objSS.GetDirectionVector(false);
			double num2 = MathUtils.GetAngleBetweenVectors(a, directionVector, true);
			float num3 = MathUtils.GetMagnitude(this.COSelf.ship.objSS.vAccLift.x, this.COSelf.ship.objSS.vAccLift.y) / 6.684587E-12f / 9.81f;
			float num4 = MathUtils.GetMagnitude(this.COSelf.ship.objSS.vAccDrag.x, this.COSelf.ship.objSS.vAccDrag.y) / 6.684587E-12f / 9.81f;
			float num5 = MathUtils.GetMagnitude(vector.x, vector.y) / 6.684587E-12f / 9.81f;
			this.txtLift.text = (((double)num3 <= 0.01) ? "-" : (num3.ToString("F2") + "G"));
			this.txtDrag.text = (((double)num4 <= 0.01) ? "-" : (num4.ToString("F2") + "G"));
			if ((double)num < 0.01 || double.IsNaN(num2))
			{
				num2 = 0.0;
			}
			this.imgShip.transform.rotation = Quaternion.Euler(base.transform.eulerAngles.x, base.transform.eulerAngles.y, (float)num2);
			double num6 = num2;
			if (num2 > 180.0)
			{
				num6 = 360.0 - num2;
			}
			this.imgAngleOfAttack.color = new Color(this._colorAoA.r, this._colorAoA.g, this._colorAoA.b, Mathf.InverseLerp(0f, 180f, (float)num6));
			this.txtAngleOfAttack.text = (((double)num < 0.01) ? "-" : (num6.ToString("F2") + "°"));
			this.txtGravity.text = (((double)num5 < 0.01) ? "-" : (num5.ToString("F2") + "G"));
			this.txtDensity.text = (((double)num < 0.01) ? "-" : (num.ToString("F2") + " kPa"));
		}

		[SerializeField]
		private Image imgShip;

		[SerializeField]
		private Image imgAngleOfAttack;

		[SerializeField]
		private TMP_Text txtLift;

		[SerializeField]
		private TMP_Text txtDrag;

		[SerializeField]
		private TMP_Text txtAngleOfAttack;

		[SerializeField]
		private TMP_Text txtGravity;

		[SerializeField]
		private TMP_Text txtDensity;

		private Color _colorAoA;
	}
}
