using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics; //stopwatch + Debug.Assert
using System;
using RBush;
using Config;

//using System.Threading; //used for Germany

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RaptorAlgoTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GTFSDataTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StopAccessibilityTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestShares")]


namespace GTFSData
{
	/// <summary>
	/// Represents arrival and departure from stop in some trip.
	/// </summary>
	public struct StopTime
	{
		[JsonProperty]
		internal TimeSpan Arrival_time { get; private set; }
		[JsonProperty]
		internal TimeSpan Departure_time { get; private set; }
		internal StopTime(TimeSpan arrival_time, TimeSpan departure_time)
		{
			Arrival_time = arrival_time;
			Departure_time = departure_time;
		}


		// used only in tests (mainly due to backward compatibility) --- could be rewritten
		internal StopTime(uint arrival_time, uint departure_time)
		{
			Arrival_time = TimeSpan.FromSeconds(arrival_time);
			Departure_time = TimeSpan.FromSeconds(departure_time);
		}
	}

	internal sealed class TripsWithDates : List<TripWithDate>
	{
		public Route Route {
			get {
				Debug.Assert(this.Any(), "No trips in List<TripWithDate> - against invariant");
				return this.First().Route;
			}
			set {
				Debug.Assert(this.Any(), "No trips in List<TripWithDate> - against invariant");
				this.First().Route = value;
			}
		}

	}

	/// <summary>
	/// Represents trip and date when it's operational.
	/// 
	/// One trip can operate in multiple dates.
	/// Such trip instance is shared between Dates, therefore it reduces memory consumption.
	/// </summary>
	public sealed class TripWithDate
	{
		[JsonProperty]
		internal Trip InnnerTrip { get; private set; }

		/// <summary>
		/// Doesn't use time part.
		/// </summary>
		[JsonProperty]
		internal DateTimeOffset Date { get; private set; }

		[JsonObject(IsReference = true)]
		internal class Trip
		{
			[JsonProperty]
			internal Route route;

			[JsonProperty]
			internal readonly List<StopTime> stopTimes = new();

			internal Trip AddStopTime(StopTime s)
			{
				Debug.Assert(stopTimes.LastOrDefault().Departure_time <= s.Arrival_time);
				stopTimes.Add(s);
				return this;
			}

		}

		// required for serialization
		private TripWithDate() { }

		internal TripWithDate(Trip trip, DateTimeOffset date)
		{
			this.InnnerTrip = trip;
			this.Date = date;
			Debug.Assert(date.Hour == 0 && date.Minute == 0 && date.Second == 0, "Date should represent midnight"); //?? +-UTC offset
		}

		public Route Route {
			get { return InnnerTrip.route; }
			internal set { InnnerTrip.route = value; }
		}

		/// <summary>
		/// Time complexity O(1)
		/// </summary>
		public DateTimeOffset GetDepartureFromStop(int idx)
		{
			return Date + InnnerTrip.stopTimes[idx].Departure_time;
		}

		/// <summary>
		/// Time complexity O(1)
		/// </summary>
		public DateTimeOffset GetArrivalAtStop(int idx)
		{
			return Date + InnnerTrip.stopTimes[idx].Arrival_time;
		}

		/// <summary>
		/// Time complexity O(n)
		/// </summary>
		public DateTimeOffset GetDepartureFromStop(Stop s)
		{
			return Date + InnnerTrip.stopTimes[Route.GetStopIdx(s)].Departure_time;
		}

		/// <summary>
		/// Time complexity O(n)
		/// </summary>
		public DateTimeOffset GetArrivalAtStop(Stop s)
		{
			return Date + InnnerTrip.stopTimes[Route.GetStopIdx(s)].Arrival_time;
		}

	}

	/// <summary>
	/// Represents route as set of trips with common stops.
	/// </summary>
	[JsonObject(IsReference = true)]
	public class Route
	{
		/// <summary>
		/// Based on timetable data. Used in combination with stops to find route for trip.
		/// </summary>
		[JsonProperty]
		public string Id { get; private set; }

