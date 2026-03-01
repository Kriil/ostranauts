using System;
using System.Collections.Generic;

namespace Ostranauts.Tools
{
	public interface ICompressionProvider
	{
		Exception CompressFolder(string saveFolderPath);

		string ExtractArchive(string fullPathToZipFile, string pathToExtractionFolder);

		Dictionary<string, byte[]> ExtractArchive(string fullPathToZipFile);
	}
}
