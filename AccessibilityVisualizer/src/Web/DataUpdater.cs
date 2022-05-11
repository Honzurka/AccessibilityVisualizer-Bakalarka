using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Config;
using GTFSData;

namespace Web
{
	public static class DataUpdater
	{
		/// <summary>
		/// Downloads zip file from GTFSSourceURI (defined in cfg).
		/// Unzips file to PathToGTFSFolder (defined in cfg)
		/// 
		/// Might throw exception if file couldn't be downloaded or unzipped.
		/// </summary>
		private static void DownloadData()
		{
			// won't be sufficient if downloaded file isn't zip
			try
			{
				if (AppConfig.appSettings.ShouldUpdate)
				{
					HttpClient client = new HttpClient();
					client.Timeout = TimeSpan.FromMinutes(10); // for downloading bigger files

					client.GetAsync(AppConfig.appSettings.GTFSSourceURI)
						.ContinueWith(response => response.Result.Content.ReadAsStreamAsync().Result)
						.ContinueWith(stream => new ZipArchive(stream.Result))
						.ContinueWith(zip => zip.Result.ExtractToDirectory(AppConfig.appSettings.PathToGTFSFolder, true))
						.Wait();
				}
			}
			catch (Exception e)
			{
				if (e is InvalidOperationException ||
					e is HttpRequestException ||
					e is System.Threading.Tasks.TaskCanceledException)
				{
					throw new Exception("Can't download data from GTFS data provider");
				}
				else
				{
					throw; //probably can't unzip file or doesn't have write access to GTFSFolder
				}
			}
		}

		/// <summary>
		/// Updates GTFS data if ShouldUpdate (from cfg) is set
		/// or if file for deserialization doesn't exist.
		/// </summary>
		/// <returns></returns>
		public static ITimetable GetTimetable()
		{
			var serializationFilePath = AppConfig.appSettings.GTFSSerializationPath;
			if (AppConfig.appSettings.ShouldUpdate || !File.Exists(serializationFilePath))
			{
				DownloadData();

				var data = new RawData();
				return Timetable.FromData(data);
				//unzipped files could be removed after serialization
			}
			else
			{
				return Timetable.FromSerialization();
			}
		}
	}
}