		/// <summary>
		/// Trips in route must be sorted by departure time to allow efficient searching.
		/// </summary>
		[JsonProperty]
		private readonly TripsWithDates routeTrips = new();
		
		/// <summary>
		/// Stops on route must be sorted from first visited to last visited.
		/// </summary>
		[JsonProperty]
		private readonly List<Stop> routeStops = new();

		public Route() { }

		public Route(string id)
		{
			Id = id;
		}

		internal void SortTrips()
		{
			routeTrips.Sort((t1, t2) => t1.GetDepartureFromStop(0).CompareTo(t2.GetDepartureFromStop(0)));
		}

		/// <summary>
		/// Time complexity O(1).
		/// </summary>
		public Stop GetStop(int idx)
		{
			return routeStops[idx];
		}

		/// <summary>
		/// Time complexity O(n).
		/// </summary>
		public int GetStopIdx(Stop s)
		{
			return routeStops.FindIndex(x => x == s);
		}

		public TripWithDate GetTrip(int idx)
		{
			return routeTrips[idx];
		}

		public ICollection<Stop> GetStops()
		{
			return routeStops;
		}

		public ICollection<TripWithDate> GetTrips()
		{
			return routeTrips;
		}

		internal Route AddRouteStop(Stop s)
		{
			routeStops.Add(s);
			return this;
		}

		internal Route AddTrips(TripsWithDates t)
		{
			if (t.Count != 0)
			{
				t.Route = this;
				routeTrips.AddRange(t);
			}
			return this;
		}
	}

	/// <summary>
	/// Represents transfers between 2 stops with corresponding transfer time.
	/// </summary>
	public class Transfer
	{
		[JsonProperty]
		public TimeSpan Time { get; private set; }

		//useful only in Journey
		[JsonProperty]
		public Stop Source { get; private set; }
		
		[JsonProperty]
		public Stop Target { get; private set; }

		public Transfer(TimeSpan time, Stop source, Stop target)
		{
			Time = time;
			Source = source;
			Target = target;
		}
	}

	/// <summary>
	/// Represents stop in timetable.
	/// Contains all routes that go through this stop - used for searching.
	/// Contains all transfers possible from this stop.
	/// Envelope from ISpatialData is used for efficient searching of neighbor - using RBush library.
	/// </summary>
	[JsonObject(IsReference = true)]
	public class Stop : ISpatialData
	{
		[JsonProperty]
		public string Id { get; private set; }

		[JsonProperty]
		public string Name { get; private set; }

		[JsonProperty]
		public double Latitude { get; private set; }

		[JsonProperty]
		public double Longitude { get; private set; }

		[JsonProperty]
		private Envelope envelope;

		[JsonIgnore]
		public ref readonly Envelope Envelope => ref envelope;

		[JsonProperty]
		List<Transfer> transfers = new();

		// Routes passing through this stop
		[JsonIgnore] // Circular reference => caused problem while serializing
		readonly List<Route> StopRoutes = new();


		public Stop(string name, string id)
		{
			Id = id;
			Name = name;
		}

		internal Stop(string name, string id, double latitude, double longitude) : this(name, id)
		{
			Latitude = latitude;
			Longitude = longitude;

			envelope = new Envelope(longitude, latitude, longitude, latitude);
		}

		//required for deserialization
		public Stop() { }

		public IEnumerable<Transfer> GetTransfers()
		{
			return transfers;
		}

		internal Stop SetTransfers(List<Transfer> transfers)
		{
			this.transfers = transfers;
			return this;
		}

		internal void AddRoute(Route r)
		{
			StopRoutes.Add(r);
		}

		public IEnumerable<Route> GetRoutes()
		{
			return StopRoutes;
		}
	}

	/// <summary>
	/// Datastructure holding timetable data. Used for efficient searching.
	/// </summary>
	public interface ITimetable
	{
		IEnumerable<Route> GetRoutes();

		/// <returns>Null if not found.</returns>
		Stop GetStopById(string stopId);

		IEnumerable<Stop> GetStops();

