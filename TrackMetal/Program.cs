using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetalAccounting;

namespace TrackMetal
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: TrackMetal <filename> [filenames...]");
				return;
			}

			MetalStorageService storageService = new MetalStorageService("GoldMoney");

			GoldMoneyParser goldMoneyParser = new GoldMoneyParser();
			BullionVaultParser bullionVaultParser = new BullionVaultParser();
			List<Transaction> transactionList = new List<Transaction>();
			foreach (string filename in args)
			{
				if (filename.Contains("GoldMoney"))
					transactionList.AddRange(goldMoneyParser.Parse(filename));
				else if (filename.Contains("BullionVault"))
					transactionList.AddRange(bullionVaultParser.Parse(filename));new List<Transaction>();
			}
			transactionList = transactionList.OrderBy(s => s.DateAndTime).ToList();
			storageService.ApplyTransactions(transactionList);
			PrintResults(storageService);

			string command = "";
			do {
				ProcessCommand(command, storageService);
				command = GetString("command: ");
			} while (command != "quit");
		}

		private static void ProcessCommand(string command, MetalStorageService storageService)
		{
			string[] args = command.Split(' ');
			if (args.Length < 1)
				return;

			switch (args[0])
			{
				case "lot":
					ShowLot(args[1], storageService);
					break;
				case "help":
				default:
					Console.WriteLine("Unknown command");
					Console.WriteLine("Commands: help, lot");
					break;
			}
		}

		private static void ShowLot(string lotID, MetalStorageService storageService)
		{
			Lot thisLot = storageService.Lots.Where(s => s.LotID == lotID).FirstOrDefault();
			if (thisLot != null)
			{
				foreach (string entry in thisLot.History)
					Console.WriteLine(entry);
			}
		}

		private static string GetString(string userPrompt)
		{
			string result = string.Empty;
			Console.Write(userPrompt);
			ConsoleKeyInfo key = Console.ReadKey();
			while (key.Key != ConsoleKey.Enter)
			{
				result += key.KeyChar;
				key = Console.ReadKey();
			}
			return result;
		}
		public static void PrintResults(MetalStorageService storageService)
		{
			Console.WriteLine("Results for " + storageService.Name);
			Console.WriteLine("");
			Console.WriteLine("Sales:");
			string formatString = "Lot ID {0}: Bought {1} {2}, sold {3} {4} for ${5:0.00}, adjusted basis ${6:0.00}, net gain ${7:0.00}";

			foreach (TaxableSale sale in storageService.Sales.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, sale.LotID, sale.MetalType.ToString().ToLower(),
					sale.PurchaseDate.ToShortDateString(), sale.SaleWeight,	sale.SaleDate.ToShortDateString(), 
					sale.SalePrice.Value, sale.AdjustedBasis.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
				Console.WriteLine(formatted);
			}
			Console.WriteLine("");
			Console.WriteLine("Remaining lots:");
			formatString = "Lot ID {0}: {1} bought {2}, remaining {3} {4} {5}";
			foreach (Lot lot in storageService.Lots.Where(s => s.CurrentWeight(MetalWeightEnum.Gram) > 0.0m)
				.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, lot.LotID, lot.Vault, lot.PurchaseDate.ToShortDateString(),
					lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit, lot.MetalType);
				Console.WriteLine(formatted);
			}
			ExportResults(storageService);
		}

		public static void ExportResults(MetalStorageService storageService)
		{
			StreamWriter sw = new StreamWriter("results.txt");
			sw.WriteLine("Lot ID\tMetal\tBought Date\tSold Date\tAdjusted Basis\tSale Price\tNet Gain");
			string formatString = "{0}\t{1}\t{2}\t{3}\t{4:0.00}\t{5:0.00}\t{6:0.00}";

			foreach (TaxableSale sale in storageService.Sales.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, sale.LotID, sale.MetalType.ToString().ToLower(),
					sale.PurchaseDate.ToShortDateString(), sale.SaleDate.ToShortDateString(), 
					sale.AdjustedBasis.Value, sale.SalePrice.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
				sw.WriteLine(formatted);
			}
			sw.Close();
		}
	}
}
