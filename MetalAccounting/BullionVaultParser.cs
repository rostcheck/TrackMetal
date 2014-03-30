using System;
using System.Collections.Generic;

namespace MetalAccounting
{
	public class BullionVaultParser : IFileParser
	{
		public BullionVaultParser()
		{
		}

		public List<Transaction> Parse(string fileName)
		{
			return new List<Transaction>();
		}
	}
}