		/// <returns>Stops within given distance of given point</returns>
		IEnumerable<Stop> GetCoordNborsWithinDistance(double Latitude, double Longitude, double distanceMeters);

		/// <summary>
		/// Represents bounding box around stops. Useful for visualisation in restricted extent.
		/// </summary>
		(double minLat, double maxLat, double minLon, double maxLon) StopPositionLimits { get; }

		/// <summary>
		/// Represents first date sice when are data valid.
		/// </summary>
		DateTimeOffset StartDate { get; }

		/// <summary>
		/// Represents last date until which are data valid.
		/// </summary>
		DateTimeOffset EndDate { get; }
	}

	/// <summary>
	/// Uses singleton pattern.
	/// </summary>
	public sealed class Timetable : ITimetable
	{
		internal static Timetable instance;

		internal static readonly uint MAX_TRANSFER_DISTANCE = AppConfig.appSettings.MaxTransferDistanceInMeters;
		public static readonly float WALKING_SPEED = AppConfig.appSettings.WalkingSpeedInMetersPerSec;

		[JsonProperty]
		public DateTimeOffset StartDate { get; internal set; }

		[JsonProperty]
		public DateTimeOffset EndDate { get; internal set; }

		[JsonProperty]
		public (double minLat, double maxLat, double minLon, double maxLon) StopPositionLimits { get; private set; }

		[JsonProperty]
		readonly Dictionary<string, Route> routeByKey = new();
		
		[JsonProperty]
		Dictionary<string, Stop> stopById;

		/// <summary>
		/// R-tree based data structure for efficient range searching
		/// </summary>
		[JsonIgnore]
		RBush<Stop> stopsInRTree;

		//required for deserialization
		private Timetable() { }

		/// <summary>
		/// Constructs timetable data structure based on data provided by <see cref="IRawData"/>.
		/// Use this unless data are already serialized.
		/// </summary>
		private Timetable(IRawData data, bool serialize, DateTimeOffset startDate, int validityInDays)
		{
			void Serialize()
			{
				using (StreamWriter writer = new StreamWriter(AppConfig.appSettings.GTFSSerializationPath))
				using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
				{
					var ser = new JsonSerializer();
					ser.Serialize(jsonWriter, this);
					jsonWriter.Flush();
				}
			}

			/// Must be called before <see cref="AddRoutes(IRawData)"/>
			void InitializeValidity()
			{
				StartDate = new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, startDate.Offset);
				EndDate = validityInDays < 0 ?
					StartDate.AddDays(AppConfig.appSettings.ValidityInDays) : StartDate.AddDays(validityInDays);
			}

			/// Must be called after <see cref="RemoveUnusedStops"/>
			void InitializeStopPosLimits()
			{
				var stops = stopById.Values;
				StopPositionLimits = (stops.Min(s => s.Latitude), stops.Max(s => s.Latitude), stops.Min(s => s.Longitude), stops.Max(s => s.Longitude));

			}

			InitializeValidity();

			AddStops(data.Stops);
			AddRoutes(data);
			AddStopRoutes(routeByKey.Values);

			RemoveUnusedStops();
			CreateStopsRTree();
			GenerateTransfers();

			InitializeStopPosLimits();
			
			if (serialize) Serialize();
		}

		/// <summary>
		/// Deserializes data from previous serialization.
		/// </summary>
		/// <param name="maxDepth">For bigger timetables deserialization might need bigger recursion depth.</param>
		public static Timetable FromSerialization(int maxDepth = 128)
		{
			static Timetable Deserialize(int maxDepth)
			{
				using (var sr = new StreamReader(AppConfig.appSettings.GTFSSerializationPath))
				using (var jsonReader = new JsonTextReader(sr) { MaxDepth = maxDepth /* 4096 - user for Germany*/ })
				{
					var serializer = new JsonSerializer();
					Timetable instance = serializer.Deserialize<Timetable>(jsonReader);

					AddStopRoutes(instance.routeByKey.Values);
					instance.CreateStopsRTree();

					return instance;
				}
			}

			if (instance == null)
			{
				/*
				//used for big data provider eg. Germany - increases stack size
				var thread = new Thread(() => {
					instance = Deserialize();
				}, 1_048_576 * 100);
				thread.Start();
				thread.Join();
				*/

				instance = Deserialize(maxDepth);
			}
			return instance;
		}

