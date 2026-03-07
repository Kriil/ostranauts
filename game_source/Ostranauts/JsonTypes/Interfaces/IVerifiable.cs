using System;
using System.Collections;
using System.Collections.Generic;

namespace Ostranauts.JsonTypes.Interfaces
{
	public interface IVerifiable
	{
		IDictionary<string, IEnumerable> GetVerifiables();
	}
}
