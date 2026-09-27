namespace Shafiee.Core.Parsers;

using Shafiee.SDK.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

public static class CommandParser
{
    public static UserCommand Parse(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("No command provided. Usage: [shafiee] <verb> <resource> [target]");
        }

        var argList = args.ToList();

        // 1. If the first word is shafiee, remove it to keep the chain intact
        if (argList[0].Equals("shafiee", StringComparison.OrdinalIgnoreCase))
        {
            argList.RemoveAt(0);
        }

        if (argList.Count == 0)
        {
            throw new ArgumentException("Invalid command format. Usage: <verb> <resource> [target]");
        }

        string verb = argList[0].ToLowerInvariant();

        // Support for single-word commands like doctor
        string resource = argList.Count > 1 && !argList[1].StartsWith("-")
            ? argList[1].ToLowerInvariant()
            : "system";

        string? target = null;
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int index = (resource == "system") ? 1 : 2;

        // Extract Target (e.g., Blog or Product)
        if (index < argList.Count && !argList[index].StartsWith("-"))
        {
            target = argList[index];
            index++;
        }

        // Extract all Flags and Options (with both - and --)
        while (index < argList.Count)
        {
            if (argList[index].StartsWith("-"))
            {
                // Remove - or -- from the start of the key
                string key = argList[index].TrimStart('-').ToLowerInvariant();
                string value = "true";

                if (index + 1 < argList.Count && !argList[index + 1].StartsWith("-"))
                {
                    value = argList[index + 1];
                    index++;
                }

                options[key] = value;
            }
            index++;
        }

        return new UserCommand
        {
            Verb = verb,
            Resource = resource,
            Target = target,
            Options = options
        };
    }
}