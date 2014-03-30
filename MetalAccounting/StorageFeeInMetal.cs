using System;

namespace MetalAccounting
{
	public class AmountInMetal
	{
		public string Vault { get; set; }
		public decimal Amount { get; set; }
		public MetalTypeEnum MetalType { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }
		public string TransactionID { get; set; }
		public DateTime Date { get; set; }

		public AmountInMetal(DateTime transationDate, string transactionID, string vault, decimal amount, 
			MetalWeightEnum weightUnit, MetalTypeEnum metalType)
		{
			this.Date = transationDate;
			this.TransactionID = transactionID;
			this.Vault = vault;
			this.Amount = amount;
			this.WeightUnit = weightUnit;
			this.MetalType = metalType;
		}

		public void Decrease(decimal amount, MetalWeightEnum fromWeightUnit)
		{
			this.Amount -= Utils.ConvertWeight(amount, fromWeightUnit, this.WeightUnit);
			if (this.Amount < 0.0m)
				throw new Exception("Cannot decrease storage fee less than 0");
		}
	}
}

