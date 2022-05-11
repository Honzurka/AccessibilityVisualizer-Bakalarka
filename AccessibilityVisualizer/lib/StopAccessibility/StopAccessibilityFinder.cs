using GTFSData;
using RaptorAlgo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace StopAccessibility
{
	/// <summary>
	/// Either late date or unreachable by trip/transfer.
	/// </summary>
	public sealed class UnreachableTargetException : Exception
	{
		public struct UnreachableTargetData
		{
			public int TargetIdx { get; private set; }
			public double Latitude { get; set; }
			public double Longitude { get; set; }
			
			public UnreachableTargetData(int targetIdx, double latitude, double longitude)
			{
				TargetIdx = targetIdx;
				Latitude = latitude;
				Longitude = longitude;
			}
		}

		public UnreachableTargetData TargetData { get; private set; }

		public UnreachableTargetException(UnreachableTargetData data)
		{
			TargetData = data;
		}

	}

	/// <summary>
	/// Represents target selected by user.
	/// Is immutable.
	/// </summary>
	public struct TargetData
	{
		public TargetData(double latitude, double longitude, List<DateTimeOffset> dates, double weight)
		{
			Latitude = latitude;
			Longitude = longitude;
			Dates = dates;
			Weight = weight;
		}

		public TargetData WithDates(List<DateTimeOffset> newDates)
		{
			return new TargetData(Latitude, Longitude, newDates, Weight);
		}

		[Range(-180, 180)]
		public double Latitude { get; private set; }

		[Range(-180, 180)]
		public double Longitude { get; private set; }
		
		public List<DateTimeOffset> Dates { get; private set; }
		
		/// <summary>
		/// Higher weight => less important.
		/// Used for weighted average computation.
		/// </summary>
		[Range(0.1, 10)]
		public double Weight { get; private set; }
	}

	/// <summary>
	/// Used `targets` are stops selected by user.
	/// RAPTOR calculates accessibility from targets to other stops.
	/// </summary>
	public sealed class StopAccessibilityFinder
	{
		readonly ITransitRouter router;
		readonly ITimetable timetable;

		/// <summary>
		/// Caches 1 calculation.
		/// </summary>
		bool calculated = false;

		/// <summary>
		/// Calculation result.
		/// </summary>
		List<Dictionary<Stop, TimeSpan>> accessForTargets;

		public static readonly TimeSpan UNREACHABLE = TimeSpan.MaxValue;

		public StopAccessibilityFinder(ITransitRouter router, ITimetable timetable)
		{
			this.router = router;
			this.timetable = timetable;
		}

		/// <summary>
		/// Removes stops unreachable by any target (member of <paramref name="travelTimesByStops"/>).
		/// If weights are specified computes weighted average of travelTime from results from targets.
		/// Otherwise computes standard average.
		/// </summary>
		/// <param name="travelTimesByStops">Contains accessibilities for each target.</param>
		private static Dictionary<Stop, TimeSpan> GetAvgTravelTimeByStop(List<Dictionary<Stop, TimeSpan>> travelTimesByStops, List<double> weights = null)
		{
			(Dictionary<Stop, uint> stopReachability, Dictionary<Stop, TimeSpan> stopTravelTime) SumStopAccessibilities()
			{
				Dictionary<Stop, uint> stopReachability = new();
				Dictionary<Stop, TimeSpan> stopTravelTime = new();

				for (int i = 0; i < travelTimesByStops.Count; i++)
				{
					foreach (var (stop, travelTime) in travelTimesByStops[i])
					{
						if (stopReachability.ContainsKey(stop))
						{
							stopReachability[stop]++;
							stopTravelTime[stop] += travelTime * weights[i];
						}
						else
						{
							stopReachability.Add(stop, 1);
							stopTravelTime.Add(stop, travelTime * weights[i]);
						}
					}
				}

				return (stopReachability, stopTravelTime);
			}

			IEnumerable<Stop> GetReachableStops(Dictionary<Stop, uint> stopReachability)
			{
				int targetCount = travelTimesByStops.Count;
				return stopReachability.Keys.Where(stop => stopReachability[stop] == targetCount);
			}

			// calculates weighted avg
			Dictionary<Stop, TimeSpan> GetAvgAccess(Dictionary<Stop, TimeSpan> stopTravelTime, IEnumerable<Stop> reachableStops)
			{
				Dictionary<Stop, TimeSpan> result = new();
				double totalWeights = weights.Sum();

				foreach (var stop in reachableStops)
				{
					result.Add(stop, stopTravelTime[stop] / totalWeights);
				}

				return result;
			}


			if (travelTimesByStops.Count == 1) return travelTimesByStops[0]; //optimization for simple cases

			if (weights == null) weights = Enumerable.Repeat(1d, travelTimesByStops.Count).ToList();

			var (stopReachability, stopTravelTime) = SumStopAccessibilities();
			var reachableStops = GetReachableStops(stopReachability);

			return GetAvgAccess(stopTravelTime, reachableStops);
		}

		/// <summary>
		/// Lazily calculates <see cref="accessForTargets"/>.
		/// 
		/// Parameters <paramref name="intervalSize"/> and <paramref name="intervalStep"/>
		/// are used only in (closed) interval.
		/// 
		/// Accessibility is averaged if target is associated with multiple dates.
		/// </summary>
		/// <param name="targets"></param>
		/// <param name="intervalSize">Half used before and half after target date/</param>
		/// <param name="intervalStep">Determines granularity.</param>
		private void CalcAccessibility(IEnumerable<TargetData> targets, TimeSpan? intervalSize = null, TimeSpan? intervalStep = null)
		{
			List<Dictionary<Stop, TimeSpan>> GetAccessibilitiesInInterval(IEnumerable<(Stop,TimeSpan)> targetNeighbors, DateTimeOffset date)
			{
				var newDate = date + intervalSize.Value / 2;
				List<Dictionary<Stop, TimeSpan>> result = new();

				//first time in interval
				result.Add(router.GetAccessibilityByStops(targetNeighbors, newDate, true));

				// other times in interval
				for (newDate = newDate - intervalStep.Value; newDate >= date - intervalSize.Value / 2; newDate -= intervalStep.Value)
				{
					result.Add(router.GetAccessibilityByStops(targetNeighbors, newDate, true, rememberStopData: true));
				}

				return result;
			}

			bool isInterval = intervalSize != null;

			if (!isInterval && calculated) return;
			calculated = true;

			accessForTargets = new();
			int idx = 0;
			foreach (var target in targets)
			{
				var targetNeighbors = NearestStops.GetStopsWithWalkTime(timetable, target.Latitude, target.Longitude);
				List<Dictionary<Stop, TimeSpan>> accessibilityByStops = new();
				foreach (var date in target.Dates) //might be optimized in the future
				{
					if(isInterval)
					{
						var accessibilitiesInInterval = GetAccessibilitiesInInterval(targetNeighbors, date);
						accessibilityByStops.Add(GetAvgTravelTimeByStop(accessibilitiesInInterval)); //no weights => all times in interval have equal relevance
					}
					else
					{
						accessibilityByStops.Add(router.GetAccessibilityByStops(targetNeighbors, date, true));
					}
				}

				var avgTravelTimeByStop = GetAvgTravelTimeByStop(accessibilityByStops); //avg over dates
				if (avgTravelTimeByStop.Count == 0) throw new UnreachableTargetException(new(idx, target.Latitude, target.Longitude));
				accessForTargets.Add(avgTravelTimeByStop);
				idx++;
			}
		}

		/// <summary>
		/// Calculates weighted average of accessibilities from given <paramref name="targets"/>.
		/// </summary>
		/// <exception cref="UnreachableTargetException">
		///		Thrown if result is empty.
		///		Problematic target is specified in exception.
		///	</exception>
		public Dictionary<Stop, TimeSpan> GetAvgAccessByStop(IEnumerable<TargetData> targets)
		{
			CalcAccessibility(targets);

			var weights = targets.Select(t => t.Weight).ToList();
			return GetAvgTravelTimeByStop(accessForTargets, weights);
		}

		/// <summary>
		/// Calculates weighted average of accessibilities from given <paramref name="targets"/> over specifiec interval.
		/// 
		/// More precise version of <see cref="GetAvgAccessByStop(IEnumerable{TargetData})"/>.
		/// Consumes more time and memory to calculate.
		/// </summary>
		/// <exception cref="UnreachableTargetException">
		///		Thrown if result is empty.
		///		Problematic target is specified in exception.
		///	</exception>
		public Dictionary<Stop, TimeSpan> GetStatisticalAvgAccessByStop(IEnumerable<TargetData> targets, TimeSpan? intervalSize = null, TimeSpan? intervalStep = null)
		{
			if (intervalSize == null) intervalSize = TimeSpan.FromHours(1);
			if (intervalStep == null) intervalStep = TimeSpan.FromMinutes(1);

			CalcAccessibility(targets, intervalSize, intervalStep);

			var weights = targets.Select(t => t.Weight).ToList();
			return GetAvgTravelTimeByStop(accessForTargets, weights);
		}


		/// <summary>
		/// Faster alternative to <see cref="GetAccessForCoords(double, double, IEnumerable{TargetData})"/>.
		/// Uses precalculated neighbors of given coords.
		/// </summary>
		/// <param name="nborAccess">Neighbors of coord with walkTime.</param>
		/// <returns>
		/// Accessibility for given neighbors of coords.
		/// If all neighbors are unreachable from any target returns <see cref="UNREACHABLE"/>
		/// </returns>
		public TimeSpan GetAccessForCoordNbors(IEnumerable<(Stop stop, TimeSpan walkTime)> nborAccess, IEnumerable<TargetData> targets)
		{
			TimeSpan GetShortestTravelTimeFrom(Dictionary<Stop, TimeSpan> targetAccess)
			{
				var result = UNREACHABLE;
				foreach (var (stop, walkTime) in nborAccess)
				{
					if (targetAccess.TryGetValue(stop, out var travelTime))
					{
						var totalTime = walkTime + travelTime;
						if (result > totalTime) result = totalTime;
					}
				}
				return result;
			}

			//from each target calculates shortest travel time to any of neighbours
			List<TimeSpan> GetTravelTimeFromTargets()
			{
				List<TimeSpan> result = new();
				foreach (var targetAccess in accessForTargets)
				{
					var shortestTravelTime = GetShortestTravelTimeFrom(targetAccess);
					if (shortestTravelTime == UNREACHABLE) return null;
					result.Add(shortestTravelTime);
				}
				return result;
			}

			TimeSpan GetWeightedAvg(IList<TimeSpan> vals)
			{
				var result = new TimeSpan(0);
				var weights = targets.Select(t => t.Weight).ToList();
				for (int i = 0; i < vals.Count; i++)
				{
					result += vals[i] * weights[i];
				}
				return result / weights.Sum();
			}

			CalcAccessibility(targets);
			var travelTimeFromTargets = GetTravelTimeFromTargets();
			if (travelTimeFromTargets == null) return UNREACHABLE;

			return GetWeightedAvg(travelTimeFromTargets);
		}

		/// <returns>
		/// Accessibility for given neighbors of coords.
		/// If all neighbors are unreachable from any target returns <see cref="UNREACHABLE"/>
		/// </returns>
		public TimeSpan GetAccessForCoords(double lat, double lon, IEnumerable<TargetData> targets)
		{
			var nbors = NearestStops.GetStopsWithWalkTime(timetable, lat, lon);
			return GetAccessForCoordNbors(nbors, targets);
		}
	}
}
