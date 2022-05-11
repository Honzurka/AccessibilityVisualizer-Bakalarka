using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Config
{
	/// <summary>
	/// Reflects structure in AppConfig file
	/// </summary>
	public sealed class AppSettings
	{
		public bool ShouldUpdate { get; set; }
		public string GTFSSourceURI { get; set; }
		public string PathToGTFSFolder { get; set; }
		public string GTFSSerializationPath { get; set; }
		public uint ValidityInDays { get; set; }
		public int UTCOffset { get; set; }

		public uint MaxTransferDistanceInMeters { get; set; }
		public float WalkingSpeedInMetersPerSec { get; set; }

		public uint NearestStopsDistanceInMeters { get; set; }

		public double VisualisedRasterPointsResolution { get; set; }
	}

	/// <summary>
	/// Application configuration based on Options pattern.
	/// </summary>
	public sealed class AppConfig
	{
		public static readonly AppSettings appSettings;
		static AppConfig()
		{
			// relative pathing is required - otherwise copied settings doesn't get updated and causes bugs
			var appsettingsPath = AppPath.GetFullPathRelativeToProjectFolder($"cfg{Path.DirectorySeparatorChar}appsettings.json");

			var cfg = new ConfigurationBuilder().AddJsonFile(appsettingsPath, false, true).Build();

			AppSettings instance = new AppSettings();
			cfg.GetSection(nameof(AppSettings)).Bind(instance);
			appSettings = instance;

			//paths made absolute
			appSettings.GTFSSerializationPath = AppPath.GetFullPathRelativeToProjectFolder(appSettings.GTFSSerializationPath);
			appSettings.PathToGTFSFolder = AppPath.GetFullPathRelativeToProjectFolder(appSettings.PathToGTFSFolder);
		}
	}

	public sealed class AppPath
	{
		private static char sep = Path.DirectorySeparatorChar;
		static readonly string projectPath = Path.GetFullPath($"..{sep}..{sep}..{sep}..{sep}..{sep}", AppContext.BaseDirectory);
		public static string GetFullPathRelativeToProjectFolder(string path)
		{
			return Path.GetFullPath(path, projectPath);
		}
	}
}
