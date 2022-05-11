using System;
using System.Collections.Generic;
using GTFSData;
using Config;

namespace StopAccessibility
{
	// not tested yet
	/// <summary>
	/// Uses linear distance for distance measurements.
	/// </summary>
	public static class NearestStops
	{
		static readonly uint DISTANCE_LIMIT = AppConfig.appSettings.NearestStopsDistanceInMeters;

		/// <returns>Stops within <see cref="DISTANCE_LIMIT"/> of given coords</returns>
		public static IEnumerable<(Stop, TimeSpan)> GetStopsWithWalkTime(ITimetable timetable, double lat, double lon)
		{
			List<(Stop, TimeSpan)> result = new();

			foreach (var stop in timetable.GetCoordNborsWithinDistance(lat, lon, DISTANCE_LIMIT))
			{
				double distance = WGS84.GetDistance(stop.Longitude, stop.Latitude, lon, lat);
				var travelTime = TimeSpan.FromSeconds(distance / Timetable.WALKING_SPEED);
				result.Add((stop, travelTime));
			}

			return result;
		}
	}
}