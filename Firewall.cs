using System.Diagnostics;
using System.IO;

namespace BalanceChecker
{
    public static class Firewall
    {
        public static void AddRule()
        {
            var addScript = @"
pushd advfirewall firewall
add rule name=""{0}"" description=""{1}"" localport=""{2}""  action=allow protocol=tcp dir=in
popd
			";

            var deleteScript = @"
pushd advfirewall firewall
delete rule name=""{0}""
popd
			";

            deleteScript = string.Format(deleteScript, Settings.Default.FirewallRuleName);

            addScript = string.Format(addScript, Settings.Default.FirewallRuleName,
                    Settings.Default.FirewallRuleDescription, Settings.Default.HTTPPort);

            const string scriptFileName = "frwl.txt";
            File.WriteAllText(scriptFileName, deleteScript);
            var process = Process.Start("netsh", "exec " + scriptFileName);
            process?.WaitForExit();
            File.WriteAllText(scriptFileName, addScript);
            var start = Process.Start("netsh", "exec " + scriptFileName);
            start?.WaitForExit();
        }
    }
}