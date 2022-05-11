//#define Measure //useful for measuring time spent searching

using System;
using System.Collections.Generic;
using System.Linq;
using GTFSData;

#if Measure
	using System.Diagnostics;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RaptorTests")]

/// <summary>
/// Time-relatve terms:
///		walk time: Time required to reach the stop.
///		travel time: Time spent on route.
///		departure time: If associated with stop then time when transport departs. Otherwise time when we are at the stop ready to depart.
///		arrival time: Time when we arrive at associated stop.
///		start time: Time from which we start searching.
///		accessibility: earliest arrival time - start time
/// </summary>
namespace RaptorAlgo
{
	public interface ITransitRouter
	{
		/// <summary>
		/// Calculates travel time to all accessible stops.
		/// </summary>
		/// <param name="srcStopsAndWalkTimes">
		/// Uses multiple starting stops in calculation.
		/// Each starting stop is associated with time required to reach it.
		/// </param>
		/// <param name="noRoundLimit">Wether number of transfers should be limited or not.</param>
		/// <param name="rememberStopData">
		/// Used to compute travel times in interval (consecutive calls with startTime ordered from latest to earliest).
		/// </param>
		/// <returns></returns>
		public Dictionary<Stop, TimeSpan> GetAccessibilityByStops( IEnumerable<(Stop stop, TimeSpan walkTime)> srcStopsAndWalkTimes,
			DateTimeOffset startTime, bool noRoundLimit = false, bool rememberStopData = false);
	}

	sealed public class Raptor : ITransitRouter
	{
		/// <summary>
		/// Used for calculations in interval - rRAPTOR.
		/// </summary>
		DateTimeOffset lastStartTime = DateTimeOffset.MinValue;

		/// <summary>
		/// Limits max number of transfer by its value-1.
		/// </summary>
		public int TotalRounds { get; set; } = 5;

		internal StopsData stopsData;
		int currentRound;

		readonly HashSet<Stop> markedStops = new();

		/// <summary>
		/// Route with earliest marked stop idx
		/// </summary>
		readonly Dictionary<Route, int> markedRoutes = new();

		/// <summary>
		/// Marks routes that go through marked stops.
		/// </summary>
		private void MarkRoutes()
		{
			markedRoutes.Clear();

			foreach (var stop in markedStops)
			{
				foreach (var route in stop.GetRoutes())
				{
					int stopIdx = route.GetStopIdx(stop);
					if (markedRoutes.TryGetValue(route, out int earliestStopIdx))
					{
						if (stopIdx < earliestStopIdx)
						{
							markedRoutes[route] = stopIdx;
						}
					}
					else
					{
						markedRoutes.Add(route, stopIdx);
					}
				}
			}
			markedStops.Clear();
		}

		/// <summary>
		/// Uses binary search optimization.
		///		invariants:
		///			lower than low => less than arrivalTime
		///			higher than high => greater or equal to arrivalTime
		/// </summary>
		/// <returns>Earliest trip that we could get on after arriving at stop.</returns>
		private TripWithDate GetEarliestTripFromStop(Route route, int stopIdx)
		{
			var arrivalTime = stopsData.GetExistingStopData(route.GetStop(stopIdx)).GetArrivalTime(currentRound - 1);
			var routeTrips = route.GetTrips();

			if (routeTrips.Last().GetDepartureFromStop(stopIdx) < arrivalTime) return null;

			int low = 0;
			int high = routeTrips.Count - 1;
			while (low <= high)
			{
				int mid = (high + low) / 2;
				DateTimeOffset midDeparture = route.GetTrip(mid).GetDepartureFromStop(stopIdx);
				if (midDeparture < arrivalTime)
				{
					low = mid + 1;
				}
				else
				{
					high = mid - 1;
				}
			}
			return route.GetTrip(low);
		}

		private void UpdateArrivalTimeByTrip(TripWithDate currentTrip, int stopIdx, int transferStopIdx)
		{
			var route = currentTrip.Route;
			var newArrivalTime = currentTrip.GetArrivalAtStop(stopIdx);
			var stop = route.GetStop(stopIdx);
			var stopData = stopsData.GetStopDataOrCreateNew(stop);
			if (newArrivalTime < stopData.GetArrivalTime(currentRound))
			{
				stopData.SetArrivalTime(newArrivalTime, currentRound);
				markedStops.Add(stop);
				stopData.ArriveByTrip(currentTrip, route.GetStop(transferStopIdx));
			}
		}

		/// <summary>
		/// Tries to catch earlier trip at given stop.
		/// </summary>
		/// <returns>
		/// If earlier trip is found. Returns earlier trip with given stop as the stop we used to get on trip.
		/// Otherwise returns same trip with given stop used to get on the trip.
		/// </returns>
		private (TripWithDate, int) CatchEarlierTrip(TripWithDate currentTrip, int stopIdx, int transferStopIdx)
		{
			var route = currentTrip.Route;
			var stopData = stopsData.GetStopDataOrCreateNew(route.GetStop(stopIdx));
			if (stopData.GetArrivalTime(currentRound - 1) < currentTrip.GetArrivalAtStop(stopIdx))
			{
				TripWithDate earliestTrip = GetEarliestTripFromStop(route, stopIdx);

				if (currentTrip != earliestTrip)
				{
					return (earliestTrip, stopIdx);
				}
			}
			return (currentTrip, transferStopIdx);
		}

