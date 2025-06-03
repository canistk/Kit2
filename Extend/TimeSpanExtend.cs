using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kit2
{
	public static class TimeSpanExtend
    {
		public static string ToStringDetail(this TimeSpan ts)
		{
			return
				$"{(ts.TotalDays			>= 1f ? $"{(int)ts.TotalDays}d "	: "")}" +
				$"{(ts.TotalHours			>= 1f ? $"{ts.Hours}h "				: "")}" +
				$"{(ts.TotalMinutes			>= 1f ? $"{ts.Minutes}m "			: "")}" +
				$"{(ts.TotalSeconds			>= 1f ? $"{ts.Seconds}s "			: "")}" +
				$"{(ts.TotalMilliseconds	>= 1f ? $"{ts.Milliseconds}ms"		: "")}".Trim();
		}

		public static string ToStringDetail(this Stopwatch stopwatch)
			=> new TimeSpan(stopwatch.ElapsedTicks).ToStringDetail();

	}
}