		/// <summary>
		/// Constructs timetable from data.
		/// </summary>
		/// <param name="serialize">Determines wether constructed timetable should be serialized.</param>
		/// <param name="startDate">Startdate of timetable validity. By default set to Now.</param>
		/// <param name="ValidDays">If negative - value from configuration is used</param>
		public static Timetable FromData(IRawData data, bool serialize = true,
			DateTimeOffset? startDate = null, int validityInDays = -1)
		{
			if (instance == null)
			{
				if (startDate == null) startDate = DateTimeOffset.Now;

				instance = new Timetable(data, serialize, startDate.Value, validityInDays);
			}
			return instance;
		}

		/// <summary>
		/// Must be called After <see cref="RemoveUnusedStops"/> and before <see cref="GenerateTransfers"/>
		/// </summary>
		private void CreateStopsRTree()
		{
			stopsInRTree = new RBush<Stop>(stopById.Count);
			stopsInRTree.BulkLoad(stopById.Values);
		}

		private void AddStops(List<GTFSStop> gtfsStops)
		{
			stopById = new();
			foreach (var stop in gtfsStops)
			{
				stopById.Add(stop.stop_id, new Stop(stop.stop_name, stop.stop_id, stop.stop_lat, stop.stop_lon));
			}
		}

		/// <summary>
		/// Wraps together all necessary information to determine if stop is available at given day.
		/// </summary>
		private class Calendar
		{
			public const int ADDED_SERVICE = 1;
			public const int REMOVED_SERVICE = 2;

			public List<DayOfWeek> availableDays = new();
			public DateTimeOffset StartDate { get; private set; }
			public DateTimeOffset EndDate { get; private set; }

			public List<(byte type, DateTimeOffset date)> Exceptions { get; private set; } = new();

			public Calendar(GTFSCalendar gtfsCalendar, List<GTFSCalendarDate> exceptions, Timetable timetable)
			{
				StartDate = timetable.StartDate > gtfsCalendar.start_date ? timetable.StartDate : gtfsCalendar.start_date;
				EndDate = timetable.EndDate < gtfsCalendar.end_date ? timetable.EndDate : gtfsCalendar.end_date;

				if (gtfsCalendar.monday) availableDays.Add(DayOfWeek.Monday);
				if (gtfsCalendar.tuesday) availableDays.Add(DayOfWeek.Tuesday);
				if (gtfsCalendar.wednesday) availableDays.Add(DayOfWeek.Wednesday);
				if (gtfsCalendar.thursday) availableDays.Add(DayOfWeek.Thursday);
				if (gtfsCalendar.friday) availableDays.Add(DayOfWeek.Friday);
				if (gtfsCalendar.saturday) availableDays.Add(DayOfWeek.Saturday);
				if (gtfsCalendar.sunday) availableDays.Add(DayOfWeek.Sunday);

				if (exceptions != null)
				{
					foreach (var exception in exceptions)
					{
						this.Exceptions.Add((exception.exception_type, exception.date));
					}
				}
			}
		}

		/// <summary>
		/// Contains stop id, stop arrival times and route to which stop belongs.
		/// </summary>
		private struct StopInfo
		{
			public StopInfo(TimeSpan arrivalTime, TimeSpan departureTime, string stopId, string routeId)
			{
				this.arrivalTime = arrivalTime;
				this.departureTime = departureTime;
				this.stopId = stopId;
				this.routeId = routeId;
			}

			public readonly TimeSpan arrivalTime;
			public readonly TimeSpan departureTime;
			public readonly string stopId;
			public readonly string routeId;
		}

