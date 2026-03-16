using System;
using System.IO;
using LitJson;

namespace Ostranauts.Blueprints;

internal static class BlueprintPersistence
{
	internal static string Save(BlueprintData blueprint)
	{
		string directory = Plugin.BlueprintDirectory.Value;
		if (directory == null || directory.Trim().Length == 0)
		{
			directory = Path.Combine(Environment.CurrentDirectory, "Ostranauts_Data");
			directory = Path.Combine(directory, "Mods");
			directory = Path.Combine(directory, Plugin.PluginName);
			directory = Path.Combine(directory, "saved_blueprints");
			directory = Path.GetFullPath(directory);
		}

		Directory.CreateDirectory(directory);
		string prefixValue = Plugin.BlueprintFilePrefix.Value;
		string prefix = (prefixValue == null || prefixValue.Trim().Length == 0) ? "blueprint" : prefixValue.Trim();
		string safeName = BlueprintRuntime.MakeSafeFileName(blueprint.strName);
		string prefixWithSeparator = prefix + "_";
		string fileNameBase = safeName.StartsWith(prefixWithSeparator, StringComparison.OrdinalIgnoreCase)
			? safeName
			: prefixWithSeparator + safeName;
		string fileName = fileNameBase + ".json";
		string path = Path.Combine(directory, fileName);
		File.WriteAllText(path, JsonMapper.ToJson(blueprint));
		return path;
	}
}
