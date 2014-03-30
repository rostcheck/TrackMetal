using System;

namespace MetalAccounting
{
	public class ValueInCurrency
	{
		public decimal Value { get; set; }
		public CurrencyUnitEnum Currency { get; set; }
		public DateTime Date { get; set; }

		public ValueInCurrency(decimal value, CurrencyUnitEnum currency, DateTime date)
		{
			this.Value = value;
			this.Currency = currency;
			this.Date = date;
		}

		public ValueInCurrency(ValueInCurrency otherPrice)
		{
			this.Value = otherPrice.Value;
			this.Currency = otherPrice.Currency;
			this.Date = otherPrice.Date;
		}
	}
}

