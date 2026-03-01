using System;
// FFU_BR extends the base `JsonColor` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonColor : JsonColor
{
	public string strReference { get; set; }
}
