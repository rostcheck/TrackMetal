using System;
using System.Collections.Generic;
using System.IO;

namespace MetalAccounting
{
	public class GenericCsvParser : ParserBase, IFileParser
	{
		public GenericCsvParser() : base("GenericCsv")
		{
		}

		public List<Transaction> Parse(string fileName)
		{
			const int headerLines = 1;
			string serviceName = ParseServiceNameFromFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName, serviceName);

			List<Transaction> transactionList = new List<Transaction>();
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
				if (fields.Length < 2)
				{
					fields = line.Split(','); // Could be CSV
				}
				if (string.Join("", fields) == string.Empty || line.Contains("Number of transactions ="))
				{
					line = reader.ReadLine();
					continue;
				}

				DateTime dateAndTime = DateTime.Parse(fields[0]);
				string vault = fields[1];
				string transactionID = fields[2];
				string transactionTypeString = fields[3];
				TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString);
				decimal currencyAmount = Convert.ToDecimal(fields[4].Replace("$",""));
				CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[5]);			
				decimal weight = Convert.ToDecimal(fields[6]);
				MetalWeightEnum weightUnit = GetWeightUnit(fields[7]);
				string itemType = "Generic";
				if (fields.Length >= 13)
				{
					itemType = fields[12];
				}

				decimal amountPaid = 0.0m, amountReceived = 0.0m;
				if (transactionType == TransactionTypeEnum.Purchase)
				{
					amountPaid = currencyAmount;
					amountReceived = weight;
				}
				else if (transactionType == TransactionTypeEnum.Sale)
				{
					amountPaid = weight;
					amountReceived = currencyAmount;
				}
				else if (transactionType == TransactionTypeEnum.StorageFeeInCurrency)
					amountPaid = Math.Abs(currencyAmount);
				else
					throw new Exception("Unknown transaction type " + transactionType);	
				
				MetalTypeEnum metalType = GetMetalType(fields[8]);

				Transaction newTransaction = new Transaction(serviceName, accountName, dateAndTime, 
					transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived, 
					weightUnit, metalType, "", itemType);
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

		private static MetalWeightEnum GetWeightUnit(string weightUnit)
		{
			switch (weightUnit.ToUpper())
			{
				case "OZ":
				case "TROYOZ":
					return MetalWeightEnum.TroyOz;
				case "G":
					return MetalWeightEnum.Gram;
				default:
					throw new Exception("Unrecognized weight unit " + weightUnit);
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
				case "storage fee":
					return TransactionTypeEnum.StorageFeeInCurrency;
				default:
					throw new Exception("Transaction type " + transactionType + " not recognized");
			}
		}
	}
}

