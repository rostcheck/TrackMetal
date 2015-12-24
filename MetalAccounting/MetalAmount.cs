using System;

namespace MetalAccounting
{
	public class MetalAmount
	{
		public decimal Weight { get; set; }
		public MetalTypeEnum MetalType { get; set; }
		public string ItemType { get; set; }
		public MetalWeightEnum WeightUnit { get; set; }

		public MetalAmount(decimal weight, MetalTypeEnum metalType, MetalWeightEnum weightUnit, string itemType)
		{
			this.Weight = weight;
			this.MetalType = metalType;
			this.WeightUnit = weightUnit;
			this.ItemType = itemType;
		}

		public static MetalAmount operator -(MetalAmount amount1, MetalAmount amount2)
		{
			if (amount1.MetalType != amount2.MetalType)
				throw new Exception(string.Format("Cannot subtract different metal types: {0} and {1}", amount1.MetalType, amount2.MetalType));

			if (amount1.ItemType != amount2.ItemType)
				throw new Exception(string.Format("Cannot subtract different item types: {0} and {1}", amount1.ItemType, amount2.ItemType));

			decimal weightToSubtract = Utils.ConvertWeight(amount2.Weight, amount2.WeightUnit, amount1.WeightUnit);
			return new MetalAmount(amount1.Weight - weightToSubtract, amount1.MetalType, amount1.WeightUnit, amount1.ItemType);
		}
	}
}

