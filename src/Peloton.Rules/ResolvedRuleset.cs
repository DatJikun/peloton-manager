using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Peloton.Domain;

namespace Peloton.Rules;

public static class ResolvedRuleset
{
    public static string ComputeIdentity(IEnumerable<RulesModuleIdentity> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        StringBuilder canonical = new();
        foreach (RulesModuleIdentity module in modules
                     .OrderBy(module => module.Slot, StringComparer.Ordinal)
                     .ThenBy(module => module.Id, StringComparer.Ordinal))
        {
            canonical.Append(module.Slot).Append('\u001f')
                .Append(module.Id).Append('\u001f')
                .Append(module.Contract).Append('\u001f')
                .Append(module.ContractVersion).Append('\u001f')
                .Append(module.ParameterIdentity).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
