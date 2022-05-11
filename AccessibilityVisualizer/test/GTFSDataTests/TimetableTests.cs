using System;
using Xunit;
using GTFSData;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace GTFSDataTests
{
	class StubRawData : IRawData
	{
		public List<GTFSStop> Stops { get; set; } = new();
		public List<GTFSTrip> Trips { get; private set; } = new();
		public List<GTFSStopTime> StopTimes { get; private set; } = new();
		public List<GTFSCalendarDate> CalendarDates { get; private set; } = new();
		public List<GTFSCalendar> Calendar { get; private set; } = new();

		//default vals => 1 week
		public DateTimeOffset startDate = DateConverter.ConvertCSVDate("19700101");
		public DateTimeOffset endDate = DateConverter.ConvertCSVDate("19700107");

		//adds unused stop
		public StubRawData AddStop(GTFSStop stop)
		{
			if (!Stops.Any(s => s.stop_id == stop.stop_id))
				Stops.Add(stop);
			return this;
		}

		public StubRawData AddTrip(GTFSTrip trip)
		{
			Trips.Add(trip);
			return this;
		}

		public StubRawData AddStopTime(GTFSStopTime stopTime)
		{
			StopTimes.Add(stopTime);
			return this;
		}

		private StubRawData AddCalendar(GTFSCalendar calendar)
		{
			Calendar.Add(calendar);
			return this;
		}

		public StubRawData AddCalendarDate(GTFSCalendarDate cd)
		{
			CalendarDates.Add(cd);
			return this;
		}

		public StubRawData AddTripWithStops(string route_id, string trip_id, List<string> stop_ids, string service_id, List<DayOfWeek> availableDays = null, List<StopTime> stopTimes = null)
		{
			//default stopTimes
			if(stopTimes == null)
			{
				stopTimes = new();
				stopTimes.AddRange(Enumerable.Repeat(new StopTime(), stop_ids.Count));
			}



			AddTrip(new GTFSTrip { trip_id = trip_id, service_id = service_id, route_id = route_id });
			for (int i = 0; i < stop_ids.Count; i++)
			{
				AddStop(new GTFSStop { stop_id = stop_ids[i] });
				AddStopTime(new GTFSStopTime { stop_id = stop_ids[i], trip_id = trip_id, arrival_time = stopTimes[i].Arrival_time, departure_time = stopTimes[i].Departure_time });
			}
			if (!Calendar.Any(c => c.service_id == service_id))
			{
				AddCalendar(new GTFSCalendar
				{
					service_id = service_id,
					start_date = startDate,
					end_date = endDate,
					monday = availableDays.Contains(DayOfWeek.Monday),
					tuesday = availableDays.Contains(DayOfWeek.Tuesday),
					wednesday = availableDays.Contains(DayOfWeek.Wednesday),
					thursday = availableDays.Contains(DayOfWeek.Thursday),
					friday = availableDays.Contains(DayOfWeek.Friday),
					saturday = availableDays.Contains(DayOfWeek.Saturday),
					sunday = availableDays.Contains(DayOfWeek.Sunday)
				});
			}

			return this;
		}

		public IEnumerable<Stop> GetStopsConverted()
		{
			return Stops.ConvertAll(x => new Stop(x.stop_name, x.stop_id));
		}
	}

	class TimetableNoSingleton
	{
		public static Timetable From(StubRawData data)
		{
			Timetable.instance = null;
			return Timetable.FromData(data, false, data.startDate, 365);
		}
	}

	class StopComparer : IEqualityComparer<Stop>
	{
		public bool Equals(Stop x, Stop y)
		{
			return x.Id == y.Id &&
				x.Name == y.Name &&
				x.Latitude == y.Latitude &&
				x.Longitude == y.Longitude;
		}

		public int GetHashCode([DisallowNull] Stop s)
		{
			return HashCode.Combine(s.Id, s.Name, s.Latitude, s.Longitude);
		}
	}

	class TransferComparer : IEqualityComparer<Transfer>
	{
		public bool Equals(Transfer x, Transfer y)
		{
			if (x.Source == y.Source && x.Target == y.Target && x.Time == y.Time) return true;
			if (x.Source == y.Target && x.Target == y.Source && x.Time == y.Time) return true;
			return false;
		}

		public int GetHashCode([DisallowNull] Transfer t)
		{
			return t.Time.GetHashCode();
		}
	}

	static class Extensions
	{
		public static StopTime ToStopTime(this (int arr, int dep) times)
		{
			return new StopTime(TimeSpan.FromSeconds(times.arr), TimeSpan.FromSeconds(times.dep));
		}
	}

	public class TimetableTests
	{
		[Fact]
		public void FromData_HasStops()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20" }, "100", new() { DayOfWeek.Monday });
			IEnumerable<Stop> expectedStops = data.GetStopsConverted();

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);

			//assert
			Assert.Equal(expectedStops, timetable.GetStops(), new StopComparer());
		}

		[Fact]
		public void FromData_RemoveUnusedStop()
		{
			//arrange
			StubRawData data = new();

			data.AddTripWithStops("1", "1", new() { "10", "20" }, "100", new() { DayOfWeek.Monday });
			var removedStopId = "30";
			data.AddStop(new GTFSStop { stop_id = removedStopId });

			var expectedStops = data.GetStopsConverted();
			expectedStops = expectedStops.Where(x => x.Id != removedStopId);

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);

			//assert
			Assert.Equal(expectedStops, timetable.GetStops(), new StopComparer());
		}

		[Fact]
		public void FromData_GeneratedTransfers_CorrectDistance()
		{
			//arrange
			StubRawData data = new();

			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Monday });


			double diff = 0.0001; //~10meters
			double latBig = 0;
			while (WGS84.GetDistance(0, 0, 0, latBig) < Timetable.MAX_TRANSFER_DISTANCE)
			{

				latBig += diff;
			}
			double latSmall = latBig - diff;

			//strange way to change lattitude of struct in collection
			data.Stops.RemoveAt(2);
			data.Stops.RemoveAt(1);
			var stop20 = new GTFSStop { stop_id = "20", stop_lat = latSmall };
			var stop30 = new GTFSStop { stop_id = "30", stop_lat = -latBig };
			data.Stops.Add(stop20);
			data.Stops.Add(stop30);

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var stops = timetable.GetStops().ToList();

			//assert
			Assert.Empty(stops[2].GetTransfers());
			Assert.Equal(stops[0].GetTransfers(), stops[1].GetTransfers(), new TransferComparer());
		}

		[Fact]
		public void FromData_StopRoute()
		{
			//arrange
			StubRawData data = new();

			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Monday });
			data.AddTripWithStops("2", "2", new() { "10", "50", "60" }, "200", new() { DayOfWeek.Tuesday });
			data.AddTripWithStops("3", "3", new() { "70", "80", "90" }, "300", new() { DayOfWeek.Wednesday });

			var assertedStopId = "10";
			var expectedRouteIds = new List<string> { "1", "2" };

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var routeIds = timetable.GetStopById(assertedStopId).GetRoutes().Select(r => r.Id);

			//assert
			Assert.Equal(expectedRouteIds, routeIds);
		}



		[Fact]
		public void FromData_Route_AddRoute()
		{
			//arrange
			StubRawData data = new();

			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Monday });
			var expectedRouteIds = new List<string> { "1" };

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var routeIds = timetable.GetRoutes().Select(r => r.Id);

			//assert
			Assert.Equal(expectedRouteIds, routeIds);
		}

		[Fact]
		public void FromData_Route_AddTripsAndMergeUnderRoute()
		{
			//arrange
			StubRawData data = new();

			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Monday });
			data.AddTripWithStops("1", "2", new() { "10", "20", "30" }, "200", new() { DayOfWeek.Tuesday });

			var expectedTripDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday }; //trips compared by date

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var tripDays = timetable.GetRoutes().First().GetTrips().Select(t => t.Date.DayOfWeek);

			//assert
			Assert.Equal(expectedTripDays, tripDays);
		}

		[Fact]
		public void FromData_Route_AddTripsWithoutMerge()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Monday });
			data.AddTripWithStops("2", "2", new() { "10", "20", "30" }, "200", new() { DayOfWeek.Tuesday });
			data.AddTripWithStops("2", "3", new() { "10", "20", "30", "40" }, "300", new() { DayOfWeek.Tuesday });
			data.AddTripWithStops("2", "4", new() { "10", "20" }, "400", new() { DayOfWeek.Tuesday });
			data.AddTripWithStops("2", "5", new() { "10", "20", "40" }, "500", new() { DayOfWeek.Tuesday });


			var expectedNumberOfRoutes = data.Trips.Count;

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);

			//assert
			Assert.Equal(expectedNumberOfRoutes, timetable.GetRoutes().Count());
		}

		[Fact]
		public void FromData_Route_HasSortedTripsByTime()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100",
				new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
						  new() { (10, 11).ToStopTime(), (20, 21).ToStopTime(), (30, 31).ToStopTime() });
			data.AddTripWithStops("1", "3", new() { "10", "20", "30" }, "100",
				stopTimes: new() { (15, 16).ToStopTime(), (25, 26).ToStopTime(), (35, 36).ToStopTime() });
			data.AddTripWithStops("1", "2", new() { "10", "20", "30" }, "100",
				stopTimes: new() { (5, 6).ToStopTime(), (15, 16).ToStopTime(), (25, 26).ToStopTime() });


			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var departures = timetable.GetRoutes().First().GetTrips().Select(t => t.GetDepartureFromStop(0));
			var expectedDepartures = departures.OrderBy(t => t.Ticks);

			//assert
			Assert.Equal(expectedDepartures, departures);
		}

		[Fact]
		public void FromData_Route_HasSortedTripsByDate()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100",
				new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
				new() { (10, 11).ToStopTime(), (20, 21).ToStopTime(), (30, 31).ToStopTime() });
			data.AddTripWithStops("1", "2", new() { "10", "20", "30" }, "200",
				new() { DayOfWeek.Friday },
				new() { (10, 11).ToStopTime(), (20, 21).ToStopTime(), (30, 31).ToStopTime() });
			data.AddTripWithStops("1", "3", new() { "10", "20", "30" }, "300",
				new() { DayOfWeek.Tuesday },
				new() { (10, 11).ToStopTime(), (20, 21).ToStopTime(), (30, 31).ToStopTime() });

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var departures = timetable.GetRoutes().First().GetTrips().Select(t => t.GetDepartureFromStop(0));
			var expectedDepartures = departures.OrderBy(t => t.Ticks);

			//assert
			Assert.Equal(expectedDepartures, departures);
		}
	}

	public class TripWithDateTests
	{
		[Fact]
		public void Date_FromStartDateToEndDate()
		{
			//arrange
			StubRawData data = new();
			data.endDate = data.startDate + TimeSpan.FromDays(3 * 7);
			data.AddTripWithStops("1", "1", new() { "10", "20" }, "100", new() { DayOfWeek.Monday, DayOfWeek.Saturday });

			var expectedDates = new List<string>{
				"19700103", "19700105", "19700110", "19700112", "19700117", "19700119" }
				.Select(d => DateConverter.ConvertCSVDate(d));
			
			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var trips = timetable.GetRoutes().First().GetTrips();
			var dates = trips.Select(t => t.Date);

			//assert
			Assert.Equal(expectedDates, dates);
		}

		[Fact]
		public void Date_ExtraAdded()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20" }, "100", new() { DayOfWeek.Monday })
				.AddCalendarDate(new GTFSCalendarDate { date = data.startDate, exception_type = 1, service_id = "100" });
			var expectedDates = new List<string>{ "19700101", "19700105" }
				.Select(d => DateConverter.ConvertCSVDate(d));

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var dates = timetable.GetRoutes().First().GetTrips().Select(t => t.Date);


			//assert
			Assert.Equal(expectedDates, dates);
		}

		[Fact]
		public void Date_ExtraRemoved()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20" }, "100", new() { DayOfWeek.Thursday, DayOfWeek.Friday })
				.AddCalendarDate(new GTFSCalendarDate { date = data.startDate, exception_type = 2, service_id = "100" });
			var expectedDates = new List<string> { "19700102" }
				.Select(d => DateConverter.ConvertCSVDate(d));


			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var dates = timetable.GetRoutes().First().GetTrips().Select(t => t.Date);

			//assert
			Assert.Equal(expectedDates, dates);
		}

		[Fact]
		public void Route_AllSame()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Tuesday });
			data.AddTripWithStops("1", "2", new() { "10", "20", "30" }, "200", new() { DayOfWeek.Wednesday });
			data.AddTripWithStops("1", "3", new() { "10", "20", "30" }, "300", new() { DayOfWeek.Saturday });

			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var tripRoutes = timetable.GetRoutes().First().GetTrips().Select(t => t.Route);
			
			//assert
			Assert.DoesNotContain(tripRoutes, r => r != tripRoutes.First()); //all same
		}

		[Fact]
		public void InnerTrip_ShouldBeShared()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10", "20", "30" }, "100", new() { DayOfWeek.Tuesday, DayOfWeek.Friday });
			
			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var innerTrips = timetable.GetRoutes().First().GetTrips().Select(t => t.InnnerTrip);

			//assert
			Assert.Equal(2, innerTrips.Count());
			Assert.DoesNotContain(innerTrips, t => t != innerTrips.First());
		}

		[Fact]
		public void InnerTrip_StopTimes_OverMidnight()
		{
			//arrange
			StubRawData data = new();
			data.AddTripWithStops("1", "1", new() { "10" }, "100", new() { DayOfWeek.Monday },
				stopTimes: new() { new StopTime(TimeConverter.TimeFrom("23:00:00"), TimeConverter.TimeFrom("23:01:00")) })
				.AddTripWithStops("1", "2", new() { "10" }, "100",
				stopTimes: new() { new StopTime(TimeConverter.TimeFrom("25:00:00"), TimeConverter.TimeFrom("25:01:00")) });


			var expectedArrivalTimes = new List<DateTimeOffset>
			{
				DateConverter.ConvertCSVDate("19700105") + TimeSpan.FromHours(23),
				DateConverter.ConvertCSVDate("19700106") + TimeSpan.FromHours(1)
			};
			
			//act
			ITimetable timetable = TimetableNoSingleton.From(data);
			var arrivalTimes = timetable.GetRoutes().First().GetTrips().Select(t => t.GetArrivalAtStop(0));

			//assert
			Assert.Equal(expectedArrivalTimes, arrivalTimes);
		}
	}
}
