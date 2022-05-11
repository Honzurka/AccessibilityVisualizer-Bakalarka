using System;
using Config;


namespace GTFSData
{
	/// <summary>
	/// Helper class for calculations working with time.
	/// </summary>
	public static class TimeConverter
	{
		/// <summary>
		/// UTC offset of timetable data.
		/// UTC offset is set in configuration file.
		/// </summary>
		public static TimeSpan UTCOffset { get; private set; }

		static TimeConverter()
		{
			UTCOffset = TimeSpan.FromHours(AppConfig.appSettings.UTCOffset);
		}

		/// <summary>
		/// Parses time from string in more than classic 24-hour format.
		/// </summary>
		/// <returns>TimeSpan with possible day part defined</returns>
		public static TimeSpan TimeFrom(string time)
		{
			var toks = time.Split(':');
			var d = int.Parse(toks[0]) / 24;
			var h = int.Parse(toks[0]) % 24;
			var m = int.Parse(toks[1]);
			var s = int.Parse(toks[2]);

			return new TimeSpan(d, h, m, s);
		}
	}
}
