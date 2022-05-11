using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using Config;

namespace GTFSData
{
	#pragma warning disable IDE1006
	public struct GTFSStop
	{
		public string stop_id { get; set; }
		public string stop_name { get; set; }
		public double stop_lat { get; set; }
		public double stop_lon { get; set; }
	}

	public struct GTFSTrip
	{
		public string route_id { get; set; }
		public string trip_id { get; set; }
		public string service_id { get; set; }
	}

	public struct GTFSStopTime
	{
		public string trip_id { get; set; }
		public TimeSpan arrival_time { get; set; }
		public TimeSpan departure_time { get; set; }
		public string stop_id { get; set; }
	}

	public struct GTFSCalendar
	{
		public string service_id { get; set; }
		public bool monday { get; set; }
		public bool tuesday { get; set; }
		public bool wednesday { get; set; }
		public bool thursday { get; set; }
		public bool friday { get; set; }
		public bool saturday { get; set; }
		public bool sunday { get; set; }
		public DateTimeOffset start_date { get; set; }
		public DateTimeOffset end_date { get; set; }
	}

	public struct GTFSCalendarDate
	{
		public string service_id { get; set; }
		public DateTimeOffset date { get; set; }
		public byte exception_type { get; set; }
	}

	#pragma warning restore IDE1006

	/// <summary>
	/// Represents timetable data required for searching.
	/// </summary>
	public interface IRawData
	{
		public List<GTFSStop> Stops { get; }
		public List<GTFSTrip> Trips { get; }
		public List<GTFSStopTime> StopTimes { get; }
		public List<GTFSCalendarDate> CalendarDates { get; }
		public List<GTFSCalendar> Calendar { get; }
	}

	/// <summary>
	/// Represents GTFS data parsed from configured `PathToGTFSFolder`
	/// </summary>
	public sealed class RawData : IRawData
	{
		public List<GTFSStop> Stops { get; private set; }
		public List<GTFSTrip> Trips { get; private set; }
		public List<GTFSStopTime> StopTimes { get; private set; }
		public List<GTFSCalendarDate> CalendarDates { get; private set; }
		public List<GTFSCalendar> Calendar { get; private set; }

		private readonly Dictionary<Type, string> typeToPath = new()
		{
			{ typeof(GTFSStop), "/stops.txt" },
			{ typeof(GTFSTrip), "/trips.txt" },
			{ typeof(GTFSStopTime), "/stop_times.txt" },
			{ typeof(GTFSCalendarDate), "/calendar_dates.txt" },
			{ typeof(GTFSCalendar), "/calendar.txt" }
		};

		private static GTFSStopTime LoadStopTimeRecord(CsvReader csv)
		{
			var record = new GTFSStopTime
			{
				arrival_time = TimeConverter.TimeFrom(csv.GetField<string>("arrival_time")),
				departure_time = TimeConverter.TimeFrom(csv.GetField<string>("departure_time")),
				stop_id = csv.GetField<string>("stop_id"),
				trip_id = csv.GetField<string>("trip_id")
			};
			return record;
		}

		private static GTFSCalendarDate LoadCalendarDateRecord(CsvReader csv)
		{
			var record = new GTFSCalendarDate
			{
				service_id = csv.GetField<string>("service_id"),
				date = DateConverter.ConvertCSVDate(csv.GetField<string>("date")),
				exception_type = csv.GetField<byte>("exception_type")
			};
			return record;
		}

		private static GTFSCalendar LoadCalendarRecord(CsvReader csv)
		{
			var record = new GTFSCalendar
			{
				service_id = csv.GetField<string>("service_id"),
				monday = csv.GetField<bool>("monday"),
				tuesday = csv.GetField<bool>("tuesday"),
				wednesday = csv.GetField<bool>("wednesday"),
				thursday = csv.GetField<bool>("thursday"),
				friday = csv.GetField<bool>("friday"),
				saturday = csv.GetField<bool>("saturday"),
				sunday = csv.GetField<bool>("sunday"),
				start_date = DateConverter.ConvertCSVDate(csv.GetField<string>("start_date")),
				end_date = DateConverter.ConvertCSVDate(csv.GetField<string>("end_date")),
			};
			return record;
		}

		private List<T> LoadDataSpecial<T>(string filePath, Func<CsvReader, T> getRecord)
		{
			filePath += typeToPath[typeof(T)];
			List<T> result;
			using (var reader = new StreamReader(filePath))
			using (var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
			{
				var records = new List<T>();
				csv.Read();
				csv.ReadHeader();
				while (csv.Read())
				{
					var record = getRecord(csv);
					records.Add(record);
				}
				result = new List<T>();
				foreach (var r in records)
				{
					result.Add(r);
				}
			}
			return result;
		}

		private List<T> LoadData<T>(string filePath)
		{
			filePath += typeToPath[typeof(T)];
			List<T> result;
			using (var reader = new StreamReader(filePath))
			using (var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
			{
				var records = csv.GetRecords<T>();
				result = new List<T>();
				foreach (var r in records)
				{
					result.Add(r);
				}
			}
			return result;
		}

		/// <summary>
		/// Parses GTFS data
		/// Data are accessible through properties
		/// </summary>
		public RawData()
		{
			string pathToGTFSFolder = AppConfig.appSettings.PathToGTFSFolder;

			Stops = LoadData<GTFSStop>(pathToGTFSFolder);
			Trips = LoadData<GTFSTrip>(pathToGTFSFolder);
			StopTimes = LoadDataSpecial(pathToGTFSFolder, LoadStopTimeRecord);
			CalendarDates = LoadDataSpecial(pathToGTFSFolder, LoadCalendarDateRecord);
			Calendar = LoadDataSpecial(pathToGTFSFolder, LoadCalendarRecord);
		}
	}
}
