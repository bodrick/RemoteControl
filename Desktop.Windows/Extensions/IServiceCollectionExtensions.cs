﻿using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static async Task AddRemoteControlClient(
            this IServiceCollection services,
            string[] args,
            string serverUri = "",
            CancellationToken cancellationToken = default)
        {

            var rootCommand = new RootCommand(
                $"This app is using the {typeof(IServiceCollectionExtensions).Assembly.GetName().Name} library, " +
                $"which allows IT administrators to provide remote assistance on this device.");

            var hostOption = new Option<string>(
                new[] { "-h", "--host" },
                "The hostname of the server to which to connect (e.g. https://example.com).");
            if (string.IsNullOrWhiteSpace(serverUri))
            {
                hostOption.IsRequired = true;
            }
            rootCommand.AddOption(hostOption);

            var modeOption = new Option<AppMode>(
                new[] { "-m", "--mode" },
                () => AppMode.Attended,
                "The remote control mode to use.  Either Attended, Unattended, or Chat.");
            rootCommand.AddOption(modeOption);

            var elevateOption = new Option<bool>(
                new[] { "-e", "--elevate" },
                "Attempt to relaunch the process with elevated privileges.");
            rootCommand.AddOption(elevateOption);

            var requesterIdOption = new Option<string>(
               new[] { "-r", "--requester" },
               "Attempt to relaunch the process with elevated privileges.");
            rootCommand.AddOption(requesterIdOption);

            var serviceIdOption = new Option<string>(
                new[] { "-s", "--service-id" },
                "The SignalR connection ID of the service process that launched this process.");
            rootCommand.AddOption(serviceIdOption);

            var deviceIdOption = new Option<string>(
                new[] { "-d", "--device-id" },
                "The unique ID (e.g. Entity PK) of this device.");
            rootCommand.AddOption(deviceIdOption);

            var organizationIdOption = new Option<string>(
                new[] { "-o", "--org-id" },
                "The organization ID (e.g. Entity PK) of the technician requesting to connect.");
            rootCommand.AddOption(organizationIdOption);

            var organizationNameOption = new Option<string>(
                new[] { "-n", "--org-name" },
                "The organization name of the technician requesting to connect.");
            rootCommand.AddOption(organizationNameOption);

            rootCommand.SetHandler(
                async (
                    host,
                    elevate,
                    mode,
                    requesterId,
                    serviceId,
                    deviceId,
                    organizationId,
                    organizationName) =>
                {
                    if (elevate)
                    {
                        RelaunchElevated();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(serverUri))
                    {
                        host = serverUri;
                    }


                },
                hostOption,
                elevateOption,
                modeOption,
                requesterIdOption,
                serviceIdOption,
                deviceIdOption,
                organizationIdOption,
                organizationNameOption);

            await rootCommand.InvokeAsync(args);
        }

        private static void RelaunchElevated()
        {
            var commandLine = Win32Interop.GetCommandLine().Replace(" --elevate", "").Replace(" -e", "");

            Console.WriteLine($"Elevating process {commandLine}.");
            var result = Win32Interop.OpenInteractiveProcess(
                commandLine,
                -1,
                false,
                "default",
                true,
                out var procInfo);
            Console.WriteLine($"Elevate result: {result}. Process ID: {procInfo.dwProcessId}.");
            Environment.Exit(0);
        }
    }
}