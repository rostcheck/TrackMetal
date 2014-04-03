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

		protected string ParseAccountNameFromFilename(string fileName)
		{
			Regex r = new Regex(string.Format(@"^{0}-(?<account>\w+)-", serviceName));
			Match m = r.Match(fileName);
			if (m.Success)
				return m.Groups["account"].Value;
			else
				throw new Exception("Cannot parse account name from filename " + fileName);
		}

		protected void VerifyFilename(string fileName)
		{
			if (!fileName.Contains(serviceName))
				throw new Exception(string.Format("Filename {0} should contain '{1}' to be parsed by {2}Parser",
					fileName, serviceName, serviceName));
		}
	}
}

