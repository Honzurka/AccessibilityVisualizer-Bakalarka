using Xunit;
using System.Collections.Generic;
using System.Linq;
using System;
using RaptorAlgo;
using GTFSData;
using TestShares;

namespace RaptorAlgoTests
{
	public static class TestExtensions
	{
		public static TimeSpan ToTimeSpan(this int time)
		{
			return TimeSpan.FromSeconds(time);
		}

		public static DateTimeOffset ToDateTime(this int time)
		{
			return DateTimeOffset.FromUnixTimeSeconds(time);
		}
	}

	class JourneyWithEquals : Journey
	{
		public override bool Equals(object obj)
		{
			if (obj != null && obj is Journey other)
			{
				return GetParts().SequenceEqual(other.GetParts());
			}
			return false;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public class RaptorTests
	{
		[Fact]
		public void Solve_1Trip0Transfers_FindsTrip()
		{
			//arrange
			var source = new Stop("First", "s1");
			var target = new Stop("Last", "s4");
			var timetable = new StubTimetable();
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(new Stop("Second", "s2"), new StopTime(3,4)),
				(new Stop("Third", "s3"), new StopTime(5,6)),
				(target, new StopTime(7,8)),
			});
			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, source, target));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), source, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_3Trips0Transfers_FindsTripByTime()
		{
			//arrange
			var s1 = new Stop("First", "s1");
			var s2 = new Stop("Second", "s2");
			var s3 = new Stop("Third", "s3");
			var s4 = new Stop("Last", "s4");

			var timetable = new StubTimetable();
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(3,4)),
				(s3, new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(3,4)),
				(s2, new StopTime(5,6)),
				(s3, new StopTime(7,8)),
				(s4, new StopTime(9,10)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(5,6)),
				(s2, new StopTime(7,8)),
				(s3, new StopTime(9,10)),
				(s4, new StopTime(11,12)),
			});

			var raptor = new Raptor();
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, s1, s4));

			//act
			Journey journey = raptor.Solve(3.ToDateTime(), s1, s4);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_2RoutesOnSimilarTrip_CheckTime()
		{
			//arrange
			var s1 = new Stop("First", "s1");
			var s2 = new Stop("Second", "s2");
			var s3 = new Stop("Third", "s3");
			var s4 = new Stop("Last", "s4");

			var timetable = new StubTimetable();
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(3,4)),
				(s3, new StopTime(5,6)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(3,4)),
				(s2, new StopTime(5,6)),
				(s3, new StopTime(7,8)),
				(s4, new StopTime(9,10)),
			});

			var raptor = new Raptor() { TotalRounds = 2 }; //required to show bug
			DateTimeOffset time = 0.ToDateTime();
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, s1, s3));

			//act
			Journey journey = raptor.Solve(time, s1, s3);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_2Trips1Transfer_ShouldTransfer()
		{
			//arrange
			var timetable = new StubTimetable();
			var source = new Stop("A", "s1");
			var target = new Stop("H", "s8");
			var s4 = new Stop("D", "s4");
			var s5 = new Stop("E", "s5");
			var tr = new Transfer(2.ToTimeSpan(), s4, s5);
			var t1 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(new Stop("B","s2"), new StopTime(3,4)),
				(new Stop("C","s3"), new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			var t2 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s5, new StopTime(12,13)),
				(new Stop("F","s6"), new StopTime(14,15)),
				(new Stop("G","s7"), new StopTime(16,17)),
				(target, new StopTime(18,19)),
			});
			timetable.AddTransfer(tr);

			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals()
				.AddPart(new PrintableTrip(t2, timetable.stops["s5"], target))
				.AddPart(new PrintableTransfer(tr))
				.AddPart(new PrintableTrip(t1, source, timetable.stops["s4"]));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), source, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_TripBeginningWithTranfser_()
		{
			//arrange
			var timetable = new StubTimetable();
			var source = new Stop("0", "s0");
			var s2 = new Stop("B", "s2");
			var target = new Stop("D", "s4");
			var tr = new Transfer(1.ToTimeSpan(), source, s2);
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(new Stop("A","s1"), new StopTime(1,2)), //unused stop
				(s2, new StopTime(3,4)),
				(new Stop("C","s3"), new StopTime(5,6)),
				(target, new StopTime(7,8)),
			});
			timetable.AddStop(source);
			timetable.AddTransfer(tr);

			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals()
				.AddPart(new PrintableTrip(t, timetable.stops["s2"], target))
				.AddPart(new PrintableTransfer(tr));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), source, target);

			//assert
			Assert.Equal(expectedJourney, journey);

		}

		[Fact]
		public void Solve_TripEndingWithTransfer_()
		{
			//arrange
			var timetable = new StubTimetable();
			var source = new Stop("A", "s1");
			var s4 = new Stop("D", "s4");
			var target = new Stop("E", "s5");
			var tr = new Transfer(1.ToTimeSpan(), s4, target);
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(new Stop("B","s2"), new StopTime(3,4)),
				(new Stop("C","s3"), new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			timetable.AddStop(target);
			timetable.AddTransfer(tr);

			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals()
				.AddPart(new PrintableTransfer(tr))
				.AddPart(new PrintableTrip(t, source, timetable.stops["s4"]));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), source, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_1TripNotWhole_()
		{
			//arrange
			var timetable = new StubTimetable();
			var source = new Stop("Second", "s2");
			var target = new Stop("Third", "s3");
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(new Stop("First", "s1"), new StopTime(1,2)),
				(source, new StopTime(3,4)),
				(target, new StopTime(5,6)),
				(new Stop("Last", "s4"), new StopTime(7,8)),
			});
			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, source, target));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), source, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_NonexistentSource()
		{
			//arrange
			var timetable = new StubTimetable();
			var target = new Stop("Last", "s4");
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(new Stop("First", "s1"), new StopTime(1,2)),
				(new Stop("Second", "s2"), new StopTime(3,4)),
				(new Stop("Third", "s3"), new StopTime(5,6)),
				(target, new StopTime(7,8))
			});
			var raptor = new Raptor();

			Assert.Throws<Exception>(() => raptor.Solve(0.ToDateTime(), null, target));
		}

		[Fact]
		public void Solve_NonexistentTarget()
		{
			//arrange
			var timetable = new StubTimetable();
			var source = new Stop("First", "s1");
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(new Stop("Second", "s2"), new StopTime(3,4)),
				(new Stop("Third", "s3"), new StopTime(5,6)),
				(new Stop("Last", "s4"), new StopTime(7,8)),
			});
			var raptor = new Raptor();

			Assert.Throws<Exception>(() => raptor.Solve(0.ToDateTime(), source, null).Print());
		}

		[Fact]
		public void Solve_ConsecutiveCalls()
		{
			//arrange
			var s1 = new Stop("First", "s1");
			var s2 = new Stop("Second", "s2");
			var s3 = new Stop("Third", "s3");
			var s4 = new Stop("Last", "s4");

			var timetable = new StubTimetable();
			var t1 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(3,4)),
				(s3, new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			var t2 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(3,4)),
				(s2, new StopTime(5,6)),
				(s3, new StopTime(7,8)),
				(s4, new StopTime(9,10)),
			});
			var t3 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(5,6)),
				(s2, new StopTime(7,8)),
				(s3, new StopTime(9,10)),
				(s4, new StopTime(11,12)),
			});

			var raptor = new Raptor();

			DateTimeOffset time = 0.ToDateTime();
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t1, s1, s4));
			Journey journey = raptor.Solve(time, s1, s4);
			Assert.Equal(expectedJourney, journey);

			time = 5.ToDateTime();
			expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t3, s1, s3));
			journey = raptor.Solve(time, s1, s3);
			Assert.Equal(expectedJourney, journey);

			time = 5.ToDateTime();
			expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t2, s2, s4));
			journey = raptor.Solve(time, s2, s4);
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_CatchEarlierTrip_ShouldChangeTrip()
		{
			//arrange
			var timetable = new StubTimetable();

			var s1 = new Stop("First", "s1");
			var s2 = new Stop("Second", "s2");
			var s3 = new Stop("Third", "s3");
			var s4 = new Stop("Last", "s4");

			var t1 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(3,4)),
				(s3, new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(3,4)),
				(s2, new StopTime(5,6)),
				(s3, new StopTime(7,8)),
				(s4, new StopTime(9,10)),
			});
			var t2 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(3,3)),
				(s3, new StopTime(4,5))
			});

			var raptor = new Raptor();
			DateTimeOffset time = 3.ToDateTime();

			var expectedJourney = new JourneyWithEquals()
				.AddPart(new PrintableTrip(t1, s3, s4))
				.AddPart(new PrintableTrip(t2, s1, s3));

			//act
			Journey journey = raptor.Solve(time, s1, s4);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_UnreachableTarget_ShouldThrow()
		{
			//arrange
			var timetable = new StubTimetable();
			var s1 = new Stop("A", "s1");
			var s5 = new Stop("E", "s5"); //unreachable
			var t1 = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(new Stop("B","s2"), new StopTime(3,4)),
				(new Stop("C","s3"), new StopTime(5,6)),
				(new Stop("D","s4"), new StopTime(7,8)),
			});

			var raptor = new Raptor();

			Assert.Throws<Exception>(() => raptor.Solve(0.ToDateTime(), s1, s5).Print());
		}

		[Fact]
		public void Solve_1Trip2Transfers_CheckTransferLooping()
		{
			//arrange
			var timetable = new StubTimetable();
			var s1 = new Stop("A", "s1");
			var s2 = new Stop("B", "s2");
			var s4 = new Stop("D", "s4");
			var s5 = new Stop("E", "s5");
			var tr1 = new Transfer(2.ToTimeSpan(), s4, s5);
			var tr2 = new Transfer(2.ToTimeSpan(), s5, s4);
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(3,4)),
				(new Stop("C", "s3"), new StopTime(5,6)),
				(s4, new StopTime(7,8)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s2, new StopTime(3,4)),
				(s5, new StopTime(100, 101))
			});
			timetable.AddTransfer(tr1);
			timetable.AddTransfer(tr2);

			var raptor = new Raptor();
			
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, s1, s4));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), s1, s4);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_2Trips1Transfer_TransferFasterThanTrip()
		{
			//arrange
			var timetable = new StubTimetable();
			var s1 = new Stop("A", "s1");
			var s2 = new Stop("B", "s2");
			var s3 = new Stop("C", "s3");
			var tr = new Transfer(2.ToTimeSpan(), s1, s3);
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(2,3)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s2, new StopTime(5,6)),
				(s3, new StopTime(100,101)),
			});
			timetable.AddTransfer(tr);

			var raptor = new Raptor();
			
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTransfer(tr));

			//act
			Journey journey = raptor.Solve(0.ToDateTime(), s1, s3);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_Reusability()
		{
			//arrange
			var timetable = new StubTimetable();
			var s1 = new Stop("A", "s1");
			var s2 = new Stop("B", "s2");
			var s3 = new Stop("C", "s3");
			var s4 = new Stop("D", "s4");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s1, new StopTime(1,2)),
				(s2, new StopTime(2,3)),
			});
			var t = timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s3, new StopTime(6,7)),
				(s4, new StopTime(8,9)),
			});
			var tr = new Transfer(1.ToTimeSpan(), s1, s3);
			timetable.AddTransfer(tr);

			var raptor = new Raptor();
			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t, s3, s4)).AddPart(new PrintableTransfer(tr));
			
			//act
			raptor.Solve(0.ToDateTime(), s1, s2);
			Journey journey = raptor.Solve(0.ToDateTime(), s1, s4);

			//assert
			Assert.Equal(expectedJourney, journey);
		}
	}


	public class DateRaptorTests
	{
		[Fact]
		public void Solve_ChoosingCorrectDate()
		{
			//arrange
			var timetable = new StubTimetable();
			var start = new Stop("A", "start");
			var target = new Stop("B", "target");

			var dto1 = DateTimeOffset.UnixEpoch;
			var dto2 = dto1.AddDays(1);
			var dto3 = dto1.AddDays(2);
			var dto = dto2.AddHours(-1);

			var t = timetable.AddTripWithDates(new()
			{
				(start, new StopTime(1, 2)),
				(target, new StopTime(3, 4))
			}, new()
			{
				dto1,
				dto2,
				dto3
			});
			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals().AddPart(new PrintableTrip(t[1], start, target));

			//act
			Journey journey = raptor.Solve(dto, start, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void Solve_RouteOverMidnight()
		{
			//arrange
			var timetable = new StubTimetable();
			var start = new Stop("A", "start");
			var s1 = new Stop("B", "s1");
			var target = new Stop("C", "target");
			
			var dto1 = DateTimeOffset.UnixEpoch;
			var dto2 = dto1.AddDays(1);

			var t0 = timetable.AddTripWithDates(new()
			{
				(start, new StopTime(10, 20)),
				(s1, new StopTime(30, 40))
			}, new()
			{
				dto1
			});
			var t1 = timetable.AddTripWithDates(new()
			{
				(s1, new StopTime(0, 1)),
				(target, new StopTime(2, 3))
			}, new()
			{
				dto2
			});
			var raptor = new Raptor();

			var expectedJourney = new JourneyWithEquals()
				.AddPart(new PrintableTrip(t1[0], s1, target))
				.AddPart(new PrintableTrip(t0[0], start, s1));

			//act
			Journey journey = raptor.Solve(dto1, start, target);

			//assert
			Assert.Equal(expectedJourney, journey);
		}

		[Fact]
		public void MarkedRouteWithoutTripsInFuture_ShouldNotCreateEmptyStopData()
		{
			var timetable = new StubTimetable();
			var source = new Stop("First", "s1");
			var target = new Stop("Second", "s2");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(target, new StopTime(3,4)),
			});
			var raptor = new Raptor();

			raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>() { (source, TimeSpan.Zero) }, 10.ToDateTime());

			Assert.Empty(raptor.stopsData.allStopsData.Where(kv => kv.Value.arrivalTimes.Count == 0));
		}
	}


	public class ExtendedRaptorTests
	{
		[Fact]
		public void GetTravelTimeByStops_GeneralCase_StartA()
		{
			//arrange
			var timetable = new StubTimetable();
			var start1 = new Stop("A", "start1");
			var start2 = new Stop("B", "start2");
			var target = new Stop("C", "target");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start1, new StopTime(1,2)),
				(target, new StopTime(2,3)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start2, new StopTime(1,20)),
				(target, new StopTime(20,21)),
			});

			var raptor = new Raptor();
			var expectedTravelTimeByStops = new Dictionary<Stop, TimeSpan>()
			{
				{ start1, 0.ToTimeSpan() },
				{ start2, 0.ToTimeSpan() },
				{ target, 2.ToTimeSpan() }
			};

			//act
			var accessibilityByStops = raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>()
				{ (start1, 0.ToTimeSpan()), (start2, 0.ToTimeSpan()) }, 0.ToDateTime());

			//assert
			Assert.Equal(expectedTravelTimeByStops, accessibilityByStops);
		}

		[Fact]
		public void GetTravelTimeByStops_GeneralCase_StartB()
		{
			var timetable = new StubTimetable();
			var start1 = new Stop("A", "start1");
			var start2 = new Stop("B", "start2");
			var target = new Stop("C", "target");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start1, new StopTime(1,20)),
				(target, new StopTime(20,21)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start2, new StopTime(1,4)),
				(target, new StopTime(4,5)),
			});

			var raptor = new Raptor();
			var expectedTravelTimeByStops = new Dictionary<Stop, TimeSpan>()
			{
				{ start1, 0.ToTimeSpan() },
				{ start2, 0.ToTimeSpan() },
				{ target, 4.ToTimeSpan() }
			};

			//act
			var accessibilityByStops = raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>()
				{ (start1, 0.ToTimeSpan()), (start2, 0.ToTimeSpan()) }, 0.ToDateTime());

			//assert
			Assert.Equal(expectedTravelTimeByStops, accessibilityByStops);
		}

		[Fact]
		public void GetTravelTimeByStops_GeneralCase_WithWalkTimes()
		{
			//arrange
			var timetable = new StubTimetable();
			var start1 = new Stop("A", "start1");
			var start2 = new Stop("B", "start2");
			var target = new Stop("C", "target");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start1, new StopTime(4,5)),
				(target, new StopTime(7,8)),
			});
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(start2, new StopTime(4,5)),
				(target, new StopTime(6,7)),
			});

			var raptor = new Raptor();
			var expectedTravelTimeByStops = new Dictionary<Stop, TimeSpan>()
			{
				{ start1, 2.ToTimeSpan() },
				{ start2, 3.ToTimeSpan() },
				{ target, 6.ToTimeSpan() }
			};

			//act
			var accessibilityByStops = raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>()
				{ (start1, 2.ToTimeSpan()), (start2, 3.ToTimeSpan()) }, 0.ToDateTime());

			//assert
			Assert.Equal(expectedTravelTimeByStops, accessibilityByStops);
		}

		[Fact]
		public void GetAccessibilityByStops_WithRememberedStopData_SimulatesConsecutiveCalls()
		{
			var timetable = new StubTimetable();
			var source = new Stop("First", "s1");
			var target = new Stop("Second", "s2");
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(source, new StopTime(1,2)),
				(target, new StopTime(3,4)),
			});
			var raptor = new Raptor();


			// following shouldn't throw exception
			raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>() { (source, TimeSpan.Zero) }, 1.ToDateTime());
			raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>() { (source, TimeSpan.Zero) }, 0.ToDateTime(), rememberStopData: true);

			raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>() { (source, TimeSpan.Zero) }, 1.ToDateTime());
			raptor.GetAccessibilityByStops(new List<(Stop, TimeSpan)>() { (source, TimeSpan.Zero) }, 0.ToDateTime(), rememberStopData: true);


			Assert.True(true);
		}
	}
}
