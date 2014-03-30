using System;

namespace MetalAccounting
{
	public class MetalAmount
	{
		public decimal Weight { get; set; }
		public MetalTypeEnum MetalType { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }

		public MetalAmount(decimal weight, MetalTypeEnum metalType, MetalWeightEnum weightUnit)
		{
			this.Weight = weight;
			this.MetalType = metalType;
			this.WeightUnit = weightUnit;
		}

		public static MetalAmount operator -(MetalAmount amount1, MetalAmount amount2)
		{
			decimal weightToSubtract = Utils.ConvertWeight(amount2.Weight, amount2.WeightUnit, amount1.WeightUnit);
			return new MetalAmount(amount1.Weight - weightToSubtract, amount1.MetalType, amount1.WeightUnit);
		}
	}
}

