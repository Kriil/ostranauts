using System;
// FFU_BR extends the base `JsonCond` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonCond : JsonCond
{
	public string strReference { get; set; }
}
