using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalAccounting
{
	public class MetalStorageService
	{
		private List<Lot> lots;
		private List<TaxableSale> sales;
		private string name;

		public string Name
		{
			get
			{
				return name;
			}
		}

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

		public MetalStorageService(string name)
		{
			lots = new List<Lot>();
			sales = new List<TaxableSale>();
			this.name = name;
		}

		public void ApplyTransactions(List<Transaction> transactionList)
		{
			transactionList = FormTransfers(transactionList);
			foreach (Transaction transaction in transactionList.OrderBy(s => s.DateAndTime))
			{
				switch (transaction.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						Console.WriteLine(string.Format("{0} received {1:0.000} {2}s {3} to account {4}", 
							transaction.DateAndTime.ToShortDateString(), transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(),
							transaction.Account));
						PurchaseNewLot(transaction);
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
						Console.WriteLine(string.Format("{0} sold {1:0.000} {2}s {3} from account {4}", 
							transaction.DateAndTime.ToShortDateString(), transaction.AmountPaid, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.Account));
						ProcessSale(transaction);
						break;
					case TransactionTypeEnum.Transfer:
						Console.WriteLine(string.Format("{0} transferred {1:0.000} {2}s {3} from account {4} vault {5} to account {6} vault {7}",
							transaction.DateAndTime.ToShortDateString(), transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.TransferFromAccount, transaction.TransferFromVault, transaction.Account, transaction.Vault));
						ProcessTransfer(transaction);
							break;
					case TransactionTypeEnum.StorageFeeInMetal:
						ApplyStorageFeeInMetal(transaction);
						break;
					case TransactionTypeEnum.StorageFeeInCurrency:
						throw new NotImplementedException("Storage fees in currency not implemented");
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
				receiveTransaction.TransferFromVault = sourceTransaction.Vault;
				receiveTransaction.TransferFromAccount = sourceTransaction.Account;
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
				&& s.AmountPaid > 0.0m
				&& !s.Memo.Contains("Fee")).FirstOrDefault();
		}

		private Transaction GetReceiveTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			return transactionList.Where(
				s => s.TransactionID == transaction.TransactionID
				&& s.AmountReceived > 0.0m
				&& !s.Memo.Contains("Fee")).FirstOrDefault();
		}
			
		// All that happens in a transfer is the vault changes
		private void ProcessTransfer(Transaction transaction)
		{
			// Find the open lots in the current vault with the correct metal type
			List<Lot> availableLots = lots.Where(
				s => s.MetalType == transaction.MetalType 
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
					Lot newLot = new Lot(lot.LotID, lot.PurchaseDate, lot.OriginalWeight, amount.WeightUnit, 
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
				s => s.MetalType == transaction.MetalType
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
			Lot newLot = new Lot(transaction.TransactionID, transaction.DateAndTime, transaction.AmountReceived, 
				transaction.WeightUnit, 
				new ValueInCurrency(transaction.AmountPaid, transaction.CurrencyUnit, transaction.DateAndTime),
				transaction.MetalType, transaction.Vault, transaction.Account);
			lots.Add(newLot);
		}

		private void ApplyStorageFeeInMetal(Transaction transaction)
		{
			AmountInMetal fee = new AmountInMetal(transaction.DateAndTime, transaction.TransactionID, 
				                            transaction.Vault, transaction.AmountPaid, transaction.WeightUnit, 
											transaction.MetalType);
			List<Lot> availableLots = lots.Where(
				s => s.MetalType == fee.MetalType 
				&& s.Account == transaction.Account
				&& s.Vault == transaction.Vault 
				&& s.CurrentWeight(fee.WeightUnit) > 0)
				.OrderBy(s => s.PurchaseDate).ToList();
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
	}
}

