using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
					Console.WriteLine(string.Format("{0} {1} received {2:0.000000} {3}s {4} ({5}) to account {6} vault {7}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(),
							transaction.ItemType, transaction.Account, transaction.Vault));
						PurchaseNewLot(transaction);
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
					Console.WriteLine(string.Format("{0} {1} sold {2:0.000000} {3}s {4} ({5}) from account {6} vault {7}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountPaid, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.ItemType, transaction.Account, transaction.Vault));
						ProcessSale(transaction);
						break;
					case TransactionTypeEnum.Transfer:
					case TransactionTypeEnum.TransferIn:
						Console.WriteLine(string.Format("{0} {1} transferred {2:0.000000} {3}s {4} ({5}) from account {6}, vault {7} to account {8}, vault {9}",
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.WeightUnit.ToString().ToLower(), transaction.MetalType.ToString().ToLower(), 
							transaction.ItemType, 
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

		public void DumpTransactions(string filename, List<Transaction> transactions)
		{
			StreamWriter sw = new StreamWriter(filename);
			sw.WriteLine("Date\tService\tType\tMetal\tWeight\tUnit\t\tItemTypeAccount\tAmountPaid\tAmountReceived\tCurrency\tVault\tTransactionId\tMemo");
			string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}";

			foreach (var transaction in transactions.OrderBy(s => s.DateAndTime).ToList())
			{
				
				string formatted = string.Format(formatString, transaction.DateAndTime, transaction.Service, 
					transaction.TransactionType.ToString(), 
					transaction.MetalType.ToString().ToLower(),
					transaction.Weight, transaction.WeightUnit, transaction.ItemType,
					transaction.Account, transaction.AmountPaid, transaction.AmountReceived, 
					transaction.CurrencyUnit, transaction.Vault, transaction.TransactionID, transaction.Memo);
				sw.WriteLine(formatted);
			}
			sw.Close();
		}

        // For transfers, combine both sides into one transaction
        private List<Transaction> FormTransfers(List<Transaction> transactionList)
        {
            List<Transaction> sourceTransactions = new List<Transaction>();
            // Handle transfers in some bullion accounts that only report "transfer"
            var transferTransactionList = transactionList.Where(
                s => s.TransactionType == TransactionTypeEnum.Transfer).ToList();
            transferTransactionList.AddRange(transactionList.Where(
                s => s.TransactionType == TransactionTypeEnum.TransferIn));
            foreach (Transaction transaction in transferTransactionList.OrderBy(s => s.DateAndTime))
            {
                if (sourceTransactions.FirstOrDefault(s => s.TransactionID == transaction.TransactionID) != null)
                    continue; // Already processed it

                // Find the source and receipt sides
                Transaction sourceTransaction = GetSourceTransaction(transaction, transactionList);
                if (sourceTransaction == null)
                    throw new Exception("Could not match source transaction for transfer " + transaction.TransactionID);
                if (sourceTransaction.AmountPaid == 0.0m && sourceTransaction.TransactionType != TransactionTypeEnum.TransferOut)
                    throw new Exception("Found incorrect source transaction for transfer " + transaction.TransactionID);
                Transaction receiveTransaction = GetReceiveTransaction(transaction, transactionList);
                if (receiveTransaction == null)
                    throw new Exception("Could not identify receive transaction for transfer " + transaction.TransactionID);
                if (receiveTransaction.AmountReceived == 0.0m && sourceTransaction.TransactionType != TransactionTypeEnum.TransferIn)
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
						"Transfer fee in metal from " + sourceTransaction.Memo, transaction.ItemType);
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
			Transaction sourceTransaction = null;
			if (transaction.TransactionType == TransactionTypeEnum.TransferOut)
				return transaction; // This is the source
			else
            {
				// Explicit source
				sourceTransaction = transactionList.Where(
					s => s.TransactionType == TransactionTypeEnum.TransferOut
					&& s.TransactionID == transaction.TransactionID
					&& s.Service == transaction.Service
					&& s.ItemType == transaction.ItemType).FirstOrDefault();
				if (sourceTransaction == null)
					// Inferred source
					sourceTransaction = transactionList.Where(
						s => s.TransactionID == transaction.TransactionID
						&& s.Service == transaction.Service
						&& s.ItemType == transaction.ItemType
						&& s.AmountPaid > 0.0m
						&& !s.Memo.Contains("Fee")).FirstOrDefault();
			}
			return sourceTransaction;
		}

		private Transaction GetReceiveTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			Transaction receiveTransaction = null;
			if (transaction.TransactionType == TransactionTypeEnum.TransferIn)
				return transaction; // This is the source
			else
			{
				// Explicit destination
				receiveTransaction = transactionList.Where(
					s => s.TransactionType == TransactionTypeEnum.TransferIn
					&& s.TransactionID == transaction.TransactionID
					&& s.Service == transaction.Service
					&& s.ItemType == transaction.ItemType).FirstOrDefault();
				if (receiveTransaction == null)
					// Inferred destination
					receiveTransaction = transactionList.Where(
						s => s.TransactionID == transaction.TransactionID
						&& s.Service == transaction.Service
						&& s.ItemType == transaction.ItemType
						&& s.AmountPaid > 0.0m
						&& !s.Memo.Contains("Fee")).FirstOrDefault();
			}
			return receiveTransaction;
		}
						
		// All that happens in a transfer is the vault changes
		private void ProcessTransfer(Transaction transaction)
		{
			// Find the open lots in the current vault with the correct metal type
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.MetalType == transaction.MetalType 
				&& s.ItemType == transaction.ItemType
				&& s.Vault == transaction.TransferFromVault
				&& s.Account == transaction.TransferFromAccount
				&& s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			AmountInMetal amount = new AmountInMetal(transaction.DateAndTime, transaction.TransactionID, 
				                       transaction.Vault, transaction.AmountReceived, transaction.WeightUnit, 
				                       transaction.MetalType);
			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentWeight(amount.WeightUnit) >= amount.Amount)
				{
					// Split lot and transfer part of it
					Lot newLot = new Lot(lot.Service, lot.LotID + "-split", lot.PurchaseDate, amount.Amount, amount.WeightUnit, 
						lot.OriginalPrice, lot.MetalType, transaction.Vault, transaction.Account, transaction.ItemType);
					lots.Add(newLot);
					// Remove from original lot
					lot.DecreaseWeightViaTransfer(transaction.DateAndTime, amount.Amount, amount.WeightUnit,
						transaction.TransferFromAccount, transaction.TransferFromVault);
					amount.Decrease(amount.Amount, amount.WeightUnit);
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
			var targetWeightUnit = transaction.WeightUnit;
			if (targetWeightUnit != MetalWeightEnum.CryptoCoin)
				targetWeightUnit = MetalWeightEnum.Gram; // Convert metals to grams
			decimal originalWeightToSell = Utils.ConvertWeight(transaction.AmountPaid, transaction.WeightUnit, targetWeightUnit);
			MetalAmount remainingAmountToSell = new MetalAmount(transaction.AmountPaid, transaction.MetalType, transaction.WeightUnit, transaction.ItemType);
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.MetalType == transaction.MetalType
				&& s.ItemType == transaction.ItemType
				&& s.Account == transaction.Account
				//&& s.Vault == transaction.Vault // where it is doesn't affect lot accounting
				&& s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			
			foreach(Lot lot in availableLots)
			{
				if (remainingAmountToSell.Weight == 0.0m)
				{
					break;
				}
				MetalAmount amountToSell = lot.GetAmountToSell(remainingAmountToSell);
				decimal percentageOfSale = Utils.ConvertWeight(amountToSell.Weight, amountToSell.WeightUnit, targetWeightUnit) /
					originalWeightToSell;
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
				transaction.MetalType, transaction.Vault, transaction.Account, transaction.ItemType);
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
				&& s.ItemType == transaction.ItemType
				&& s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			if (transaction.Vault.ToLower() != "any")
				availableLots = availableLots.Where(s => s.Vault == transaction.Vault).ToList();

			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentWeight(fee.WeightUnit) >= fee.Amount)
				{
					lot.DecreaseWeightViaFee(transaction.DateAndTime, fee.Amount, fee.WeightUnit);
					fee.Decrease(fee.Amount, fee.WeightUnit); // fee paid
					break;
				}
				else
				{
					fee.Decrease(lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit); // some fee remaining
					lot.DecreaseWeightViaFee(transaction.DateAndTime, lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit); // lot expended
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
										  && s.ItemType == transaction.ItemType
				                          && s.IsDepleted() == false)
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

