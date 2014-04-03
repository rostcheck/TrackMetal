using System;

namespace MetalAccounting
{
	public static class MatchAlgorithmFactory
	{
		public static ITransactionListProcessor Create(MatchAlgorithmEnum matchAlgorithm)
		{
			switch (matchAlgorithm)
			{
				case MatchAlgorithmEnum.MatchAcrossTransactions:
					return new MatchAcrossTransactionsAlgorithm();
				case MatchAlgorithmEnum.SimilarTransactions:
					return new MatchSimilarTransactions();
				default:
					throw new Exception("Unknown match algorithm " + matchAlgorithm);
			}
		}
	}
}

