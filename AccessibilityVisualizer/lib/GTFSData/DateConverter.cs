using System;
using System.Globalization;

namespace GTFSData
{
	public static class DateConverter
	{
		/// <summary>
		/// Converts YYYYMMDD format to DateTimeOffset and sets correct UTC offset.
		/// </summary>
		public static DateTimeOffset ConvertCSVDate(string date)
		{
			return new DateTimeOffset(DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture), TimeConverter.UTCOffset);
		}
	}
}
