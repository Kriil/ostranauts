using System;
using System.Collections.Generic;
// FFU_BR extends the base `JsonModInfo` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonModInfo : JsonModInfo
{
	public Dictionary<string, string[]> removeIds { get; set; }
	public Dictionary<string, Dictionary<string, string[]>> changesMap { get; set; }
}
