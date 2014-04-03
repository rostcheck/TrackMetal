using System;
using System.Collections.Generic;

namespace MetalAccounting
{
	public class Lot
	{
		public string LotID { get; set; }
		public DateTime PurchaseDate { get; set; }
		public decimal OriginalWeight { get; set; }
		public ValueInCurrency OriginalPrice { get; set; }
		public ValueInCurrency AdjustedPrice { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }
		public MetalTypeEnum MetalType { get; set; }	
		public string Vault 
		{ 
			get
			{
				return vault;
			}
			set
			{
				vault = value;
				history.Add("Set vault to " + value);
			}
		}

		public List<string> History 
		{ 
			get
			{
				return history;
			}
		}

		public string Account
		{
			get
			{
				return account;
			}
			set
			{
				account = value;
				history.Add("Set account to " + value);
			}
		}

		public string Service
		{
			get
			{
				return service;
			}
		}

		private decimal currentWeight;
		private List<string> history;
		private string vault;
		private string account;
		private string service;

		public Lot(string service, string transactionID, DateTime purchaseDate, decimal originalWeight, MetalWeightEnum weightUnit, 
			ValueInCurrency price, MetalTypeEnum metalType, string vault, string account)
		{
			history = new List<string>();
			this.service = service;
			this.LotID = transactionID;
			this.PurchaseDate = purchaseDate;
			this.OriginalWeight = originalWeight;
			this.currentWeight = originalWeight;
			this.WeightUnit = weightUnit;
			this.OriginalPrice = price;
			this.MetalType = metalType;
			this.AdjustedPrice = new ValueInCurrency(OriginalPrice);
			this.vault = vault;
			this.account = account;
			history.Add(string.Format("Opened lot {0} bought {1} {2} {3} for {4} {5}, vault {6}, account {7}", 
				purchaseDate.ToShortDateString(), originalWeight, weightUnit, metalType, price.Value, 
				price.Currency, vault, account));
		}

		// Get the current weight expressed in weight units
		public decimal CurrentWeight(MetalWeightEnum toWeightUnit)
		{
			return Utils.ConvertWeight(currentWeight, this.WeightUnit, toWeightUnit);
		}

		public void ApplyFeeInCurrency(ValueInCurrency fee)
		{
			this.AdjustedPrice.Value += Utils.ConvertCurrency(fee.Value, fee.Currency, this.AdjustedPrice.Currency);
			history.Add(fee.Date.ToShortDateString() + " Applied fee " + fee.Value + " " + fee.Currency);
		}

		public void DecreaseWeight(decimal weightAmount, MetalWeightEnum fromWeightUnit)
		{
			decimal newCurrentWeight = currentWeight - Utils.ConvertWeight(weightAmount, fromWeightUnit, this.WeightUnit);
			if (newCurrentWeight < 0.0m)
				throw new Exception("Cannot decrease lot weight by more than its current weight");
			currentWeight = newCurrentWeight;
			history.Add("Decreased weight by " + weightAmount + " " + WeightUnit);
		}

		public void IncreaseWeight(decimal weightAmount, MetalWeightEnum fromWeightUnit)
		{
			currentWeight += Utils.ConvertWeight(weightAmount, fromWeightUnit, this.WeightUnit);
			history.Add("Increased weight by " + weightAmount + " " + WeightUnit);
		}			

		public TaxableSale Sell(MetalAmount amount, ValueInCurrency salePrice)
		{
			if (this.MetalType != amount.MetalType)
				throw new Exception("Metal types in Sell() do not match: lot type " + this.MetalType + ", sale type " + amount.MetalType);

			decimal weightToSell = Utils.ConvertWeight(amount.Weight, amount.WeightUnit, this.WeightUnit);
			decimal percentOfLotToSell = weightToSell / currentWeight;

			decimal newCurrentWeight = currentWeight - weightToSell;
			if (newCurrentWeight < 0.0m)
				throw new Exception("Cannot sell more than the lot's current weight");

			TaxableSale taxableSale = new TaxableSale(this, amount, salePrice);
			currentWeight = newCurrentWeight;
			AdjustedPrice.Value = AdjustedPrice.Value * (1.0m - percentOfLotToSell);
			history.Add(string.Format("{0} sold {1} {2} {3} for {4} {5}", 
				salePrice.Date.ToShortDateString(), amount.Weight, amount.WeightUnit, amount.MetalType,
				salePrice.Currency, salePrice.Value));
			return taxableSale;
		}

		public MetalAmount GetAmountToSell(MetalAmount desiredAmount)
		{
			if (this.MetalType != desiredAmount.MetalType)
				throw new Exception("Metal types in GetAmountToSell() do not match: lot type " + this.MetalType + ", sale type " + desiredAmount.MetalType);

			decimal desiredWeight = Utils.ConvertWeight(desiredAmount.Weight, desiredAmount.WeightUnit, this.WeightUnit);
			return new MetalAmount(currentWeight > desiredWeight ? desiredWeight : currentWeight, this.MetalType, this.WeightUnit);
		}
	}
}

