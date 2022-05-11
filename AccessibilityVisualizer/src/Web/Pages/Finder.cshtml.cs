using System;
using System.Collections.Generic;
using System.Linq;
using GTFSData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StopAccessibility;

namespace Web.Pages
{
	struct PointWithAccessibility
	{
		public readonly double latitude;
		public readonly double longitude;
		public readonly double totalSec;
		public readonly string name;

		public PointWithAccessibility(double latitude, double longitude, double totalSec, string name)
		{
			this.latitude = latitude;
			this.longitude = longitude;
			this.totalSec = totalSec;
			this.name = name;
		}
	}

	public sealed class FinderModel : PageModel
    {
		private readonly StopAccessibilityFinder saf;
		public ITimetable timetable;
		private readonly PointsWithNeighbors pwn;

		private readonly ILogger<FinderModel> logger;

		public Dictionary<string, string> visualizationTypes = new()
		{
			{ "stops", "Vizualizovat zastávky" },
			{ "points", "Vizualizovat body v rastru" },
			{ "all", "Vizualizovat zastávky i body v rastru" },
		};

		/// <summary>
		/// Uses dependency injection to acquire services.
		/// </summary>
		public FinderModel(StopAccessibilityFinder saf, ITimetable timetable, ILogger<FinderModel> logger, PointsWithNeighbors pwn)
		{
			this.saf = saf;
			this.timetable = timetable;
			this.logger = logger;
			this.pwn = pwn;
		}

		/// <summary>
		/// Used when adding stop by name.
		/// </summary>
		public IActionResult OnGetStopId(string stopId)
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}
			var stop = timetable.GetStopById(stopId);
			if (stop == null)
			{
				logger.LogError("Shouldn't happen: OnGetStopId - stop is null");
				return BadRequest(stopId);
			}

			return Content(JsonConvert.SerializeObject(new { latitude = stop.Latitude, longitude = stop.Longitude }));
		}

		/// <param name="dto">Datetime to be formatted</param>
		/// <returns>Parameter <paramref name="dto"/> in format YYYY-MM-DDThh:mm</returns>
		public string FormatDateTimeOffset(DateTimeOffset dto)
		{
			return $"{dto.Year.ToString("D4")}-{dto.Month.ToString("D2")}-{dto.Day.ToString("D2")}T{dto.Hour.ToString("D2")}:{dto.Minute.ToString("D2")}";
		}

		/// <summary>
		/// Handles accessibility calculation
		/// </summary>
		/// <param name="jsonData">Target selected by user in JSON format</param>
		/// <param name="calculateStatistics">Wether accessibility should be calculated in interval</param>
		/// <returns>
		/// Coords and accessibility in seconds serialized as JSON.
		/// If some target is unreachable returns <see cref="BadRequestResult"/> with unreachable target data instead.
		/// </returns>
		public IActionResult OnPost(string jsonData, string visualizationType, bool calculateStatistics = false)
		{
			/// <summary>
			/// Date validation.
			/// Requires runtime information => can't be specified using attribute.
			/// </summary>
			bool ValidDates(List<TargetData> targets)
			{
				foreach (var target in targets)
				{
					foreach (var date in target.Dates)
					{
						if (date < timetable.StartDate || date > timetable.EndDate) return false;
					}
				}
				return true;
			}
			
			IEnumerable<PointWithAccessibility> EnumerateTimeSpanByStop(IDictionary<Stop, TimeSpan> data)
			{
				return data.Select(keyval => new PointWithAccessibility(
					keyval.Key.Latitude, keyval.Key.Longitude, keyval.Value.TotalSeconds, keyval.Key.Name)
				);
			}

			/// <summary>
			/// Calculates accessibility from targets.
			/// Based on visualisationType returns points for visualisation.
			/// </summary>
			IEnumerable<PointWithAccessibility> GetPointsWithAccessibility(List<TargetData> targets, string visualizationType)
			{
				IEnumerable<PointWithAccessibility> result = Enumerable.Empty<PointWithAccessibility>();
				if (visualizationType == "stops" || visualizationType == "all")
				{
					if (calculateStatistics)
					{
						result = result.Concat(EnumerateTimeSpanByStop(saf.GetStatisticalAvgAccessByStop(targets)));
					}
					else
					{
						result = result.Concat(EnumerateTimeSpanByStop(saf.GetAvgAccessByStop(targets)));
					}
				}
				else if (visualizationType == "points" || visualizationType == "all")
				{
					//doesn't calculate statistics - StopAccessibilityFinder doesn't cache results in interval
					List<PointWithAccessibility> newPoints = new();
					foreach (var point in pwn.GetPoints())
					{
						newPoints.Add(new PointWithAccessibility(
							point.lat,
							point.lon,
							saf.GetAccessForCoordNbors(point.nbors, targets).TotalSeconds,
							$"point at: {point.lat},{point.lon}"
						));
					}
					result = result.Concat(newPoints);
				}

				return result;
			}

			List<TargetData> targets = null;
			try
			{
				targets = JsonConvert.DeserializeObject<List<TargetData>>(jsonData);
			}
			catch (Exception)
			{
				logger.LogError("Finder.OnPost - failed to deserialize target data.");
				return Page();
			}
			
			//validation
			TryValidateModel(targets, nameof(targets));
			if (!ModelState.IsValid || !ValidDates(targets))
			{
				return Page();
			}

			try
			{
				IEnumerable<PointWithAccessibility> result = GetPointsWithAccessibility(targets, visualizationType);
				return Content(JsonConvert.SerializeObject(result));
			}
			catch(UnreachableTargetException e)
			{
				return BadRequest(JsonConvert.SerializeObject(e.TargetData));
			}
		}
	}
}
