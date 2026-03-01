using System;
// FFU_BR extends the base `JsonPersonSpec` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonPersonSpec : JsonPersonSpec
{
	public string strReference { get; set; }
}
