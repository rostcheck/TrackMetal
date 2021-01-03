using System;

namespace MetalAccounting
{
	public enum TransactionTypeEnum
	{
		StorageFeeInMetal,
		StorageFeeInCurrency,
		Purchase,
		PurchaseViaExchange, // Heterogenous metal-to-metal exchange (ex. gold-to-silver) forces a sale and purchase
		Sale,
		SaleViaExchange,
		TransferOut,
		TransferIn,
		Indeterminate // Cannot determine with info provided - will determine in later phase. Use in parsers only.
	}
}

