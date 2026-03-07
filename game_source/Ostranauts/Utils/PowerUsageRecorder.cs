using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;

namespace Ostranauts.Utils
{
	public class PowerUsageRecorder
	{
		private void CleanupExpiredValues()
		{
			while (this._observedPowerUsage.Count > 0 && this._observedPowerUsage.First.Value.Item1 < Math.Truncate(StarSystem.fEpoch) - (double)PowerUsageRecorder.ObservedTimeSpan)
			{
				this._observedPowerUsage.RemoveFirst();
			}
		}

		private double GetAverage()
		{
			this.CleanupExpiredValues();
			if (this._observedPowerUsage.Count == 0)
			{
				return 0.0;
			}
			double num = this._observedPowerUsage.Last.Value.Item1 - this._observedPowerUsage.First.Value.Item1;
			double num2 = (num <= 0.0 || num >= (double)PowerUsageRecorder.ObservedTimeSpan) ? ((double)PowerUsageRecorder.ObservedTimeSpan) : Math.Truncate(num);
			return this._observedPowerUsage.Sum((Tuple<double, double> x) => x.Item2) / num2;
		}

		public void RecordChange(double currentPowerDrain)
		{
			this._observedPowerUsage.AddLast(new Tuple<double, double>(Math.Truncate(StarSystem.fEpoch), currentPowerDrain));
			this.CleanupExpiredValues();
		}

		public PowerUsageDTO GetStatsString(double storedPower, double maxStorage)
		{
			if (storedPower <= 0.0)
			{
				return new PowerUsageDTO();
			}
			double average = this.GetAverage();
			if (average == 0.0)
			{
				return new PowerUsageDTO();
			}
			double dfAmount = Math.Abs(storedPower / average);
			string powerRemainingTime = (average > 0.0) ? this.GetChargingApproximation(maxStorage, storedPower, average) : MathUtils.GetDurationFromS(dfAmount, 4);
			return new PowerUsageDTO
			{
				PowerRemainingTime = powerRemainingTime,
				PowerCurrentLoad = ((average <= 0.0) ? string.Empty : "+") + this.GetLoadString(average)
			};
		}

		private string GetLoadString(double avgRate)
		{
			if (avgRate == 0.0)
			{
				return string.Empty;
			}
			double num = avgRate * 3600.0;
			string result = string.Empty;
			double num2 = Math.Abs(num);
			if (num2 > 10.0)
			{
				result = num.ToString("N0") + " kW";
			}
			else if (num2 > 0.1)
			{
				result = num.ToString("n2") + " kW";
			}
			else if (num2 > 0.001)
			{
				result = (num * 1000.0).ToString("n2") + " W";
			}
			else
			{
				result = (num * 1000.0 * 1000.0).ToString("n2") + " mW";
			}
			return result;
		}

		private string GetChargingApproximation(double maxStorage, double currentlyStoredPower, double avg)
		{
			double num = maxStorage * 0.9900000095367432;
			if (currentlyStoredPower >= num)
			{
				return "Trickle Charge";
			}
			double num2 = maxStorage - currentlyStoredPower;
			if (avg >= num2 * 0.001)
			{
				double dfAmount = 1000.0 * Math.Log(num2 / (maxStorage - num));
				return MathUtils.GetDurationFromS(dfAmount, 4);
			}
			double num3 = maxStorage - avg / 0.001;
			if (num3 >= num)
			{
				return MathUtils.GetDurationFromS((num - currentlyStoredPower) / avg, 4);
			}
			double num4 = (num3 - currentlyStoredPower) / avg;
			double num5 = 1000.0 * Math.Log((maxStorage - num3) / (maxStorage - num));
			return MathUtils.GetDurationFromS(num4 + num5, 4);
		}

		private readonly LinkedList<Tuple<double, double>> _observedPowerUsage = new LinkedList<Tuple<double, double>>();

		private static readonly int ObservedTimeSpan = 10;
	}
}
