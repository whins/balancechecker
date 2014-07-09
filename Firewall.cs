using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETCONLib;
using NetFwTypeLib;
using System.Diagnostics;
using System.IO;

namespace BalanceChecker
{
	public static class Firewall
	{
		public static void AddRule()
		{
			string addScript = @"
pushd advfirewall firewall
add rule name=""{0}"" description=""{1}"" remoteip=""{2}"" localport=""{3}""  action=allow protocol=tcp dir=in
popd
			";

			string deleteScript = @"
pushd advfirewall firewall
delete rule name=""{0}""
popd
			";

			deleteScript = string.Format(deleteScript, Settings.Default.FirewallRuleName);

			addScript = string.Format(addScript, Settings.Default.FirewallRuleName,
					Settings.Default.FirewallRuleDescription, Settings.Default.Server, Settings.Default.HTTPPort);

			string scriptFileName = "frwl.txt";
			File.WriteAllText(scriptFileName, deleteScript);
			Process.Start("netsh", "exec " + scriptFileName).WaitForExit();
			File.WriteAllText(scriptFileName, addScript);
			Process.Start("netsh", "exec " + scriptFileName).WaitForExit();
		}
	}
}
