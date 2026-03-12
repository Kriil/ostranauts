using System;
using System.IO;
using LitJson;

namespace Ostranauts.Blueprints;

internal static class BlueprintPersistence
{
	internal static string Save(BlueprintData blueprint)
	{
		string directory = Plugin.BlueprintDirectory.Value;
		if (string.IsNullOrWhiteSpace(directory))
		{
			directory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "blueprints"));
		}

		Directory.CreateDirectory(directory);
		string prefix = string.IsNullOrWhiteSpace(Plugin.BlueprintFilePrefix.Value) ? "blueprint" : Plugin.BlueprintFilePrefix.Value.Trim();
		string safeName = BlueprintRuntime.MakeSafeFileName(blueprint.strName);
		string fileName = prefix + "_" + safeName + ".json";
		string path = Path.Combine(directory, fileName);
		File.WriteAllText(path, JsonMapper.ToJson(blueprint));
		return path;
	}
}
