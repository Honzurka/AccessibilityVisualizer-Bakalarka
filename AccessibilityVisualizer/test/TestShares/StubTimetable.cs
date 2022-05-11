using System;
using System.Collections.Generic;
using System.Linq;
using GTFSData;

namespace TestShares
{
	public class StubTimetable : ITimetable
	{
		public (double minLat, double maxLat, double minLon, double maxLon) StopPositionLimits => throw new NotImplementedException();
		public DateTimeOffset StartDate => throw new NotImplementedException();
		public DateTimeOffset EndDate => throw new NotImplementedException();


		public Dictionary<string, Route> routes = new();
		public Dictionary<string, Stop> stops = new();


		public StubTimetable(Dictionary<string, Route> routes, Dictionary<string, Stop> stops)
		{
			this.routes = routes;
			this.stops = stops;
		}

		public StubTimetable() { }

		public IEnumerable<Route> GetRoutes() => routes.Values;

		public void SortRouteTrips()
		{
			foreach (var route in routes)
			{
				route.Value.SortTrips();
			}
		}

		public Stop GetStopById(string stopId)
		{
			stops.TryGetValue(stopId, out Stop s);
			if (s == null) throw new Exception();
			return s;
		}

		public IEnumerable<Stop> GetStops() => stops.Values;

		private static void AddStopRoutes(Route route)
		{
			foreach (var stop in route.GetStops())
			{
				stop.AddRoute(route);
			}
		}

		public TripWithDate AddTripByParts(List<(Stop, StopTime)> tripParts, DateTimeOffset? dto = null)
		{
			dto = dto == null ? DateTime.UnixEpoch : dto;

			var t = new TripWithDate.Trip();
			var r = new Route();
			string routeKey = "";
			foreach (var (stop, stopTime) in tripParts)
			{
				t.AddStopTime(stopTime);
				if (!stops.ContainsKey(stop.Id))
				{
					stops.Add(stop.Id, stop);
				}
				r.AddRouteStop(stop);
				routeKey += stop.Id;
			}

			var tWithDate = new TripWithDate(t, dto.Value);
			var tWithDates = new TripsWithDates() { tWithDate };
			if (routes.TryGetValue(routeKey, out Route foundRoute))
			{
				foundRoute.AddTrips(tWithDates);
			}
			else
			{
				r.AddTrips(tWithDates);
				routes.Add(routeKey, r);
				AddStopRoutes(r);
			}

			return tWithDate;
		}

		public List<TripWithDate> AddTripWithDates(List<(Stop, StopTime)> tripParts, List<DateTimeOffset> dtos)
		{
			List<TripWithDate> result = new();
			foreach (var dto in dtos)
			{
				var t = AddTripByParts(tripParts, dto);
				result.Add(t);
			}
			return result;
		}


		public void AddTransfer(Transfer transfer)
		{
			var srcId = transfer.Source.Id;
			var t = stops[srcId].GetTransfers().ToList();
			t.Add(transfer);
			stops[srcId].SetTransfers(t);
		}

		public void AddStop(Stop s)
		{
			stops.Add(s.Id, s);
		}

		public IEnumerable<Stop> GetCoordNborsWithinDistance(double Latitude, double Longitude, double distanceMeters)
		{
			return stops.Values.Where(
				s => WGS84.GetDistance(s.Longitude, s.Latitude, Longitude, Latitude) <= distanceMeters
			);
		}

	}

}
