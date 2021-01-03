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
			Console.WriteLine("\nStarting run at {0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());

			MetalStorageService storageService = new MetalStorageService();
			GoldMoneyParser goldMoneyParser = new GoldMoneyParser();
			BullionVaultParser bullionVaultParser = new BullionVaultParser();
			GenericCsvParser genericCsvParser = new GenericCsvParser();
			List<Transaction> transactionList = new List<Transaction>();
			foreach (string filename in args)
			{
				if (filename.Contains("tm-"))
					continue;
				else if (filename.Contains("GoldMoney"))
					transactionList.AddRange(goldMoneyParser.Parse(filename));
				else if (filename.Contains("BullionVault"))
					transactionList.AddRange(bullionVaultParser.Parse(filename));
				else
					transactionList.AddRange(genericCsvParser.Parse(filename));
			}
			transactionList = transactionList.OrderBy(s => s.DateAndTime).ToList();
			storageService.ApplyTransactions(transactionList);
			PrintResults(storageService);
			storageService.DumpTransactions("tm-transactions.txt", transactionList);
			//string command = "";
			//do {
			//	ProcessCommand(command, storageService);
			//	command = GetString("command: ");
			//} while (command != "quit");
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
			Console.WriteLine();
			Console.WriteLine("Output list of all transactions to tm-transactions.txt");
			Console.WriteLine("Output capital gains to tm-gains.txt files (by year)");
			ExportCapitalGains(storageService.Sales);

			Console.WriteLine();
			Console.WriteLine("Remaining lots:");
			string formatString = "Lot ID {0} @ {1} in {2}: bought {3}, remaining {4} {5} {6} {7}";
			foreach (Lot lot in storageService.Lots.Where(s => !s.IsDepleted())
				.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, lot.LotID, lot.Service, lot.Vault, lot.PurchaseDate.ToShortDateString(),
					lot.CurrentWeight(lot.WeightUnit), lot.WeightUnit, lot.MetalType, lot.ItemType);
				Console.WriteLine(formatted);
				ShowLot(lot.LotID, storageService);
			}
		}

		public static void PrintCapitalGains(List<TaxableSale> sales)
		{
			string formatString = "{0} Lot ID {1}: Bought {2} {3} ({4}), sold {5} {6} for ${7:0.00}, adjusted basis ${8:0.00}, net gain ${9:0.00}";

			foreach (TaxableSale sale in sales.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, sale.Service, sale.LotID, sale.MetalType.ToString().ToLower(),
					sale.PurchaseDate.ToShortDateString(), sale.SaleWeight,	sale.SaleDate.ToShortDateString(), 
					sale.SalePrice.Value, sale.AdjustedBasis.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
				Console.WriteLine(formatted);
			}
		}

		public static void ExportCapitalGains(List<TaxableSale> sales)
		{
			var years = sales.Select(s => s.SaleDate.Year).Distinct();
			foreach (var year in years)
			{
				StreamWriter sw = new StreamWriter(string.Format("tm-gains-{0}.txt", year));
				sw.WriteLine("Service\tLot ID\tMetal\tItemType\tBought Date\tSold Date\tAdjusted Basis\tSale Price\tNet Gain");
				string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:0.00}\t{7:0.00}\t{8:0.00}";

				foreach (TaxableSale sale in sales.Where(s => s.SaleDate.Year == year).OrderBy(s => s.PurchaseDate).ToList())
				{
					string formatted = string.Format(formatString, sale.Service,sale.LotID, 
						sale.MetalType.ToString().ToLower(), sale.ItemType, 
						sale.PurchaseDate.ToShortDateString(), sale.SaleDate.ToShortDateString(), 
						sale.AdjustedBasis.Value, sale.SalePrice.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
					sw.WriteLine(formatted);
				}
				sw.Close();				
			}
		}
	}
}
