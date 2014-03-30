using System;
using System.Collections.Generic;

namespace MetalAccounting
{
	public interface IFileParser
	{
		List<Transaction> Parse(string fileName);
	}
}

