using System;
using System.Globalization;
using UnityEngine;

namespace Ostranauts.Utils
{
	public static class TimeUtils
	{
		public static DateTime FromUnixTimeMillis(long unixTime)
		{
			return TimeUtils.EpochZero.AddMilliseconds((double)unixTime);
		}

		public static DateTime FromUnixTimeSeconds(double unixTime)
		{
			return TimeUtils.EpochZero.AddSeconds(unixTime);
		}

		public static long ConvertStringDate(string realWorldTime)
		{
			if (string.IsNullOrEmpty(realWorldTime))
			{
				return 0L;
			}
			try
			{
				DateTime d = DateTime.ParseExact(realWorldTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				return (long)(d - TimeUtils.EpochZero).TotalMilliseconds;
			}
			catch (FormatException)
			{
				Debug.Log(realWorldTime + " is not in the correct format.");
			}
			return 0L;
		}

		public static double GetCurrentEpochTimeSeconds()
		{
			return Math.Floor((DateTime.Now - TimeUtils.EpochZero).TotalSeconds);
		}

		public static readonly DateTime EpochZero = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}
}
