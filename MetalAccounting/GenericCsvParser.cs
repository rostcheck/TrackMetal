using System;
using System.Collections.Generic;
using System.IO;
using Csv;

namespace MetalAccounting
{
	public class GenericCsvParser : ParserBase, IFileParser
	{
		public GenericCsvParser() : base("GenericCsv")
		{
		}

		public List<Transaction> Parse(string fileName)
		{
			if (fileName.ToLower().EndsWith(".txt"))
				return ParseTxt(fileName);
			else if (fileName.ToLower().EndsWith(".csv"))
				return ParseCsv(fileName);
			else
				throw new FileLoadException("Unrecognized filename extension");
		}

		private List<Transaction> ParseTxt(string fileName)
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

				transactionList.Add(ParseFields(fields, serviceName, accountName));
				line = reader.ReadLine();
			}

			return transactionList;
		}

		private List<Transaction> ParseCsv(string fileName)
        {
			string serviceName = ParseServiceNameFromFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName, serviceName);
			List<Transaction> transactionList = new List<Transaction>();
			var csv = File.ReadAllText(fileName);
			foreach (var readFields in CsvReader.ReadFromText(csv))
			{
				List<string> fields = new List<string>(readFields.ColumnCount);
				for (int i = 0; i < readFields.ColumnCount; i++)
					fields.Add(readFields[i]);
				transactionList.Add(ParseFields(fields, serviceName, accountName));
			}
			return transactionList;
		}

		private Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
			DateTime dateAndTime = DateTime.Parse(fields[0]);
			string vault = fields[1];
			string transactionID = fields[2];
			string transactionTypeString = fields[3];
			TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString);
			decimal currencyAmount = Convert.ToDecimal(fields[4].Replace("$", ""));
			CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[5]);
			decimal weight = Convert.ToDecimal(fields[6]);
			MetalWeightEnum weightUnit = GetWeightUnit(fields[7]);
			string itemType = "Generic";
			itemType = fields[12];

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
			else if (transactionType == TransactionTypeEnum.TransferIn)
				amountReceived = weight;
			else if (transactionType == TransactionTypeEnum.TransferOut)
				amountPaid = weight;
			else if (transactionType == TransactionTypeEnum.StorageFeeInCurrency)
				amountPaid = Math.Abs(currencyAmount);
			else if (transactionType != TransactionTypeEnum.Transfer)
				throw new Exception("Unknown transaction type " + transactionType);

			MetalTypeEnum metalType = GetMetalType(fields[8]);

			return new Transaction(serviceName, accountName, dateAndTime,
				transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived,
				weightUnit, metalType, "", itemType);

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
				case "CRYPTOCOIN":
					return MetalWeightEnum.CryptoCoin;
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
				case "crypto":
					return MetalTypeEnum.Crypto;
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
				case "send":
					return TransactionTypeEnum.TransferOut;
				case "receive":
					return TransactionTypeEnum.TransferIn;
				case "transfer":
					return TransactionTypeEnum.Transfer;
				default:
					throw new Exception("Transaction type " + transactionType + " not recognized");
			}
		}
	}
}

