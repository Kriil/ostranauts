using System;
using UnityEngine;

public class BodyTemp : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.coUs = base.GetComponent<CondOwner>();
	}

	private void Update()
	{
		if (this.dfEpochLast <= 0.0)
		{
			this.dfEpochLast = StarSystem.fEpoch;
		}
		double num = StarSystem.fEpoch - this.dfEpochLast;
		if (num >= this.dfUpdateInterval)
		{
			this.Exposure(num / 3600.0);
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void ForceUpdate()
	{
		this.dfEpochLast = StarSystem.fEpoch - this.dfUpdateInterval;
		this.Update();
	}

	public void CatchUp()
	{
		this.dfEpochLast = StarSystem.fEpoch;
	}

	private void Exposure(double fHoursPassed)
	{
		bool flag = this.coUs.HasCond("IsWet");
		this.fMinSafeTemp = this.coUs.GetCondAmount("StatSafeTempMin");
		this.fMaxSafeTemp = this.coUs.GetCondAmount("StatSafeTempMax");
		if (flag)
		{
			double condAmount = this.coUs.GetCondAmount("StatSafeTempWet");
			this.fMinSafeTemp += condAmount;
			this.fMaxSafeTemp += condAmount;
		}
		double num = this.coUs.GetCondAmount("StatBodyInsulation");
		num = MathUtils.Clamp(num, 0.0, 1.0);
		double condAmount2 = this.coUs.GetCondAmount("StatPassiveRewarmTemp");
		double condAmount3 = this.coUs.GetCondAmount("StatSolidTemp");
		double num2 = condAmount3;
		Room room = null;
		if (this.coUs.ship != null)
		{
			room = this.coUs.ship.GetRoomAtWorldCoords1(this.coUs.transform.position, true);
		}
		if (room != null && room.CO != null)
		{
			this.fAmbientTemp = room.CO.GetCondAmount("StatGasTemp");
			this.fAmbientPressure = room.CO.GetCondAmount("StatGasPressure");
		}
		double num3 = 0.0;
		if ((this.fAmbientTemp < this.fMinSafeTemp && condAmount3 < this.fNormalBodyTemp) || (this.fAmbientTemp > this.fMaxSafeTemp && condAmount3 > this.fNormalBodyTemp))
		{
			if (this.fAmbientTemp < this.fMinSafeTemp)
			{
				num3 = this.fAmbientTemp - this.fMinSafeTemp;
				if (condAmount3 < this.fMinSafeTemp)
				{
					num3 = this.fAmbientTemp - condAmount3;
				}
			}
			else
			{
				num3 = this.fAmbientTemp - this.fMaxSafeTemp;
				if (condAmount3 > this.fMaxSafeTemp)
				{
					num3 = this.fAmbientTemp - condAmount3;
				}
			}
			num3 = 5.56 * (double)Mathf.Pow(Convert.ToSingle(num3 / 27.9), 3f);
			num3 *= 1.0 - num;
			num3 *= MathUtils.Clamp(this.fAmbientPressure, 0.1, this.fAmbientPressure) / 101.30000305175781;
		}
		else if (condAmount3 > this.fNormalBodyTemp)
		{
			num2 -= condAmount2 * fHoursPassed;
		}
		else
		{
			num2 += condAmount2 * fHoursPassed;
		}
		num2 += num3 * fHoursPassed;
		if (num >= 0.8 && !this.coUs.HasCond("IsWearingSuitCooled") && this.fAmbientPressure < 101.30000305175781)
		{
			double num4 = condAmount2 * fHoursPassed;
			num4 *= num;
			num4 *= 1.0 - this.fAmbientPressure / 101.30000305175781;
			num2 += num4;
		}
		double condAmount4 = this.coUs.GetCondAmount("StatGasPressure");
		if (condAmount4 != this.fAmbientPressure && !this.coUs.HasCond("IsAirtight"))
		{
			this.coUs.SetCondAmount("StatGasPressure", this.fAmbientPressure, 0.0);
		}
		this.atmoTemp = BodyTemp.AtmoTemp.Normal;
		if (num2 < condAmount3 && condAmount3 < this.fNormalBodyTemp)
		{
			double num5 = condAmount3 - num2;
			if (!this.coUs.HasCond("DcBodyTemp05") || num5 > 0.00067)
			{
				this.atmoTemp = BodyTemp.AtmoTemp.Cold;
			}
		}
		else if (num2 > condAmount3 && condAmount3 > this.fNormalBodyTemp)
		{
			double num6 = num2 - condAmount3;
			if (!this.coUs.HasCond("DcBodyTemp05") || num6 > 0.00067)
			{
				this.atmoTemp = BodyTemp.AtmoTemp.Hot;
			}
		}
		this.coUs.AddCondAmount("StatSolidTemp", num2 - condAmount3, 0.0, 0f);
		this.dfEpochLast = StarSystem.fEpoch;
		this.coUs.mapInfo["Body Temp"] = MathUtils.GetTemperatureString(num2);
	}

	private CondOwner coUs;

	private double dfEpochLast = -1.0;

	private double dfUpdateInterval = 1.0;

	private double fNormalBodyTemp = 310.16;

	private double fAmbientTemp;

	private double fAmbientPressure;

	private double fMinSafeTemp;

	private double fMaxSafeTemp;

	public BodyTemp.AtmoTemp atmoTemp;

	private const double TEMP_RATE_MIN_THRESHOLD = 0.00067;

	public enum AtmoTemp
	{
		Normal,
		Hot,
		Cold
	}
}
