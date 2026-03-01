using System;
// FFU_BR extends the base `JsonPlot` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonPlot : JsonPlot
{
	public string strReference { get; set; }
}
