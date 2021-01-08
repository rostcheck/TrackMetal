using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Csv;

namespace MetalAccounting
{
	public abstract class ParserBase : IFileParser
	{
		private string serviceName;

		public ParserBase(string serviceName)
		{
			if (serviceName == null || serviceName == string.Empty)
				throw new Exception("Cannot initialize ParserBase without a service name");

			this.serviceName = serviceName;
		}

		public virtual List<Transaction> Parse(string fileName)
		{
			if (fileName.ToLower().EndsWith(".txt"))
				return ParseTxt(fileName);
			else if (fileName.ToLower().EndsWith(".csv"))
				return ParseCsv(fileName);
			else
				throw new FileLoadException("Unrecognized filename extension");
		}

		public abstract Transaction ParseFields(IList<string> fields, string serviceName, string accountName);

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

				transactionList.Add(this.ParseFields(fields, serviceName, accountName));
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

		protected string ParseAccountNameFromFilename(string fileName, string thisServiceName = null)
		{
			if (thisServiceName == null)
				thisServiceName = serviceName;
			Regex r = new Regex(string.Format(@"^{0}-(?<account>\w+)-", thisServiceName));
			Match m = r.Match(fileName);
			if (m.Success)
				return m.Groups["account"].Value;
			else
				throw new Exception("Cannot parse account name from filename " + fileName);
		}

		protected string ParseServiceNameFromFilename(string fileName)
		{
			var parts = fileName.Split('-');
			if (serviceName.ToLower().Contains("generic"))
				serviceName = parts[0];
			return parts[0];
		}

		protected void VerifyFilename(string fileName)
		{
			if (serviceName.ToLower().Contains("generic"))
				return; // Generic parser will accept anything
			
			if (!fileName.Contains(serviceName))
				throw new Exception(string.Format("Filename {0} should contain '{1}' to be parsed by {2}Parser",
					fileName, serviceName, serviceName));
		}
    }
}

