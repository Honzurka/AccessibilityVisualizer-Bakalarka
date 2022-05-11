using TestShares;
using GTFSData;
using System;
using System.Collections.Generic;
using RaptorAlgo;
using StopAccessibility;
using Xunit;

namespace StopAccessibilityTests
{
	class TimetableCreator
	{
		public static StubTimetable GetDefault()
		{
			var result = new StubTimetable();

			var s0 = new Stop("", "0", 10, 10);
			var s1 = new Stop("", "1", 10, 11);
			var s2 = new Stop("", "2", 11, 10);
			var s3 = new Stop("", "3", 11, 11);
			var s4 = new Stop("", "4", 10, 10.001); //near s0
			var s5 = new Stop("", "5", 12, 12);

			// just adding Stops, trip isn't used
			result.AddTripByParts(new List<(Stop, StopTime)>
			{
				(s0, new StopTime(0, 0)),
				(s1, new StopTime(0, 0)),
				(s2, new StopTime(0, 0)),
				(s3, new StopTime(0, 0)),
				(s4, new StopTime(0, 0)),
				(s5, new StopTime(0, 0))
			});
			return result;
		}
	}

	class StubRouter : ITransitRouter
	{
		ITimetable timetable;

		public StubRouter(ITimetable timetable)
		{
			this.timetable = timetable;
		}

		private static void InsertOrUpdate(Dictionary<Stop, TimeSpan> dict, Stop key, TimeSpan val)
		{
			if (dict.TryGetValue(key, out TimeSpan oldVal))
			{
				if (val < oldVal) dict[key] = val;
			}
			else
			{
				dict.Add(key, val);
			}
		}

