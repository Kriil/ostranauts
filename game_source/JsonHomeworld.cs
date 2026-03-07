using System;

// Homeworld/colony definition used by character generation and social flavor.
// These records likely come from a `data/homeworlds`-style registry and are
// referenced by JsonPersonSpec when filtering or assigning a person's origin.
public class JsonHomeworld
{
	// `strName` is the internal id; `strATCCode` is the short code used in UI/filters.
	public string strName { get; set; }

	public string strATCCode { get; set; }

	public string strColonyName { get; set; }

	public int nFoundingYear { get; set; }

	public bool bPCOnly { get; set; }

	// Condition ids granted or checked for citizens, residents, or illegal status.
	public string[] aCondsCitizen { get; set; }

	public string[] aCondsResident { get; set; }

	public string[] aCondsIllegal { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
