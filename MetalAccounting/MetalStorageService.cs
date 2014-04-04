using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalAccounting
{
	public class MetalStorageService
	{
		private List<Lot> lots;
		private List<TaxableSale> sales;

		public List<Lot> Lots
		{
			get
			{
				return new List<Lot>(lots);
			}
		}

		public List<TaxableSale> Sales
		{
			get
			{
				return new List<TaxableSale>(sales);
			}
		}

		public MetalStorageService()
		{
			lots = new List<Lot>();
			sales = new List<TaxableSale>();
		}

		public void ApplyTransactions(List<Transaction> transactionList)
		{
			transactionList = FormTransfers(transactionList);
			transactionList = MatchAlgorithmFactory.Create(MatchAlgorithmEnum.MatchAcrossTransactions)
				.FormLikeKindExchanges(transactionList);
			foreach (Transaction transaction in transactionList.OrderBy(s => s.DateAndTime))
			{
				switch (transaction.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						Console.WriteLine(string.Format("{0} {1} received {2:0.000} {3}s {4} to account {5}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(),
							transaction.Account));
						PurchaseNewLot(transaction);
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
						Console.WriteLine(string.Format("{0} {1} sold {2:0.000} {3}s {4} from account {5}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountPaid, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.Account));
						ProcessSale(transaction);
						break;
					case TransactionTypeEnum.Transfer:
						Console.WriteLine(string.Format("{0} {1} transferred {2:0.000} {3}s {4} from account {5}, vault {6} to account {7}, vault {8}",
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.TransferFromAccount, transaction.TransferFromVault, transaction.Account, transaction.Vault));
						ProcessTransfer(transaction);
							break;
					case TransactionTypeEnum.StorageFeeInCurrency:
						ApplyStorageFeeInCurrency(transaction);
						break;
					case TransactionTypeEnum.StorageFeeInMetal:
						ApplyStorageFeeInMetal(transaction);
						break;
				}
			}
		}

		// For transfers, combine both sides into one transaction
		private List<Transaction> FormTransfers(List<Transaction> transactionList)
		{
			List<Transaction> sourceTransactions = new List<Transaction>();
			foreach (Transaction transaction in transactionList.Where(
				s => s.TransactionType == TransactionTypeEnum.Transfer)
				.OrderBy(s => s.DateAndTime))
			{
				if (sourceTransactions.FirstOrDefault(s => s.TransactionID == transaction.TransactionID) != null)
					continue; // Already processed it

				// Find the source and receipt sides
				Transaction sourceTransaction = GetSourceTransaction(transaction, transactionList);
				if (sourceTransaction == null)
					throw new Exception("Could not match source transaction for transfer " + transaction.TransactionID);
				if (sourceTransaction.AmountPaid == 0.0m) 
					throw new Exception("Found incorrect source transaction for transer " + transaction.TransactionID);
				Transaction receiveTransaction = GetReceiveTransaction(transaction, transactionList);
				if (receiveTransaction == null)
					throw new Exception("Could not identify receive transaction for transfer " + transaction.TransactionID);
				if (receiveTransaction.AmountReceived == 0.0m)
					throw new Exception("Found incorrect receive transaction for transfer " + transaction.TransactionID);
					
				if (receiveTransaction.AmountReceived != sourceTransaction.AmountPaid)
				{
					decimal amountDifference = sourceTransaction.AmountPaid - Utils.ConvertWeight(receiveTransaction.AmountReceived,
						                           receiveTransaction.WeightUnit, sourceTransaction.WeightUnit);
					// Create a metal storage fee to account for the difference
					Transaction storageFee = new Transaction(sourceTransaction.Service, sourceTransaction.Account,
						sourceTransaction.DateAndTime, sourceTransaction.TransactionID, TransactionTypeEnum.StorageFeeInMetal,
						sourceTransaction.Vault, amountDifference, sourceTransaction.CurrencyUnit, 0.0m, 
						sourceTransaction.WeightUnit, sourceTransaction.MetalType, 
						"Transfer fee in metal from " + sourceTransaction.Memo);
					transactionList.Add(storageFee);
				}

				// Set the source vault property in the receipt side
				receiveTransaction.MakeTransfer(sourceTransaction.Account, sourceTransaction.Vault);
				sourceTransactions.Add(sourceTransaction);
			}
			foreach (Transaction sourceTransaction in sourceTransactions)
			{
				// Throw away the source side
				transactionList.Remove(sourceTransaction);
			}
			return transactionList;
		}

		private Transaction GetSourceTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			return transactionList.Where(
				s => s.TransactionID == transaction.TransactionID
				&& s.Service == transaction.Service
				&& s.AmountPaid > 0.0m
				&& !s.Memo.Contains("Fee")).FirstOrDefault();
		}

		private Transaction GetReceiveTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			return transactionList.Where(
				s => s.TransactionID == transaction.TransactionID
				&& s.Service == transaction.Service
				&& s.AmountReceived > 0.0m
				&& !s.Memo.Contains("Fee")).FirstOrDefault();
		}
						
		// All that happens in a transfer is the vault changes
		private void ProcessTransfer(Transaction transaction)
		{
			// Find the open lots in the current vault with the correct metal type
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.MetalType == transaction.MetalType 
				&& s.Vault == transaction.TransferFromVault 
				&& s.Account == transaction.TransferFromAccount
				&& s.CurrentWeight(transaction.WeightUnit) > 0)
				.OrderBy(s => s.PurchaseDate).ToList();
			AmountInMetal amount = new AmountInMetal(transaction.DateAndTime, transaction.TransactionID, 
				                       transaction.Vault, transaction.AmountReceived, transaction.WeightUnit, 
				                       transaction.MetalType);
			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentWeight(amount.WeightUnit) >= amount.Amount)
				{
					// Split lot and transfer part of it
					amount.Decrease(amount.Amount, amount.WeightUnit);
					Lot newLot = new Lot(lot.Service, lot.LotID, lot.PurchaseDate, lot.OriginalWeight, amount.WeightUnit, 
						lot.OriginalPrice, lot.MetalType, transaction.Vault, transaction.Account);
					lots.Add(newLot);
					break;
				}
				else
				{
					// Just reassign entire lot to other vault
					lot.Vault = transaction.Vault;
					amount.Decrease(lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit); // some transfer remaining
				}
			}
			if (amount.Amount > 0.0m)
				throw new Exception("Requested transfer exceeds available lots");
		}

		private void ProcessSale(Transaction transaction)
		{
			decimal originalWeightToSellInGrams = Utils.ConvertWeight(transaction.AmountPaid, transaction.WeightUnit,
				MetalWeightEnum.Gram);
			MetalAmount remainingAmountToSell = new MetalAmount(transaction.AmountPaid, transaction.MetalType, transaction.WeightUnit);
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.MetalType == transaction.MetalType
				&& s.Account == transaction.Account
				&& s.Vault == transaction.Vault
				&& s.CurrentWeight(transaction.WeightUnit) > 0.0m)
				.OrderBy(s => s.PurchaseDate).ToList();
			foreach(Lot lot in availableLots)
			{
				if (remainingAmountToSell.Weight == 0.0m)
				{
					break;
				}
				MetalAmount amountToSell = lot.GetAmountToSell(remainingAmountToSell);
				decimal percentageOfSale = Utils.ConvertWeight(amountToSell.Weight, amountToSell.WeightUnit, MetalWeightEnum.Gram) /
					originalWeightToSellInGrams;
				ValueInCurrency valuePaidForThisAmount = new ValueInCurrency(percentageOfSale * transaction.AmountReceived,
					                                         transaction.CurrencyUnit, transaction.DateAndTime);
				remainingAmountToSell = remainingAmountToSell - amountToSell;
				sales.Add(lot.Sell(amountToSell, valuePaidForThisAmount));
			}
			if (remainingAmountToSell.Weight > 0.0m)
				throw new Exception("Cannot sell more metal than is available");
		}

		private void PurchaseNewLot(Transaction transaction)
		{
			Lot newLot = new Lot(transaction.Service, transaction.TransactionID, transaction.DateAndTime, transaction.AmountReceived, 
				transaction.WeightUnit, 
				new ValueInCurrency(transaction.AmountPaid, transaction.CurrencyUnit, transaction.DateAndTime),
				transaction.MetalType, transaction.Vault, transaction.Account);
			lots.Add(newLot);
		}

		private void ApplyStorageFeeInMetal(Transaction transaction)
		{
			if (transaction.TransactionType != TransactionTypeEnum.StorageFeeInMetal)
				throw new Exception("Wrong transaction type " + transaction.TransactionType + " passed to ApplyStorageFeeInMetal");

			AmountInMetal fee = new AmountInMetal(transaction.DateAndTime, transaction.TransactionID, 
				                            transaction.Vault, transaction.AmountPaid, transaction.WeightUnit, 
											transaction.MetalType);
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.MetalType == fee.MetalType 
				&& s.Account == transaction.Account
				&& s.CurrentWeight(fee.WeightUnit) > 0)
				.OrderBy(s => s.PurchaseDate).ToList();
			if (transaction.Vault.ToLower() != "any")
				availableLots = availableLots.Where(s => s.Vault == transaction.Vault).ToList();

			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentWeight(fee.WeightUnit) >= fee.Amount)
				{
					lot.DecreaseWeight(fee.Amount, fee.WeightUnit);
					fee.Decrease(fee.Amount, fee.WeightUnit); // fee paid
					break;
				}
				else
				{
					fee.Decrease(lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit); // some fee remaining
					lot.DecreaseWeight(lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit); // lot expended
				}
			}
			if (fee.Amount > 0.0m)
				throw new Exception("Storage fee exceeds available funds");
		}

		private void ApplyStorageFeeInCurrency(Transaction transaction)
		{
			if (transaction.TransactionType != TransactionTypeEnum.StorageFeeInCurrency)
				throw new Exception("Wrong transaction type " + transaction.TransactionType + " passed to ApplyStorageFeeInCurrency");
				
			List<Lot> availableLots = lots.Where(
				                          s => s.Service == transaction.Service
				                          && s.MetalType == transaction.MetalType
				                          && s.Account == transaction.Account
				                          && s.CurrentWeight(transaction.WeightUnit) > 0)
				.OrderBy(s => s.PurchaseDate).ToList();
			if (transaction.Vault.ToLower() != "any")
				availableLots = availableLots.Where(s => s.Vault == transaction.Vault).ToList();

			// Apply fees to the first available lot
			if (availableLots.Count == 0)
				throw new Exception("No available lots to allocate storage fee to");

			availableLots[0].ApplyFeeInCurrency(new ValueInCurrency(transaction.AmountPaid, transaction.CurrencyUnit, transaction.DateAndTime));
		}
	}
}

