using System;

namespace MetalAccounting
{
	public class Transaction
	{
		public string Service { get; set; }
		public string Account { get; set; }
		public DateTime DateAndTime { get; set; }
		public string TransactionID { get; set; }
		public TransactionTypeEnum TransactionType { get; set; }
		public string Vault { get; set; }
		public decimal AmountPaid { get; set; }
		public decimal AmountReceived { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }
		public string Memo { get; set; }
		public CurrencyUnitEnum CurrencyUnit { get; set; }
		public MetalTypeEnum MetalType { get; set; }
		public string TransferFromVault { get; set; }
		public string TransferFromAccount { get; set; }

		public Transaction(string service, string account, DateTime dateAndTime, string transactionID, 
			TransactionTypeEnum transactionType, string vault, decimal amountPaid, CurrencyUnitEnum currencyUnit, 
			decimal amountReceived, MetalWeightEnum weightUnit, MetalTypeEnum metalType, string memo)
		{
			this.Service = service;
			this.Account = account;
			this.DateAndTime = dateAndTime;
			this.TransactionID = transactionID;
			this.TransactionType = transactionType;
			this.Vault = vault;
			this.AmountPaid = amountPaid;
			this.CurrencyUnit = currencyUnit;
			this.AmountReceived = amountReceived;
			this.WeightUnit = weightUnit;
			this.MetalType = metalType;
			this.Memo = memo;
		}

		public decimal Weight
		{
			get
			{
				switch (this.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						return AmountReceived;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
					case TransactionTypeEnum.StorageFeeInMetal:
						return AmountPaid;
					default:
						return 0.0m;
				}
			}

			set
			{
				switch (this.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						AmountReceived = value;
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
					case TransactionTypeEnum.StorageFeeInMetal:
						AmountPaid = value;
						break;
				}
			}
		}

		public decimal GetWeightInUnits(MetalWeightEnum toWeightUnit)
		{
			return Utils.ConvertWeight(Weight, WeightUnit, toWeightUnit);
		}

		public Transaction Duplicate()
		{
			return new Transaction(Service, Account, DateAndTime, TransactionID, TransactionType,
				Vault, AmountPaid, CurrencyUnit, AmountReceived, WeightUnit, MetalType, Memo);
		}

		public void MakeTransfer(string account, string vault)
		{
			this.TransactionType = TransactionTypeEnum.Transfer;
			this.TransferFromAccount = account;
			this.TransferFromVault = vault;
		}

		// Returns TransactionTypeEnum.Indeterminate if the type has no opposite
		public TransactionTypeEnum GetOppositeTransactionType()
		{
			switch (TransactionType)
			{
				case TransactionTypeEnum.Purchase:
					return TransactionTypeEnum.Sale;
				case TransactionTypeEnum.Sale:
					return TransactionTypeEnum.Purchase;
				case TransactionTypeEnum.PurchaseViaExchange:
					return TransactionTypeEnum.SaleViaExchange;
				case TransactionTypeEnum.SaleViaExchange:
					return TransactionTypeEnum.PurchaseViaExchange;
				default:
					return TransactionTypeEnum.Indeterminate;
			}
		}
	}
}

