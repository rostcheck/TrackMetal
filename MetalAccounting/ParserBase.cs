using System;
using System.Text.RegularExpressions;

namespace MetalAccounting
{
	public class ParserBase
	{
		private string serviceName;

		public ParserBase(string serviceName)
		{
			if (serviceName == null || serviceName == string.Empty)
				throw new Exception("Cannot initialize ParserBase without a service name");

			this.serviceName = serviceName;
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

