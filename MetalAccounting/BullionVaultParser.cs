using System;
using System.Collections.Generic;
using System.IO;

namespace MetalAccounting
{
	public class BullionVaultParser : ParserBase, IFileParser
	{
		public BullionVaultParser() : base("BullionVault")
		{
		}

		public List<Transaction> Parse(string fileName)
		{
			const int headerLines = 1;
			VerifyFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName);

			List<Transaction> transactionList = new List<Transaction>();
			MetalWeightEnum weightUnit = MetalWeightEnum.Gram; // BullionVault does all calculations in grams except silver (kg)
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

				DateTime dateAndTime = DateTime.Parse(fields[0]);
				string transactionID = fields[1];
				string transactionTypeString = fields[2];
				TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString);
				string vault = fields[3];
				decimal weight = Convert.ToDecimal(fields[14]);
				decimal commission = Convert.ToDecimal(fields[15]);
				decimal consideration = Convert.ToDecimal(fields[16]);
				decimal totalCompensation = commission + consideration;
				decimal amountPaid = 0.0m, amountReceived = 0.0m;

				MetalTypeEnum metalType = GetMetalType (fields [7]);
				if (metalType == MetalTypeEnum.Silver)
					weight *= 1000; // Bullionvault counts silver in kg
				
				if (transactionType == TransactionTypeEnum.Purchase)
				{
					amountPaid = totalCompensation;
					amountReceived = weight;
				}
				else if (transactionType == TransactionTypeEnum.Sale)
				{
					amountPaid = weight;
					amountReceived = totalCompensation;
				}
				else if (transactionType == TransactionTypeEnum.StorageFeeInCurrency)
					amountPaid = Math.Abs(totalCompensation);
				else
					throw new Exception("Unknown transaction type " + transactionType);	
				CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[6]);

				Transaction newTransaction = new Transaction("BullionVault", accountName, dateAndTime, 
					transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived, 
					weightUnit, metalType, "");
				transactionList.Add(newTransaction);
				line = reader.ReadLine();
			}

			return transactionList;
		}

		private static CurrencyUnitEnum GetCurrencyUnit(string currencyUnit)
		{
			switch (currencyUnit.ToUpper())
			{
				case "USD":
					return CurrencyUnitEnum.USD;
				default:
					throw new Exception("Unrecognized currency unit " + currencyUnit);
			}
		}

		private static MetalTypeEnum GetMetalType(string metalType)
		{
			switch (metalType.ToLower())
			{
				case "gold":
					return MetalTypeEnum.Gold;
				case "silver":
					return MetalTypeEnum.Silver;
				case "platinum":
					return MetalTypeEnum.Platinum;
				case "palladium":
					return MetalTypeEnum.Palladium;
				default:
					throw new Exception("Unrecognized metal type " + metalType);
			}
		}

		private static TransactionTypeEnum GetTransactionType(string transactionType)
		{
			switch (transactionType.ToLower())
			{
				case "buy":
					return TransactionTypeEnum.Purchase;
				case "sell":
					return TransactionTypeEnum.Sale;
				case "storage_fee":
					return TransactionTypeEnum.StorageFeeInCurrency;
				default:
					throw new Exception("Transaction type " + transactionType + " not recognized");
			}
		}
	}
}

