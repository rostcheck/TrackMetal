using System;

namespace MetalAccounting
{
	public static class Utils
	{
		const decimal GramsPerTroyOz = 31.1034768m;

		public static decimal ConvertCurrency(decimal amount, CurrencyUnitEnum fromCurrency, CurrencyUnitEnum toCurrency)
		{
			if (fromCurrency == toCurrency)
				return amount;
			else
				throw new NotImplementedException("Currency conversion not implemented");
		}

		public static decimal ConvertWeight(decimal weight, MetalWeightEnum fromUnits, MetalWeightEnum toUnits)
		{
			if (fromUnits == toUnits)			
				return weight;
			else			
				return ConvertFromGrams(ConvertToGrams(weight, fromUnits), toUnits); 
		}
			
		private static decimal ConvertToGrams(decimal weight, MetalWeightEnum fromUnits)
		{
			switch (fromUnits)
			{
				case MetalWeightEnum.Gram:
					return weight;
				case MetalWeightEnum.TroyOz:
					return weight * GramsPerTroyOz;
				default:
					throw new Exception("Unknown weight unit " + fromUnits);
			}
		}

		private static decimal ConvertFromGrams(decimal weightInGrams, MetalWeightEnum toUnits)
		{
			switch (toUnits)
			{
				case MetalWeightEnum.Gram:
					return weightInGrams;
				case MetalWeightEnum.TroyOz:
					return weightInGrams / GramsPerTroyOz;
				default:
					throw new Exception("Unknown weight unit " + toUnits);
			}
		}
	}
}

