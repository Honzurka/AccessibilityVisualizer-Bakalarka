using System;

namespace GTFSData
{
    /// <summary>
    /// Helper class for calculations in WGS84 projection.
    /// Meter is used as distance unit in all calculations.
    /// </summary>
	public sealed class WGS84
	{
        /// <summary>
        /// Earth radius in meters. Used in calculations.
        /// </summary>
        static double earthRadiusM = 6376500.0;

        /// <summary>
        /// Calculates distance between 2 points specified in Latitude and Longitude coordinations.
        /// 
        /// Source of formula: https://stackoverflow.com/a/51839058
        /// </summary>
        /// <returns>Distance in meters.</returns>
        public static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return earthRadiusM * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

        /// <summary>
        /// Calculates bounding box around given circle (specified by lat/lon point and distance)
        /// </summary>
        /// <returns>Top-left and bottom-right coordinates of rectangle around specified circle. </returns>
        public static (double minLat, double maxLat, double minLon, double maxLon)
            GetBoundaryBox(double latitude, double longitude, double offsetMeters)
		{
            var latOffset = (offsetMeters / earthRadiusM) * (180.0 / Math.PI);
            var lonOffset = (offsetMeters / earthRadiusM) * (180.0 / Math.PI) / Math.Cos(latitude * Math.PI / 180.0);

            return (latitude - latOffset, latitude + latOffset, longitude - lonOffset, longitude + lonOffset);
        }
    }
}