		/// <summary>
		/// Joins <see cref="IRawData.CalendarDates"/> 
		/// and <see cref="IRawData.Trips"/> 
		/// and <see cref="IRawData.Calendar"/>
		/// on <see cref="GTFSCalendarDate.service_id"/>.
		/// </summary>
		/// <returns>Calendar for each trip_id</returns>
		private Dictionary<string, Calendar> GetCalendarByTripId(IRawData data)
		{
			static void UpdateOrInsert(Dictionary<string, List<GTFSCalendarDate>> dict, string newKey, GTFSCalendarDate newVal)
			{
				if (dict.TryGetValue(newKey, out var dates))
				{
					dates.Add(newVal);
				}
				else
				{
					dict.Add(newKey, new() { newVal });
				}
			}

			static Dictionary<string, List<GTFSCalendarDate>> GetCalendarDatesByServiceId(List<GTFSCalendarDate> calendarDates)
			{
				Dictionary<string, List<GTFSCalendarDate>> calendarDatesByServiceId = new();
				calendarDates.ForEach(calendarDate => UpdateOrInsert(calendarDatesByServiceId, calendarDate.service_id, calendarDate));
				return calendarDatesByServiceId;
			}

			IEnumerable<IGrouping<string, (GTFSCalendar calendar, string tripId)>> GetTripIdWithCalendarByServiceId(IRawData data)
			{
				var result = from c in data.Calendar
							 join t in data.Trips on c.service_id equals t.service_id
							 group (c, t.trip_id) by c.service_id into g
							 select g;
				return result;
			}

			Dictionary<string, Calendar> result = new();
			Dictionary<string, List<GTFSCalendarDate>> calendarDatesByServiceId = GetCalendarDatesByServiceId(data.CalendarDates);
			foreach (var TripIdWithCalendar in GetTripIdWithCalendarByServiceId(data)) //not sure if it gets optimized away
			{
				var serviceId = TripIdWithCalendar.Key;
				calendarDatesByServiceId.TryGetValue(serviceId, out var exceptions);

				foreach (var (calendar, tripId) in TripIdWithCalendar)
				{
					result.Add(tripId, new Calendar(calendar, exceptions, this));
				}
			}

			return result;
		}

		/// <summary>
		/// Joins <see cref="IRawData.StopTimes"/> and <see cref="IRawData.Trips"/>.
		/// Ensures ordering of stop arrival time from earliest to latest.
		/// </summary>
		/// <returns>
		/// Enumeration of trips with stopsInfo under them.
		/// IGrouping equals values under 1 key in dictionary.
		/// </returns>
		private static IEnumerable<IGrouping<string, StopInfo>> GetAllTripIdsWithStopsInfo(IRawData data)
		{
			//data.StopTimes
			var result = from s in data.StopTimes
						 join t in data.Trips on s.trip_id equals t.trip_id
						 orderby s.arrival_time //ensures ordering - although PID data are sorted
						 group new StopInfo(s.arrival_time, s.departure_time, s.stop_id, t.route_id) by t.trip_id into g
						 select g;

			return result;
		}

		/// <summary>
		/// Creates TripWithDate for each date when is trip available.
		/// </summary>
		static TripsWithDates CreateTripWithDates(TripWithDate.Trip trip, Calendar tripCalendar)
		{
			static bool ValidDay(DateTimeOffset date, Calendar calendar)
			{
				return calendar.availableDays.Contains(date.DayOfWeek);
			}

			IEnumerable<DateTimeOffset> CreateDateInterval(DateTimeOffset startDate, DateTimeOffset endDate)
			{
				List<DateTimeOffset> result = new();
				for (var date = tripCalendar.StartDate; date <= tripCalendar.EndDate; date = date.AddDays(1))
				{
					if (ValidDay(date, tripCalendar))
					{
						result.Add(date);
					}
				}
				return result;
			}
			IEnumerable<DateTimeOffset> RemoveExceptionalDates(IEnumerable<DateTimeOffset> dates, Calendar calendar)
			{
				var removedDates = tripCalendar.Exceptions.Where(e => e.type == Calendar.REMOVED_SERVICE).Select(e => e.date);
				return dates.Except(removedDates);
			}
			IEnumerable<DateTimeOffset> AddExceptionalDates(IEnumerable<DateTimeOffset> dates, Calendar calendar)
			{
				var addedDates = tripCalendar.Exceptions.Where(e => e.type == Calendar.ADDED_SERVICE).Select(e => e.date);
				return dates.Concat(addedDates);
			}

			var dates = CreateDateInterval(tripCalendar.StartDate, tripCalendar.EndDate);
			dates = RemoveExceptionalDates(dates, tripCalendar);
			dates = AddExceptionalDates(dates, tripCalendar);

			TripsWithDates result = new();
			foreach (var date in dates) result.Add(new TripWithDate(trip, date));
			return result;
		}

