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
			directory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "blueprints"));
		}

		Directory.CreateDirectory(directory);
		string prefixValue = Plugin.BlueprintFilePrefix.Value;
		string prefix = (prefixValue == null || prefixValue.Trim().Length == 0) ? "blueprint" : prefixValue.Trim();
		string safeName = BlueprintRuntime.MakeSafeFileName(blueprint.strName);
		string fileName = prefix + "_" + safeName + ".json";
		string path = Path.Combine(directory, fileName);
		File.WriteAllText(path, JsonMapper.ToJson(blueprint));
		return path;
	}
}
