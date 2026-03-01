using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using UnityEngine;

namespace Ostranauts.Tools
{
	public class DotNetZipCompressor : ICompressionProvider
	{
		public Exception CompressFolder(string saveFolderPath)
		{
			string[] directories = Directory.GetDirectories(saveFolderPath);
			string name = new DirectoryInfo(saveFolderPath).Name;
			string fileName = saveFolderPath + "/" + name + ".zip";
			Exception result;
			try
			{
				using (ZipFile zipFile = new ZipFile())
				{
					string[] files = Directory.GetFiles(saveFolderPath);
					zipFile.AddFiles(files, string.Empty);
					this.CompressSubFolders(zipFile, string.Empty, directories);
					zipFile.Save(fileName);
				}
				result = null;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("aborted"))
				{
					Debug.Log("Could not compress save folder, " + ex.Message);
				}
				else
				{
					Debug.LogError("Could not compress save folder, " + ex.Message);
				}
				result = ex;
			}
			return result;
		}

		private void CompressSubFolders(ZipFile zip, string parentFolder, string[] folders)
		{
			foreach (string path in folders)
			{
				string name = new DirectoryInfo(path).Name;
				string[] files = Directory.GetFiles(path);
				zip.AddFiles(files, parentFolder + name);
				string[] directories = Directory.GetDirectories(path);
				if (directories != null && directories.Length > 0)
				{
					this.CompressSubFolders(zip, name + "/", directories);
				}
			}
		}

		public string ExtractArchive(string fullPathToZipFile, string pathToExtractionFolder)
		{
			if (!File.Exists(fullPathToZipFile))
			{
				return null;
			}
			string result;
			try
			{
				using (ZipFile zipFile = ZipFile.Read(fullPathToZipFile))
				{
					string directoryName = Path.GetDirectoryName(fullPathToZipFile);
					foreach (ZipEntry zipEntry in zipFile)
					{
						zipEntry.Extract(pathToExtractionFolder, ExtractExistingFileAction.OverwriteSilently);
					}
				}
				result = fullPathToZipFile;
			}
			catch (Exception ex)
			{
				Debug.LogError("Could not unzip compressed save file, " + ex.Message);
				result = null;
			}
			return result;
		}

		public Dictionary<string, byte[]> ExtractArchive(string fullPathToZipFile)
		{
			if (!File.Exists(fullPathToZipFile))
			{
				return null;
			}
			Dictionary<string, byte[]> result;
			try
			{
				Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
				using (ZipFile zipFile = ZipFile.Read(fullPathToZipFile))
				{
					string directoryName = Path.GetDirectoryName(fullPathToZipFile);
					foreach (ZipEntry zipEntry in zipFile)
					{
						MemoryStream memoryStream = new MemoryStream();
						zipEntry.Extract(memoryStream);
						dictionary.Add(zipEntry.FileName, memoryStream.ToArray());
					}
				}
				result = dictionary;
			}
			catch (Exception ex)
			{
				Debug.LogError("Could not unzip compressed save file, " + ex.Message);
				result = null;
			}
			return result;
		}
	}
}