		/// <summary>
		/// Creates Trips for available dates, adds StopTimes to trips. 
		/// Also creates route under which created trip belongs.
		/// </summary>
		private TripsWithDates CreateTrips(IGrouping<string, StopInfo> tripIdWithStopsInfo, Dictionary<string, Calendar> calendarByTripId)
		{
			/// <summary>
			/// Creates Trip with StopTimes.
			/// Assumes that StopTimes are ordered from earliest to latest.
			/// </summary>
			/// <returns>Created trip and route under which trip belongs.</returns>
			(Route, TripWithDate.Trip) GetTripWithRoute(IGrouping<string, StopInfo> tripIdWithStopsInfo)
			{
				TripWithDate.Trip newTrip = new();
				Route newRoute = new(tripIdWithStopsInfo.First().routeId); //all routeIds are same
				foreach (var stopInfo in tripIdWithStopsInfo)
				{
					newTrip.AddStopTime(new StopTime(stopInfo.arrivalTime, stopInfo.departureTime));
					newRoute.AddRouteStop(stopById[stopInfo.stopId]);
				}
				return (newRoute, newTrip);
			}

			var (newRoute, newTrip) = GetTripWithRoute(tripIdWithStopsInfo);
			if (calendarByTripId.TryGetValue(tripIdWithStopsInfo.Key, out var tripCalendar))
			{
				var result = CreateTripWithDates(newTrip, tripCalendar);
				newRoute.AddTrips(result);
				return result;
			}
			else
			{
				return new(); // for datasets (Germany) with trips without associated calendar
			}
		}

		/// <summary>
		/// Creates routes based on unique identifier(route id + stops).
		/// Adds existing stops to routes.
		/// Creates trips with StopTimes and groups them under route.
		/// Sorts trips under route.
		/// </summary>
		private void AddRoutes(IRawData data)
		{
			/// <summary>
			/// Generates unique route identifier.
			/// Based on route id and stops on route.
			/// </summary>
			static string GetRouteKey(Route route)
			{
				StringBuilder keyBuilder = new(route.Id);
				foreach (var stop in route.GetStops()) keyBuilder.Append(stop.Id);
				return keyBuilder.ToString();
			}

			/// <summary>
			/// Adds trip to route if exists otherwise adds new route with trip.
			/// </summary>
			static void UpdateOrInsert(Dictionary<string, Route> dict, string routeKey, TripsWithDates newTrips)
			{
				if (dict.TryGetValue(routeKey, out var route))
				{
					route.AddTrips(newTrips);
				}
				else
				{
					dict.Add(routeKey, newTrips.Route);
				}
			}

			var calendarByTripId = GetCalendarByTripId(data);
			foreach (var tripIdWithStopsInfo in GetAllTripIdsWithStopsInfo(data))
			{
				TripsWithDates newTrips = CreateTrips(tripIdWithStopsInfo, calendarByTripId);
				if (newTrips.Count != 0) UpdateOrInsert(routeByKey, GetRouteKey(newTrips.Route), newTrips);
			}
			foreach (var route in routeByKey.Values) route.SortTrips();
		}

		public Stop GetStopById(string stopId)
		{
			stopById.TryGetValue(stopId, out Stop result);
			return result;
		}

		public IEnumerable<Stop> GetStops()
		{
			return stopById.Values;
		}

		public IEnumerable<Route> GetRoutes()
		{
			return routeByKey.Values;
		}

