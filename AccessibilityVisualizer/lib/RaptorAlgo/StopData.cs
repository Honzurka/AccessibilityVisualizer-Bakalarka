using System;
using System.Collections.Generic;
using System.Linq;
using GTFSData;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RaptorTests")]

namespace RaptorAlgo
{
	sealed class StopData
	{
		public readonly static DateTimeOffset UNREACHABLE = DateTimeOffset.MaxValue;

		/// <summary>
		/// Represents transfer from other stop to this stop.
		/// </summary>
		Transfer transfer;

		/// <summary>
		/// Represents stop where we got on <see cref="trip"/>.
		/// </summary>
		Stop previousStop;
		
		TripWithDate trip;

		internal readonly List<DateTimeOffset> arrivalTimes = new();

		public void SetArrivalTime(DateTimeOffset time, int round)
		{
			EnsureArrivalTimesSize(round + 1);
			arrivalTimes[round] = time;
		}

		/// <summary>
		/// Safe. Returns <see cref="UNREACHABLE"/> if round is out of range.
		/// </summary>
		public DateTimeOffset GetArrivalTime(int round)
		{
			if (arrivalTimes.Count > round)
			{
				return arrivalTimes[round];
			}
			return UNREACHABLE;
		}

		/// <summary>
		/// Useful mainly when computing arrival times in interval.
		/// </summary>
		public DateTimeOffset GetEarliestArrivalTime()
		{
			return arrivalTimes.Min();
		}

		public void ArriveByTransfer(Transfer t)
		{
			transfer = t;
			previousStop = null;
			trip = null;
		}

		public void ArriveByTrip(TripWithDate t, Stop previousStop)
		{
			trip = t;
			this.previousStop = previousStop;
			transfer = null;
		}

		/// <summary>
		/// Used in <see cref="Journey"/>.
		/// </summary>
		/// <returns></returns>
		public Transfer GetTransfer()
		{
			return transfer;
		}

		/// <summary>
		/// Used in <see cref="Journey"/>.
		/// </summary>
		/// <returns>Stop from which we got on trip.</returns>
		public (TripWithDate, Stop) GetTripInfo()
		{
			return (trip, previousStop);
		}

		/// <summary>
		/// Propagates arrival time from previous round only if it's earlier than arrival time in current round.
		/// Particularly useful in rRAPTOR - interval computations.
		/// </summary>
		public void PropagateArrival(int currenRound)
		{
			if (GetArrivalTime(currenRound - 1) < GetArrivalTime(currenRound))
			{
				SetArrivalTime(GetArrivalTime(currenRound - 1), currenRound);
			}
		}

		private void EnsureArrivalTimesSize(int size)
		{
			int sizeDeficit = size - arrivalTimes.Count;
			if (sizeDeficit > 0)
			{
				arrivalTimes.AddRange(Enumerable.Repeat(UNREACHABLE, sizeDeficit));
			}
		}
	}


	sealed class StopsData
	{
		internal readonly Dictionary<Stop, StopData> allStopsData = new();

		public StopData GetExistingStopData(Stop stop)
		{
			if (!allStopsData.TryGetValue(stop, out StopData result))
			{
				result = new StopData();
			}
			return result;
		}

		/// <returns>
		///		existing StopData if stop is in stopData
		///		otherwise returns newly created StopData for stop
		///	</returns>
		public StopData GetStopDataOrCreateNew(Stop stop)
		{
			if (!allStopsData.TryGetValue(stop, out StopData result))
			{
				result = new StopData();
				allStopsData.Add(stop, result);
			}
			return result;
		}

		/// <summary>
		/// Copies arrival times from previous round to current round for all stops with data.
		/// </summary>
		/// <param name="currentRound"></param>
		public void PropagateArrivalFromPreviousRound(int currentRound)
		{
			foreach (var (stop, stopdata) in allStopsData)
			{
				stopdata.PropagateArrival(currentRound);
			}
		}

		public IEnumerable<KeyValuePair<Stop,StopData>> GetAllStopsWithData()
		{
			foreach (var keyval in allStopsData)
			{
				yield return keyval;
			}
		}
	}
}