		/// <summary>
		/// Goes through each marked route and updates arrival times at stops on route.
		/// </summary>
		private void ProcessMarkedRoutes()
		{
			foreach (var markedRoute in markedRoutes)
			{
				var route = markedRoute.Key;
				var routeStops = route.GetStops();
				int transferStopIdx = -1;
				TripWithDate currentTrip = null;
				for (int stopIdx = markedRoute.Value; stopIdx < routeStops.Count; stopIdx++)
				{
					currentTrip = GetEarliestTripFromStop(route, stopIdx);
					if (currentTrip != null)
					{
						transferStopIdx = stopIdx;
						break;
					}
				}
				if (currentTrip == null) continue;

				// updates stops that follow the stop at which we got on trip
				for (int stopIdx = transferStopIdx + 1; stopIdx < routeStops.Count; stopIdx++)
				{
					UpdateArrivalTimeByTrip(currentTrip, stopIdx, transferStopIdx);
					(currentTrip, transferStopIdx) = CatchEarlierTrip(currentTrip, stopIdx, transferStopIdx);
				}
			}
		}

		private void UpdateArrivalTimeByTransfer(Transfer transfer, StopData srcData)
		{
			var newArrivalTime = srcData.GetArrivalTime(currentRound) + transfer.Time;
			var targetData = stopsData.GetStopDataOrCreateNew(transfer.Target);
			if (newArrivalTime < targetData.GetArrivalTime(currentRound))
			{
				targetData.SetArrivalTime(newArrivalTime, currentRound);
				markedStops.Add(transfer.Target);
				targetData.ArriveByTransfer(transfer);
			}
		}

		/// <summary>
		/// Uses transfers to improve arrival time at stops.
		/// </summary>
		private void ProcessTransfers()
		{
			//markedStops must be copied(by creating list) because UpdateArrivalTimeByTransfer marks new stops
			foreach (var stop in markedStops.ToList())
			{
				var src = stopsData.GetStopDataOrCreateNew(stop);
				foreach (var transfer in stop.GetTransfers())
				{
					if (src.GetArrivalTime(currentRound) == StopData.UNREACHABLE) continue; //overflow check

					UpdateArrivalTimeByTransfer(transfer, src);
				}
			}
		}

		/// <summary>
		/// Initializes arrival at start stop.
		/// 
		/// Unlike original RAPTOR - also marks source neighbors within transfer distance. Otherwise it doesn't work in some edge cases.
		/// </summary>
		/// <param name="rememberStopData">
		/// If true and startTime is later than previous startTime <see cref="ArgumentException"/> is thrown.
		/// </param>
		private void Init(IEnumerable<(Stop stop, TimeSpan walkTime)> srcStopsAndWalkTimes,
			DateTimeOffset startTime, bool rememberStopData = false)
		{
			void MarkSourceNeighbours(Stop srcStop)
			{
				var data = stopsData.GetStopDataOrCreateNew(srcStop);
				foreach (var transfer in srcStop.GetTransfers())
				{
					UpdateArrivalTimeByTransfer(transfer, data);
				}
			}

			currentRound = 0;
			if (rememberStopData)
			{
				if (lastStartTime < startTime) throw new ArgumentException("Previous startTime must be after current startTime!");
			}
			else
			{
				stopsData = new();
			}

			foreach (var (stop, walkTime) in srcStopsAndWalkTimes)
			{
				if (stop == null) throw new Exception("Source stop was not found");

				DateTimeOffset departureTime = startTime + walkTime;

				var srcStopData = stopsData.GetStopDataOrCreateNew(stop);
				srcStopData.SetArrivalTime(departureTime, 0);
				markedStops.Add(stop);

				MarkSourceNeighbours(stop);
			}

			currentRound = 1;
		}

		/// <summary>
		/// Finds Journey from souce stop to target stop starting after departureTime.
		/// </summary>
		/// <param name="noRoundLimit">Wether number of transfers should be limited or not.</param>
		/// <returns></returns>
		public Journey Solve(DateTimeOffset departureTime, Stop srcStop, Stop tarStop, bool noRoundLimit = false)
		{
			Solve(new List<(Stop, TimeSpan)>() { (srcStop, new TimeSpan(0)) }, departureTime, noRoundLimit);

			return Journey.FromRaptor(srcStop, tarStop, stopsData);
		}


		/************************************************************/
		/* From now on `custom raptor` => not based on RAPTOR paper	*/
		/************************************************************/


		/// <summary>
		/// Creates <see cref="StopData"/> for each stop reachable within <see cref="TotalRounds"/>.
		/// </summary>
		/// <param name="noRoundLimit">Wether number of transfers should be limited or not.</param>
		private void Solve(IEnumerable<(Stop, TimeSpan)> srcStopsAndWalkTimes, DateTimeOffset startTime,
			bool noRoundLimit = false, bool rememberStopData = false)
		{
#if Measure
			Stopwatch timer = new();
			timer.Start();
#endif

			Init(srcStopsAndWalkTimes, startTime, rememberStopData);
			while (noRoundLimit || currentRound <= TotalRounds)
			{
				stopsData.PropagateArrivalFromPreviousRound(currentRound);
				MarkRoutes();
				ProcessMarkedRoutes();
				ProcessTransfers();
				if (markedStops.Count == 0) break;
				currentRound++;
			}
#if Measure
			timer.Stop();
			Console.WriteLine($"raptors' solve took: {timer.ElapsedMilliseconds}ms");
#endif
			lastStartTime = startTime;
		}

		public Dictionary<Stop, TimeSpan> GetAccessibilityByStops(IEnumerable<(Stop, TimeSpan)> srcStopsAndWalkTimes,
			DateTimeOffset startTime, bool noRoundLimit = false, bool rememberStopData = false)
		{
			Solve(srcStopsAndWalkTimes, startTime, noRoundLimit, rememberStopData);

			var result = new Dictionary<Stop, TimeSpan>();
			foreach (var (stop, stopData) in stopsData.GetAllStopsWithData())
			{
				result.Add(stop, stopData.GetEarliestArrivalTime() - startTime);
			}

			return result;
		}
	}

}
