using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalAccounting
{
	public class MatchSimilarTransactions : ITransactionListProcessor
	{
		public MatchSimilarTransactions()
		{
		}

		// Like transfers, like kind exchanges become one transfer transaction (the receiving side) and a 
		// storage fee (on the sending side account) that accounts for any effect on basis.
		public List<Transaction> FormLikeKindExchanges(List<Transaction> transactionList)
		{
			Console.WriteLine();
			Console.WriteLine("Identifying like kind exchanges using similar transactions algorithm:");
			List<Transaction> transactionsToRemove = new List<Transaction>();
			string formatString = "Matched {0} {1} from {2} on {3} of {4} {5} (transaction ID {6}) with {7} to {8} on {9} of {10} {11} (transaction ID {12})";				
			List<Transaction> sourceTransactions = new List<Transaction>();

			foreach (Transaction sourceTransaction in transactionList
				.Where(s => s.TransactionType == TransactionTypeEnum.Sale)
				.OrderBy(s => s.DateAndTime))
			{
				Transaction receiveTransaction = GetPossibleLikeKindTransaction(sourceTransaction, transactionList);
				if (receiveTransaction == null)
					continue; // no match

				Console.WriteLine(formatString, sourceTransaction.MetalType, sourceTransaction.TransactionType, 
					sourceTransaction.Service, sourceTransaction.DateAndTime.ToShortDateString(),
					sourceTransaction.Weight, sourceTransaction.WeightUnit, sourceTransaction.TransactionID,
					receiveTransaction.TransactionType, receiveTransaction.Service, 
					receiveTransaction.DateAndTime.ToShortDateString(), receiveTransaction.Weight, 
					receiveTransaction.WeightUnit, receiveTransaction.TransactionID);	
				if (receiveTransaction.AmountReceived != sourceTransaction.AmountPaid)
				{
					decimal amountDifference = sourceTransaction.AmountPaid - Utils.ConvertWeight(receiveTransaction.AmountReceived,
						receiveTransaction.WeightUnit, sourceTransaction.WeightUnit);
					// Create a metal storage fee to account for the difference
					Transaction storageFee = new Transaction(sourceTransaction.Service, sourceTransaction.Account,
						sourceTransaction.DateAndTime, sourceTransaction.TransactionID, TransactionTypeEnum.StorageFeeInMetal,
						sourceTransaction.Vault, amountDifference, sourceTransaction.CurrencyUnit, 0.0m, 
						sourceTransaction.WeightUnit, sourceTransaction.MetalType, 
						"Transfer fee in metal from like-kind exchange " + sourceTransaction.TransactionID);
					transactionList.Add(storageFee);
				}

				// Set the source vault property in the receipt side
				receiveTransaction.MakeTransfer(sourceTransaction.Account, sourceTransaction.Vault);
				sourceTransactions.Add(sourceTransaction);
			}
			foreach (Transaction sourceTransaction in transactionsToRemove)
				transactionList.Remove(sourceTransaction);
			return transactionList;
		}

		// A similar transaction is a later purchase within 30 days of the sale transaction with the same
		// metal and within 10% of the price. May returns null.
		private Transaction GetPossibleLikeKindTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			TransactionTypeEnum oppositeTransactionType = transaction.GetOppositeTransactionType();
			if (oppositeTransactionType != TransactionTypeEnum.Purchase && oppositeTransactionType != TransactionTypeEnum.Sale)
				return null;
			else
			{
				Transaction returnTransaction = transactionList.Where(
					s => s.TransactionType == TransactionTypeEnum.Purchase
					&& s.DateAndTime <= (transaction.DateAndTime + new TimeSpan(30, 0, 0, 0))
					&& s.DateAndTime >= transaction.DateAndTime
					&& s.MetalType == transaction.MetalType
					&& s.TransactionType == oppositeTransactionType
					&& s.AmountReceived >= (.9m * transaction.AmountPaid))
						.OrderBy(s => s.DateAndTime).FirstOrDefault();

				// Prefer later transactions, but an earlier one may qualify
				if (returnTransaction == null)
					returnTransaction = transactionList.Where(
						s => s.TransactionType == TransactionTypeEnum.Purchase
						&& s.DateAndTime >= (transaction.DateAndTime - new TimeSpan(30, 0, 0, 0))
						&& s.DateAndTime <= transaction.DateAndTime
						&& s.MetalType == transaction.MetalType
						&& s.TransactionType == oppositeTransactionType
						&& s.AmountReceived >= (.9m * transaction.AmountPaid))
						.OrderBy(s => s.DateAndTime).FirstOrDefault();
//				if (returnTransaction != null)
//				{
//					Console.WriteLine("boo");
//				}
				return returnTransaction;
			}
		}
	}
}


