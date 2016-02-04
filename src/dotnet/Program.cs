﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel.Server;
using Microsoft.DotNet.Tools.Build;
using Microsoft.DotNet.Tools.Compiler;
using Microsoft.DotNet.Tools.Compiler.Csc;
using Microsoft.DotNet.Tools.Compiler.Fsc;
using Microsoft.DotNet.Tools.Compiler.Native;
using Microsoft.DotNet.Tools.Help;
using Microsoft.DotNet.Tools.New;
using Microsoft.DotNet.Tools.Publish;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.Tools.Restore;
using NuGet.Frameworks;
using Microsoft.DotNet.Tools.Resgen;
using Microsoft.DotNet.Tools.Run;
using Microsoft.DotNet.Tools.Test;
using Microsoft.DotNet.Tools.Repl;

namespace Microsoft.DotNet.Cli
{
    public class Program
    {

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                return ProcessArgs(args);
            }
            catch (CommandUnknownException e)
            {
                Console.WriteLine(e.Message);

                return 1;
            }

        }

        private static int ProcessArgs(string[] args)
        {
            // CommandLineApplication is a bit restrictive, so we parse things ourselves here. Individual apps should use CLA.

            bool? verbose = null;
            var success = true;
            var command = string.Empty;
            var lastArg = 0;
            for (; lastArg < args.Length; lastArg++)
            {
                if (IsArg(args[lastArg], "v", "verbose"))
                {
                    verbose = true;
                }
                else if (IsArg(args[lastArg], "version"))
                {
                    PrintVersionInfo();
                    return 0;
                }
                else if (IsArg(args[lastArg], "h", "help"))
                {
                    HelpCommand.PrintHelp();
                    return 0;
                }
                else if (args[lastArg].StartsWith("-"))
                {
                    Reporter.Error.WriteLine($"Unknown option: {args[lastArg]}");
                    success = false;
                }
                else
                {
                    // It's the command, and we're done!
                    command = args[lastArg];
                    break;
                }
            }
            if (!success)
            {
                HelpCommand.PrintHelp();
                return 1;
            }

            var appArgs = (lastArg + 1) >= args.Length ? Enumerable.Empty<string>() : args.Skip(lastArg + 1).ToArray();

            if (verbose.HasValue)
            {
                Environment.SetEnvironmentVariable(CommandContext.Variables.Verbose, verbose.ToString());
            }

            if (string.IsNullOrEmpty(command))
            {
                command = "help";
            }

            var builtIns = new Dictionary<string, Func<string[], int>>
            {
                ["build"] = BuildCommand.Run,
                ["compile"] = CommpileCommand.Run,
                ["compile-csc"] = CompileCscCommand.Run,
                ["compile-fsc"] = CompileFscCommand.Run,
                ["compile-native"] = CompileNativeCommand.Run,
                ["help"] = HelpCommand.Run,
                ["new"] = NewCommand.Run,
                ["pack"] = PackCommand.Run,
                ["projectmodel-server"] = ProjectModelServerCommand.Run,
                ["publish"] = PublishCommand.Run,
                ["repl"] = ReplCommand.Run,
                ["restore"] = RestoreCommand.Run,
                ["resgen"] = ResgenCommand.Run,
                ["run"] = RunCommand.Run,
                ["test"] = TestCommand.Run
            };

            Func<string[], int> builtIn;
            if (builtIns.TryGetValue(command, out builtIn))
            {
                return builtIn(appArgs.ToArray());
            }

            return Command.Create("dotnet-" + command, appArgs, FrameworkConstants.CommonFrameworks.DnxCore50)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute()
                .ExitCode;
        }

        private static void PrintVersionInfo()
        {
            HelpCommand.PrintVersionHeader();

            var runtimeEnvironment = PlatformServices.Default.Runtime;
            Reporter.Output.WriteLine("Runtime Environment:");
            Reporter.Output.WriteLine($" OS Name:     {runtimeEnvironment.OperatingSystem}");
            Reporter.Output.WriteLine($" OS Version:  {runtimeEnvironment.OperatingSystemVersion}");
            Reporter.Output.WriteLine($" OS Platform: {runtimeEnvironment.OperatingSystemPlatform}");
            Reporter.Output.WriteLine($" Runtime Id:  {runtimeEnvironment.GetRuntimeIdentifier()}");
        }

        private static bool IsArg(string candidate, string longName)
        {
            return IsArg(candidate, shortName: null, longName: longName);
        }

        private static bool IsArg(string candidate, string shortName, string longName)
        {
            return (shortName != null && candidate.Equals("-" + shortName)) || (longName != null && candidate.Equals("--" + longName));
        }
    }
}
