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
		public ValueInCurrency AdjustedPrice
		{ 
			get
			{
				return adjustedPrice;
			}
		}
		public MetalWeightEnum WeightUnit { get; set; }
		public MetalTypeEnum MetalType { get; set; }	
		public string ItemType { get; set; }
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
		private ValueInCurrency adjustedPrice;

		public Lot(string service, string transactionID, DateTime purchaseDate, decimal originalWeight, MetalWeightEnum weightUnit, 
			ValueInCurrency price, MetalTypeEnum metalType, string vault, string account, string itemType)
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
			this.adjustedPrice = new ValueInCurrency(OriginalPrice);
			this.vault = vault;
			this.account = account;
			this.ItemType = itemType;
			history.Add(string.Format("Opened lot {0} bought {1} {2} {3} ({4}) for {5} {6}, vault{7}, account {8}", 
				purchaseDate.ToShortDateString(), originalWeight, weightUnit, metalType, itemType, price.Value, 
				price.Currency, vault, account));
		}

		// Get the current weight expressed in weight units
		public decimal CurrentWeight(MetalWeightEnum toWeightUnit)
		{
			return Utils.ConvertWeight(currentWeight, this.WeightUnit, toWeightUnit);
		}

		public bool IsDepleted()
        {
			return currentWeight == 0.0m; // Current weight is 0 (in native units)
        }

		public void ApplyFeeInCurrency(ValueInCurrency fee)
		{
			this.AdjustedPrice.Value += Utils.ConvertCurrency(fee.Value, fee.Currency, this.AdjustedPrice.Currency);
			history.Add(fee.Date.ToShortDateString() + " Applied fee " + fee.Value + " " + fee.Currency);
		}

		public void DecreaseWeightViaFee(DateTime transactionDateTime, decimal weightAmount, MetalWeightEnum fromWeightUnit)
        {
			this.DecreaseWeight(weightAmount, fromWeightUnit);
			history.Add(string.Format("{0} decreased weight by {1:0.0000000} {2} as fee", transactionDateTime.Date.ToShortDateString(),
				weightAmount, WeightUnit));
		}

		public void DecreaseWeightViaTransfer(DateTime transactionDateTime, decimal weightAmount, MetalWeightEnum fromWeightUnit,
			string account, string vault)
		{
			this.DecreaseWeight(weightAmount, fromWeightUnit);
			history.Add(string.Format("{0} transferred {1:0.0000000} {2} to account {3}, vault {4}", transactionDateTime.Date.ToShortDateString(),
				weightAmount, WeightUnit, account, vault));
		}

		private void DecreaseWeight(decimal weightAmount, MetalWeightEnum fromWeightUnit)
		{
			decimal newCurrentWeight = currentWeight - Utils.ConvertWeight(weightAmount, fromWeightUnit, this.WeightUnit);
			if (newCurrentWeight < 0.0m)
				throw new Exception("Cannot decrease lot weight by more than its current weight");
			currentWeight = newCurrentWeight;
		}

		private void IncreaseWeight(decimal weightAmount, MetalWeightEnum fromWeightUnit)
		{
			currentWeight += Utils.ConvertWeight(weightAmount, fromWeightUnit, this.WeightUnit);
			history.Add(string.Format("Increased weight by {0:0.0000000} {1}", weightAmount, WeightUnit));
		}			

		public TaxableSale Sell(MetalAmount amount, ValueInCurrency salePrice)
		{
			if (this.MetalType != amount.MetalType)
				throw new Exception("Metal types in Sell() do not match: lot type " + this.MetalType + ", sale type " + amount.MetalType);

			if (this.ItemType != amount.ItemType)
				throw new Exception("Item types in Sell() do not match: lot type " + this.ItemType + ", sale type " + amount.ItemType);
			
			decimal weightToSell = Utils.ConvertWeight(amount.Weight, amount.WeightUnit, this.WeightUnit);
			decimal percentOfLotToSell = weightToSell / currentWeight;

			decimal newCurrentWeight = currentWeight - weightToSell;
			if (newCurrentWeight < 0.0m)
				throw new Exception("Cannot sell more than the lot's current weight");

			TaxableSale taxableSale = new TaxableSale(this, amount, salePrice);
			currentWeight = newCurrentWeight;
			AdjustedPrice.Value = AdjustedPrice.Value * (1.0m - percentOfLotToSell);
			history.Add(string.Format("{0} sold {1} {2} {3} for {4} {5:0.00}", 
				salePrice.Date.ToShortDateString(), amount.Weight, amount.WeightUnit, amount.MetalType,
				salePrice.Currency, salePrice.Value));
			return taxableSale;
		}

		public MetalAmount GetAmountToSell(MetalAmount desiredAmount)
		{
			if (this.MetalType != desiredAmount.MetalType)
				throw new Exception("Metal types in GetAmountToSell() do not match: lot type " + this.MetalType + ", sale type " + desiredAmount.MetalType);

			if (this.ItemType != desiredAmount.ItemType)
				throw new Exception("Item types in GetAmountToSell() do not match: lot type " + this.ItemType + ", sale type " + desiredAmount.ItemType);

			decimal desiredWeight = Utils.ConvertWeight(desiredAmount.Weight, desiredAmount.WeightUnit, this.WeightUnit);
			return new MetalAmount(currentWeight > desiredWeight ? desiredWeight : currentWeight, this.MetalType, this.WeightUnit, this.ItemType);
		}
	}
}