		public IEnumerable<Stop> GetCoordNborsWithinDistance(double Latitude, double Longitude, double distanceMeters)
		{
			var boundaryBox = WGS84.GetBoundaryBox(Latitude, Longitude, distanceMeters+100);
			var neighborStops = stopsInRTree.Search(new Envelope(
				boundaryBox.minLon,
				boundaryBox.minLat,
				boundaryBox.maxLon,
				boundaryBox.maxLat
			));

			return neighborStops.Where(s =>
				WGS84.GetDistance(s.Longitude, s.Latitude, Longitude, Latitude) < distanceMeters);
		}

		/// <summary>
		/// Generates transfers between stops within <see cref="MAX_TRANSFER_DISTANCE"/>.
		/// <see cref="WALKING_SPEED"/> is used to determine time spent trasfering.
		/// Transfers are transitive.
		/// Simplified: measures distance in straight line.
		/// </summary>
		private void GenerateTransfers()
		{
			/// <summary>
			/// Finds transfers = edges in graph.
			/// </summary>
			Dictionary<Stop, List<Stop>> FindTransfers(ICollection<Stop> stops)
			{
				Dictionary<Stop, List<Stop>> result = new();
				foreach (var stop in stops)
				{
					foreach (var nborStop in GetCoordNborsWithinDistance(stop.Latitude, stop.Longitude, MAX_TRANSFER_DISTANCE))
					{
						if (stop == nborStop) continue;
						
						if (result.TryGetValue(stop, out var nbors))
						{
							nbors.Add(nborStop);
						}
						else
						{
							result.Add(stop, new() { nborStop });
						}
					}
				}
				return result;
			}

			/// <summary>
			/// Finds all strongly connected Stops
			/// </summary>
			List<Stop> FindComponent_DFS(Stop stop, HashSet<Stop> visited, Dictionary<Stop, List<Stop>> transfersByStop)
			{
				var result = new List<Stop>();
				Stack<Stop> stack = new();
				stack.Push(stop);

				while (stack.Count != 0)
				{
					Stop curr = stack.Pop();
					if (!visited.Contains(curr))
					{
						visited.Add(curr);
						result.Add(curr);

						if (transfersByStop.ContainsKey(curr))
						{
							foreach (var nborStop in transfersByStop[curr])
							{
								stack.Push(nborStop);
							}
						}
					}

				}
				return result;
			}

			/// <summary>
			/// Adds transfers to Stops.
			/// </summary>
			void CreateTransfersFromComponents(List<List<Stop>> components)
			{
				foreach (var comp in components)
				{
					foreach (var stop in comp)
					{
						List<Transfer> transfers = new();
						foreach (var otherStop in comp)
						{
							if (stop == otherStop) continue;
							double distance = WGS84.GetDistance(stop.Longitude, stop.Latitude, otherStop.Longitude, otherStop.Latitude);
							TimeSpan transferTime = TimeSpan.FromSeconds(distance / WALKING_SPEED);
							transfers.Add(new Transfer(transferTime, stop, otherStop));
						}
						stop.SetTransfers(transfers);
					}
				}
			}

			var stops = stopById.Values;
			Dictionary<Stop, List<Stop>> transfersByStop = FindTransfers(stops); // edges - could be computed lazily => would consume less memory

			HashSet<Stop> visited = new();
			List<List<Stop>> components = new();
			foreach (var stop in stops)
			{
				if (visited.Contains(stop)) continue;
				components.Add(FindComponent_DFS(stop, visited, transfersByStop));
			}

			CreateTransfersFromComponents(components);
		}
		
		private static void AddStopRoutes(IEnumerable<Route> routes)
		{
			foreach (var route in routes)
			{
				foreach (var stop in route.GetStops())
				{
					stop.AddRoute(route);
				}
			}
		}

		/// <summary>
		/// Removes stops without <see cref="Stop.StopRoutes"/>.
		/// </summary>
		private void RemoveUnusedStops()
		{
			stopById = stopById.Where(keyval => keyval.Value.GetRoutes().Any())
				.ToDictionary(keyval => keyval.Key, keyval => keyval.Value);
		}
	}
}
