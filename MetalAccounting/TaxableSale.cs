using System;

namespace MetalAccounting
{
	public class TaxableSale
	{
		public string LotID { get; set; }
		public DateTime PurchaseDate { get; set; }
		public DateTime SaleDate { get; set; }
		public ValueInCurrency AdjustedBasis { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }
		public MetalTypeEnum MetalType { get; set; }
		public decimal SaleWeight { get; set; }
		public ValueInCurrency SalePrice { get; set; }

		public TaxableSale(Lot fromLot, MetalAmount amount, ValueInCurrency salePrice)
		{
			this.LotID = fromLot.LotID;
			this.PurchaseDate = fromLot.PurchaseDate;
			this.WeightUnit = fromLot.WeightUnit;
			decimal saleWeight = Utils.ConvertWeight(amount.Weight, amount.WeightUnit, this.WeightUnit);
			decimal percentageOfLot = saleWeight / fromLot.CurrentWeight(fromLot.WeightUnit);
			this.AdjustedBasis = new ValueInCurrency(percentageOfLot * fromLot.AdjustedPrice.Value,
				fromLot.AdjustedPrice.Currency, fromLot.AdjustedPrice.Date);
			this.MetalType = fromLot.MetalType;
			this.SaleWeight = Utils.ConvertWeight(amount.Weight, amount.WeightUnit, this.WeightUnit);
			this.SalePrice = new ValueInCurrency(salePrice);
			this.SaleDate = salePrice.Date;
		}
	}
}

