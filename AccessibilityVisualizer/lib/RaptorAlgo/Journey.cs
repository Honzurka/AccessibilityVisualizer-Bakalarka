using System;
using System.Collections.Generic;
using GTFSData;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("RaptorAlgoTests")]

namespace RaptorAlgo
{
	/// <summary>
	/// Represents printable trip or transfer.
	/// Helps in printing <see cref="Journey"/> without the need of distinguishing between trip and transfer.
	/// </summary>
	public interface IPrintable
	{
		const string separator = "==================================================";
		void Print();
	}

	/// <summary>
	/// Represents part of trip taken in journey.
	/// Pickup/dropoff stop represents when traveller got on/off trip.
	/// </summary>
	sealed class PrintableTrip : IPrintable
	{
		readonly TripWithDate trip;
		readonly Stop pickUp;
		readonly Stop dropOff;

		public PrintableTrip(TripWithDate trip, Stop pickUp, Stop dropOff)
		{
			this.trip = trip;
			this.pickUp = pickUp;
			this.dropOff = dropOff;
		}

		public void Print()
		{
			Console.WriteLine($"{trip.Route.Id}");
			Console.WriteLine($"from {pickUp.Name} at {trip.GetDepartureFromStop(pickUp)}");
			Console.WriteLine($"to {dropOff.Name} at {trip.GetDepartureFromStop(dropOff)}");
			Console.WriteLine(IPrintable.separator);
		}

		public override bool Equals(object obj)
		{
			if (obj is PrintableTrip)
			{
				var other = obj as PrintableTrip;
				return trip == other.trip && pickUp == other.pickUp && dropOff == other.dropOff;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(trip, pickUp, dropOff);
		}
	}

	/// <summary>
	/// Represents <see cref="Transfer"/>.
	/// Between 2 trips or at the end/beginning of journey.
	/// </summary>
	sealed class PrintableTransfer : IPrintable
	{
		readonly Transfer transfer;

		public PrintableTransfer(Transfer transfer)
		{
			this.transfer = transfer;
		}

		public void Print()
		{
			Console.WriteLine($"Transfer from {transfer.Source.Name} to {transfer.Target.Name} takes {transfer.Time}.");
			Console.WriteLine(IPrintable.separator);
		}

		public override bool Equals(object obj)
		{
			if(obj is PrintableTransfer)
			{
				var other = obj as PrintableTransfer;
				return transfer == other.transfer;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(transfer);
		}
	}

	/// <summary>
	/// Used to find sequence of trips and transfers between source and target stop.
	/// Designed as singleton.
	/// 
	/// Mostly used for raptor debugging/testing.
	/// </summary>
	public class Journey
	{
		readonly Stack<IPrintable> parts = new();
		public IEnumerable<IPrintable> GetParts()
		{
			return parts;
		}

		Stop srcStop;
		Stop targetStop;
		StopsData stopsData;

		protected Journey() { }

		internal static Journey FromRaptor(Stop srcStop, Stop targetStop, StopsData stopsData)
		{
			var j = new Journey
			{
				srcStop = srcStop,
				targetStop = targetStop,
				stopsData = stopsData
			};
			return j.CreateFromRaptor();
		}

		/// <summary>
		/// Prints trips and transfer delimited by <see cref="IPrintable.separator"/>.
		/// </summary>
		public void Print()
		{
			CreateFromRaptor();

			foreach (var part in parts)
			{
				part.Print();
			}
		}

		/// <summary>
		/// Used to create Journeys for validation.
		/// </summary>
		internal Journey AddPart(IPrintable part)
		{
			parts.Push(part);
			return this;
		}

		private Journey CreateFromRaptor()
		{
			if (targetStop == null) throw new Exception("Target stop was not found");


			var stop = targetStop;
			var data = stopsData.GetStopDataOrCreateNew(stop);

			if (data.GetTransfer() == null && data.GetTripInfo().Item1 == null && srcStop != targetStop)
			{
				throw new Exception("Target is not accessible");
			}

			while (stop != srcStop)
			{
				Transfer transfer;
				if ((transfer = data.GetTransfer()) != null)
				{
					AddPart(new PrintableTransfer(transfer));
					stop = transfer.Source;
				}
				else
				{
					var (trip, previousStop) = data.GetTripInfo();
					AddPart(new PrintableTrip(trip, previousStop, stop));
					stop = previousStop;
				}
				data = stopsData.GetStopDataOrCreateNew(stop);
			}
			return this;
		}
	}
}
