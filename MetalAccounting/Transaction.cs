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
	}
}