		public Dictionary<Stop, TimeSpan> GetAccessibilityByStops(IEnumerable<(Stop stop, TimeSpan walkTime)> srcStopsAndWalkTimes,
			DateTimeOffset startTime, bool noRoundLimit = false, bool rememberStopData = false)
		{
			var result = new Dictionary<Stop, TimeSpan>();

			if(startTime == DateTimeOffset.MinValue) //UNUSED startTime from following tests
			{
				foreach (var (stop, walkTime) in srcStopsAndWalkTimes)
				{
					switch (stop.Id)
					{
						case "0":
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(100));
							InsertOrUpdate(result, timetable.GetStopById("2"), walkTime + TimeSpan.FromSeconds(200));
							InsertOrUpdate(result, timetable.GetStopById("3"), walkTime + TimeSpan.FromSeconds(300));
							InsertOrUpdate(result, timetable.GetStopById("5"), walkTime + TimeSpan.FromSeconds(5000));
							break;
						case "1":
							InsertOrUpdate(result, timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(100));
							InsertOrUpdate(result, timetable.GetStopById("2"), walkTime + TimeSpan.FromSeconds(400));
							InsertOrUpdate(result, timetable.GetStopById("3"), walkTime + TimeSpan.FromSeconds(500));
							break;
						case "2":
							InsertOrUpdate(result, timetable.GetStopById("2"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(600));
							InsertOrUpdate(result, timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(700));
							//3 unreachable
							break;
						case "3":
							break;
						case "4":
							InsertOrUpdate(result, timetable.GetStopById("4"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(1000));
							InsertOrUpdate(result, timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(1100));
							InsertOrUpdate(result, timetable.GetStopById("5"), walkTime + TimeSpan.FromSeconds(5));
							break;
						case "5":
							InsertOrUpdate(result, timetable.GetStopById("5"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(5000));
							InsertOrUpdate(result, timetable.GetStopById("4"), walkTime + TimeSpan.FromSeconds(5));
							break;
						default:
							throw new Exception("Unknown id");
					}
				}
			}
			else if(startTime == DateTimeOffset.MinValue.AddHours(1))
			{

				foreach (var (stop, walkTime) in srcStopsAndWalkTimes)
				{
					switch (stop.Id)
					{
						case "0":
							InsertOrUpdate(result, timetable.GetStopById("0"), walkTime + TimeSpan.FromSeconds(0));
							InsertOrUpdate(result, timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(2));
							InsertOrUpdate(result, timetable.GetStopById("2"), walkTime + TimeSpan.FromSeconds(4));
							break;
						case "1":
							
							break;
						case "2":
							break;
						case "3":
							break;
						case "4":
							break;
						case "5":
							break;
						default:
							throw new NotImplementedException();
					}
				}
			}
			return result;
		}
	}

	public class StopAccessibilityFinderTests
	{
		static readonly DateTimeOffset UNUSED = DateTimeOffset.MinValue;

		[Fact]
		public void GetAvgAccessByStop_IsCorrect()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { UNUSED }, 1),
				new TargetData(timetable.GetStopById("1").Latitude, timetable.GetStopById("1").Longitude, new() { UNUSED }, 1),
			});
			var expectedResult = new Dictionary<Stop, TimeSpan>
			{
				{ timetable.GetStopById("0"), TimeSpan.FromSeconds(50) },
				{ timetable.GetStopById("1"), TimeSpan.FromSeconds(50) },
				{ timetable.GetStopById("2"), TimeSpan.FromSeconds(300) },
				{ timetable.GetStopById("3"), TimeSpan.FromSeconds(400) },
			};

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_Weights()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { UNUSED }, 1),
				new TargetData(timetable.GetStopById("1").Latitude, timetable.GetStopById("1").Longitude, new() { UNUSED }, 2),
			});
			var expectedResult = new Dictionary<Stop, TimeSpan>
			{
				{ timetable.GetStopById("0"), TimeSpan.FromSeconds(200) / 3d },
				{ timetable.GetStopById("1"), TimeSpan.FromSeconds(100) / 3d },
				{ timetable.GetStopById("2"), TimeSpan.FromSeconds(1000) / 3d },
				{ timetable.GetStopById("3"), TimeSpan.FromSeconds(1300d) / 3d },
			};

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_TargetsWithDifferentTimes()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { DateTimeOffset.MinValue.AddHours(1) }, 1),
				new TargetData(timetable.GetStopById("1").Latitude, timetable.GetStopById("1").Longitude, new() { UNUSED }, 1),
			});
			var expectedResult = new Dictionary<Stop, TimeSpan>
			{
				{ timetable.GetStopById("0"), TimeSpan.FromSeconds(50)},
				{ timetable.GetStopById("1"), TimeSpan.FromSeconds(1)},
				{ timetable.GetStopById("2"), TimeSpan.FromSeconds(202)},
			};

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_MultipleDates()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { UNUSED, DateTimeOffset.MinValue.AddHours(1) }, 1),
			});
			var expectedResult = new Dictionary<Stop, TimeSpan>
			{
				{ timetable.GetStopById("0"), TimeSpan.FromSeconds(0)},
				{ timetable.GetStopById("1"), TimeSpan.FromSeconds(51)},
				{ timetable.GetStopById("2"), TimeSpan.FromSeconds(102)},
			};

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_RemovesStopsUnreachableFromAnyTarget()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { UNUSED }, 1),
				new TargetData(timetable.GetStopById("2").Latitude, timetable.GetStopById("2").Longitude, new() { UNUSED }, 1),
			});
			var expectedResult = new Dictionary<Stop, TimeSpan> {
				{ timetable.GetStopById("0"), TimeSpan.FromSeconds(300) },
				{ timetable.GetStopById("1"), TimeSpan.FromSeconds(400) },
				{ timetable.GetStopById("2"), TimeSpan.FromSeconds(100) },
			};

			//assert
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_UsesStopsWithinWalkingDistanceToOptimizeTravelTime()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("4").Latitude, timetable.GetStopById("4").Longitude, new() { UNUSED }, 1),
			});
			TimeSpan walkTime = TimeSpan.FromSeconds(WGS84.GetDistance(
				timetable.GetStopById("0").Longitude, timetable.GetStopById("0").Latitude,
				timetable.GetStopById("4").Longitude, timetable.GetStopById("4").Latitude
			));
			var expectedResult = new Dictionary<Stop, TimeSpan> {
				{ timetable.GetStopById("0"), walkTime }, //within walking distance
				{ timetable.GetStopById("1"), walkTime + TimeSpan.FromSeconds(100) },
				{ timetable.GetStopById("2"), walkTime + TimeSpan.FromSeconds(200) },
				{ timetable.GetStopById("3"), walkTime + TimeSpan.FromSeconds(300) },
				{ timetable.GetStopById("4"), TimeSpan.FromSeconds(0) },
				{ timetable.GetStopById("5"), TimeSpan.FromSeconds(5) },
			};

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAvgAccessByStop_NoNeighbor()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			Assert.Throws<UnreachableTargetException>(() => saf.GetAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("3").Latitude, timetable.GetStopById("3").Longitude, new() { UNUSED }, 1),
			}));
		}

		[Fact]
		public void GetAccessForCoords_IsCorrect()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAccessForCoords(10.002, 10.002, new List<TargetData>() {
				new TargetData(timetable.GetStopById("1").Latitude, timetable.GetStopById("1").Longitude, new() { UNUSED }, 1),
				new TargetData(timetable.GetStopById("5").Latitude, timetable.GetStopById("5").Longitude, new() { UNUSED }, 1),
			});
			TimeSpan expectedResult = (
				TimeSpan.FromSeconds(WGS84.GetDistance(10.002, 10.002, timetable.GetStopById("0").Longitude, timetable.GetStopById("0").Latitude)) + TimeSpan.FromSeconds(100) +
				TimeSpan.FromSeconds(WGS84.GetDistance(10.002, 10.002, timetable.GetStopById("4").Longitude, timetable.GetStopById("4").Latitude)) + TimeSpan.FromSeconds(5)
				) / 2;

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAccessForCoords_Weights()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAccessForCoords(10.002, 10.002, new List<TargetData>() {
				new TargetData(timetable.GetStopById("1").Latitude, timetable.GetStopById("1").Longitude, new() { UNUSED }, 1),
				new TargetData(timetable.GetStopById("5").Latitude, timetable.GetStopById("5").Longitude, new() { UNUSED }, 2),
			});
			TimeSpan expectedResult = (
				TimeSpan.FromSeconds(WGS84.GetDistance(10.002, 10.002, timetable.GetStopById("0").Longitude, timetable.GetStopById("0").Latitude)) + TimeSpan.FromSeconds(100) +
				2 * (TimeSpan.FromSeconds(WGS84.GetDistance(10.002, 10.002, timetable.GetStopById("4").Longitude, timetable.GetStopById("4").Latitude)) + TimeSpan.FromSeconds(5))
				) / 3;

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void GetAccessForCoords_AllUnreachable()
		{
			var timetable = TimetableCreator.GetDefault();
			var saf = new StopAccessibilityFinder(new StubRouter(timetable), timetable);

			//act
			var result = saf.GetAccessForCoords(20, 20, new List<TargetData>() {
				new TargetData(timetable.GetStopById("0").Latitude, timetable.GetStopById("0").Longitude, new() { UNUSED }, 1),
			});

			Assert.Equal(StopAccessibilityFinder.UNREACHABLE, result);
		}

	}

	public class IntervalAccessibilityTests
	{
		static readonly DateTimeOffset DEFAULT_DTO = DateTimeOffset.MinValue.AddDays(1);

		private void AddTripForMultipleDates(StubTimetable timetable, List<DateTimeOffset> dates, List<(Stop, StopTime)> parts)
		{
			foreach (var date in dates)
			{
				timetable.AddTripByParts(parts, date);
			}
			timetable.SortRouteTrips();
		}

		[Theory]
		[InlineData(10, 1, 100, 11, 300)]
		public void OneTargetOneDateTwoIntervalTimes_IsCorrect(int intervalSize, uint s1out1, uint s2in1, uint s1out2, uint s2in2)
		{
			var timetable = new StubTimetable();
			var first = new Stop("First", "s1", 0, 0);
			var last = new Stop("Last", "s2", 10, 10);
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(first, new StopTime(s1out1, s1out1)),
				(last, new StopTime(s2in1, s2in1))
			}, DEFAULT_DTO);
			timetable.AddTripByParts(new List<(Stop, StopTime)>
			{
				(first, new StopTime(s1out2, s1out2)),
				(last, new StopTime(s2in2, s2in2))
			}, DEFAULT_DTO);

			var saf = new StopAccessibilityFinder(new Raptor(), timetable);


			var result = saf.GetStatisticalAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("s1").Latitude, timetable.GetStopById("s1").Longitude, new() { DEFAULT_DTO }, 1),
			}, TimeSpan.FromSeconds(intervalSize), TimeSpan.FromSeconds(intervalSize));
			
			TimeSpan expectedResult = TimeSpan.FromSeconds(
				(s2in1 + intervalSize / 2 + s2in2 - intervalSize / 2) / 2
			);


			Assert.Equal(expectedResult, result[last]);
		}

		[Fact]
		public void MultipleTargetsOneDateIntervalTimes_IsCorrect()
		{
			int intervalSize = 10;
			uint s1out1 = 1;
			uint s2in1 = 100;
			uint s1out2 = 11;
			uint s2in2 = 300;
			var first = new Stop("First", "s1", 0, 0);
			var last = new Stop("Last", "s2", 10, 10);

			var timetable = new StubTimetable();
			AddTripForMultipleDates(timetable, new() { DEFAULT_DTO, DEFAULT_DTO.AddDays(1), DEFAULT_DTO.AddDays(2) }, new() {
				(first, new StopTime(s1out1, s1out1)),
				(last, new StopTime(s2in1, s2in1))
			});
			AddTripForMultipleDates(timetable, new() { DEFAULT_DTO, DEFAULT_DTO.AddDays(1), DEFAULT_DTO.AddDays(2) }, new() {
				(first, new StopTime(s1out2, s1out2)),
				(last, new StopTime(s2in2, s2in2))
			});

			var saf = new StopAccessibilityFinder(new Raptor(), timetable);

			TimeSpan expectedResult = TimeSpan.FromSeconds((s2in1 + s2in2) / 2);
			var result = saf.GetStatisticalAvgAccessByStop(new List<TargetData>() {
				new TargetData(timetable.GetStopById("s1").Latitude, timetable.GetStopById("s1").Longitude, new() { DEFAULT_DTO }, 1),
				new TargetData(timetable.GetStopById("s1").Latitude, timetable.GetStopById("s1").Longitude, new() { DEFAULT_DTO.AddDays(2) }, 1),
				new TargetData(timetable.GetStopById("s1").Latitude, timetable.GetStopById("s1").Longitude, new() { DEFAULT_DTO.AddDays(1) }, 1)
			}, TimeSpan.FromSeconds(intervalSize), TimeSpan.FromSeconds(intervalSize));
			

			Assert.Equal(expectedResult, result[last]);
		}
	}
}
