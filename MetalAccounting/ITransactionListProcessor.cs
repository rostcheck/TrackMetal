using System;
using System.Collections.Generic;

namespace MetalAccounting
{
	public interface ITransactionListProcessor
	{
		List<Transaction> FormLikeKindExchanges(List<Transaction> transactionList);
	}
}

