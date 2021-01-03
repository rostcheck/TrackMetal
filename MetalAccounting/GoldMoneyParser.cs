using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MetalAccounting
{
	public class GoldMoneyParser : ParserBase, IFileParser
	{
		const int headerLines = 1;
		public GoldMoneyParser () : base("GoldMoney")
		{
		}

		public List<Transaction> Parse(string fileName)
		{
			VerifyFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName);

			List<Transaction> transactionList = new List<Transaction>();
			MetalTypeEnum metalType = ParseMetalTypeFromFilename(fileName);
			MetalWeightEnum weightUnit = GetWeightUnit(metalType);

			StreamReader reader = new StreamReader(fileName);
			string line = reader.ReadLine();
			int lineCount = 0;
			while (line != null && line != string.Empty) 
			{
				if (lineCount++ < headerLines)
				{
				 	line = reader.ReadLine();
					continue;
				}
				string[] fields = line.Split('\t');
				if (string.Join("", fields) == string.Empty || line.Contains("Number of transactions ="))
				{
					line = reader.ReadLine();
					continue;
				}
				CurrencyUnitEnum currencyUnit = CurrencyUnitEnum.USD;

				DateTime dateAndTime = DateTime.Parse(fields[0]);
				string transactionID = fields[1];
				string transactionTypeString = fields[2];
				string memo = fields[6];
				decimal amountPaid = (fields[4] == string.Empty) ? 0.0m : Convert.ToDecimal(fields[4]);
				decimal amountReceived = (fields[5] == string.Empty) ? 0.0m : Convert.ToDecimal(fields[5]);
				TransactionTypeEnum apparentTransactionType = GetApparentTransactionType(amountPaid, amountReceived);
				TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString, memo, apparentTransactionType);
				memo = FixMemo(transactionTypeString, memo, transactionID);
				if (transactionType == TransactionTypeEnum.TransferIn || transactionType == TransactionTypeEnum.TransferOut)
					transactionID = ParseTransferTransactionID(memo);
				string vault = fields[3];
				currencyUnit = ParseCurrencyUnit(memo);
				if (amountPaid == 0.0m && transactionType != TransactionTypeEnum.TransferOut)
					amountPaid = ParseAmountPaid(memo, currencyUnit);
				if (amountReceived == 0.0m && transactionType != TransactionTypeEnum.TransferIn)
					amountReceived = ParseAmountReceived(memo, currencyUnit);
				Transaction newTransaction = new Transaction("GoldMoney", accountName, dateAndTime, 
					transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived, 
					weightUnit, metalType, memo, "Generic");
				transactionList.Add(newTransaction);
			 	line = reader.ReadLine();
			}
			return transactionList;
		}

		private static TransactionTypeEnum GetApparentTransactionType(decimal amountPaid, decimal amountReceived)
		{
			if (amountPaid > 0.0m && amountReceived == 0.0m)
				return TransactionTypeEnum.Sale;
			else if (amountPaid == 0.0m && amountReceived > 0.0m)
				return TransactionTypeEnum.Purchase;
			else
				return TransactionTypeEnum.Indeterminate;
		}

		private static MetalWeightEnum GetWeightUnit(MetalTypeEnum metalType)
		{
			switch (metalType)
			{
				case MetalTypeEnum.Gold:
					return MetalWeightEnum.Gram;
				case MetalTypeEnum.Silver:
					return MetalWeightEnum.TroyOz;
				case MetalTypeEnum.Platinum:
					return MetalWeightEnum.Gram;
				case MetalTypeEnum.Palladium:
					return MetalWeightEnum.Gram;
				default:
					throw new Exception("Unknown metal type " + metalType);
			}
		}

		private static string FixMemo(string transactionTypeString, string memo, string transactionID)
		{
			memo = transactionTypeString + " " + memo;
			memo = memo.Replace("GoldMoney Fee: 0.00%", "");
			if (transactionTypeString.ToLower().Contains("payment"))
				memo = memo + "Thank you for using GoldMoney. (" + transactionID + ")";
			return memo;
		}

		private static string ParseTransferTransactionID(string memo)
		{
			Regex r = new Regex(@"Thank you for using GoldMoney. \((?<transfer_id>[\w\d\-]+)\)");
			Match m = r.Match(memo);
			if (m.Success)
				return m.Groups["transfer_id"].Value;
			else
				throw new Exception("Cannot parse transfer transaction id name from memo " + memo);
		}

		private static MetalTypeEnum ParseMetalTypeFromFilename(string filenameIn)
		{
			Regex r = new Regex(@"^GoldMoney-(?<account>\w+)-(?<metal>\w+)");
			Match m = r.Match(filenameIn);
			if (!m.Success)
				throw new Exception("Can't identify metal type from filename " + filenameIn);
						
			switch (m.Groups["metal"].Value.ToLower())
			{
				case "gold":
					return MetalTypeEnum.Gold;
				case "silver":
					return MetalTypeEnum.Silver;
				case "platinum":
					return MetalTypeEnum.Platinum;
				case "paladium":
					return MetalTypeEnum.Palladium;
				default:
					throw new Exception("Can't identify metal type from filename " + filenameIn);
			}
		}

		private static decimal ParseAmountPaid(string memo, CurrencyUnitEnum currencyUnit)
		{
			return ParseAmount(memo, currencyUnit);
		}

		private static decimal ParseAmountReceived(string memo, CurrencyUnitEnum currencyUnit)
		{
			return ParseAmount(memo, currencyUnit);
		}

		private static CurrencyUnitEnum ParseCurrencyUnit(string memo)
		{
			CurrencyUnitEnum currencyUnit = CurrencyUnitEnum.USD; // default
			if (memo.Contains("USD"))
				currencyUnit = CurrencyUnitEnum.USD;
			// TODO: add other currencies
			return currencyUnit;
		}
					
		/*
		 * Example memo fields to parse amount paid from:
		 * GoldGram purchase by e-check on 2004-Sep-07 for $241.00 plus a $3 processing fee (at a spot rate of $12.747/gg plus the Standard rate of 2.99%).
		 * GoldGram purchase by e-check on 2006-Jan-27 for USD1,099.00 of goldgrams (USD1,096.00 plus a USD3.00 processing fee) at a spot rate of USD17.866/gg plus the Standard rate of 3.49% --- Thank you for using GoldMoney. (B-GCZT9T)
		 * GoldGram sale for a Gold-to-Silver exchange: 72.334gg for USD 1,700.00 --- Thank you for using GoldMoney. (X-W4T8NB)
		 * GoldGram sale for a Gold-to-Gold exchange: 5.826gg for USD 178.39 --- Thank you for using GoldMoney. (X-GS4N2L)
		 * GoldGram purchase by Silver-to-Gold exchange on 2009-Sep-03 for USD 2,429.90 at a spot rate of USD 31.7666/gg plus the GoldMoney rate of 1.99% --- Thank you for using GoldMoney. (X-LXRSRY)
		 * Gold-to-Platinum exchange at a spot rate of USD 53.0925/gg: 222.500gg for USD 11,813.08. GoldMoney Fee: 0.00% --- Thank you for using GoldMoney. (X-2YXPNH)
		 */
		private static decimal ParseAmount(string memo, CurrencyUnitEnum currencyUnit)
		{
			decimal amount = 0.0m;
			// Transform "for $121.00", "for USD121.00" and "for USD 121.00" all to "$121.00"
			if (memo.Contains(currencyUnit.ToString()))
			{
				memo = memo.Replace(currencyUnit.ToString(), "$");
				memo = memo.Replace("$ ", "$");
			}

			Regex r = new Regex(@"for \$(?<cost>[\d\.\,]+)");
			Match m = r.Match(memo);
			if (m.Success)
			{
				string amountString = m.Groups["cost"].Value;
				if (amountString.EndsWith("."))
					amountString = amountString.Remove(amountString.Length - 1);
				amount = Convert.ToDecimal(amountString);
			}

			// Early memos, without thank-you message, include a fee that is not totalled
			if (!memo.Contains("Thank you for using GoldMoney"))
			{
				r = new Regex(@"plus a \$(?<fee>[\d\.\,]+) processing fee");
				m = r.Match(memo);
				if (m.Success)				
					amount += Convert.ToDecimal(m.Groups["fee"].Value);
			}
			return amount;
		}

		private static TransactionTypeEnum GetTransactionType(string transactionType, string memo, 
			TransactionTypeEnum apparentTransactionType)
		{
			switch (transactionType.ToLower())
			{
				case "account fee":
				case "storage fee":
				case "payment fee":
					return TransactionTypeEnum.StorageFeeInMetal;
				case "buy metal":
					return TransactionTypeEnum.Purchase;
				case "sell metal":
					return TransactionTypeEnum.Sale;
				case "exchange metal":
					if (apparentTransactionType == TransactionTypeEnum.Indeterminate)
						return GetExchangeType(memo);
					else
					{
						// If the transaction is a exchange of a metal for the same metal, this is really a transfer
						if (IsFromAndToSameMetal(memo))
						{
							return (apparentTransactionType == TransactionTypeEnum.Sale) ? TransactionTypeEnum.TransferOut : TransactionTypeEnum.TransferIn;
						}
						switch (apparentTransactionType)
						{
							case TransactionTypeEnum.Purchase:
								return TransactionTypeEnum.PurchaseViaExchange;
							case TransactionTypeEnum.Sale:
								return TransactionTypeEnum.SaleViaExchange;
							default:
								return apparentTransactionType;
						}
					}
				default:
					return (apparentTransactionType == TransactionTypeEnum.Sale) ? TransactionTypeEnum.TransferOut : TransactionTypeEnum.TransferIn;

			}
		}

		private static bool IsFromAndToSameMetal(string memo)
		{
			memo = memo.ToLower();
			var metalPairs = new string[] {
				"silver-to-silver",
				"gold-to-gold",
				"platinum-to-platinum",
				"palladium-to-palladium"
			};
			foreach (string pair in metalPairs)
			{
				if (memo.Contains(pair))
					return true;
			}
			return false;
		}

		private static TransactionTypeEnum GetOtherType(string transactionType, string memo)
		{
			if (transactionType.ToLower().Contains("payment"))
				return TransactionTypeEnum.TransferOut;
			else
				throw new Exception("Unknown transaction type " + transactionType);
		}

		private static TransactionTypeEnum GetExchangeType(string memo)
		{
			Regex r = new Regex(@"(?<metal1>\w+)-to-(?<metal2>\w+) exchange");
			Match m = r.Match(memo.ToLower());
			if (m.Success)
			{
				if (m.Groups["metal1"].Value == m.Groups["metal2"].Value)
				{
					throw new Exception("can't identify transaction from: " + memo);
				}
				else
				{
					if (memo.ToLower().Contains("sale"))
						return TransactionTypeEnum.Sale;
					else if (memo.ToLower().Contains("purchase"))
						return TransactionTypeEnum.Purchase;
					else if (memo.ToLower().Contains("exchange"))
						return TransactionTypeEnum.Sale;
					else
						throw new Exception("can't identify transaction from: " + memo);
				}
			}
			else
			{
				throw new Exception("can't identify transaction from exchange: " + memo);
			}
		}
	}
}

