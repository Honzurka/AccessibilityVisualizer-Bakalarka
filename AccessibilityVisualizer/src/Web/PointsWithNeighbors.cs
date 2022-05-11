using System;
using System.Collections.Generic;
using StopAccessibility;
using GTFSData;
using Config;


namespace Web
{
	/// <summary>
	/// Used to precalculate neighbors of points.
	/// Makes calculating accessibility at those points faster.
	/// </summary>
	public class PointsWithNeighbors
	{
		ITimetable timetable;

		IEnumerable<(IEnumerable<(Stop, TimeSpan)> nbors, double lat, double lon)> data = null;

		public PointsWithNeighbors(ITimetable timetable)
		{
			this.timetable = timetable;
		}

		// could be optimized using serialization
		public IEnumerable<(IEnumerable<(Stop, TimeSpan)> nbors, double lat, double lon)> GetPoints()
		{
			if (data != null) return data; //ensures data are calculated only once

			var result = new List<(IEnumerable<(Stop, TimeSpan)> nbors, double lat, double lon)>();

			var (minLat, maxLat, minLon, maxLon) = timetable.StopPositionLimits;
			double resolution = AppConfig.appSettings.VisualisedRasterPointsResolution;
			double latStep = (maxLat - minLat) / resolution;
			double lonStep = (maxLon - minLon) / resolution;
			for (double lat = minLat; lat < maxLat; lat += latStep)
			{
				for (double lon = minLon; lon < maxLon; lon += lonStep)
				{
					var nbors = NearestStops.GetStopsWithWalkTime(timetable, lat, lon);
					result.Add((nbors, lat, lon));
				}
			}

			data = result;
			return data;
		}
	}
}
