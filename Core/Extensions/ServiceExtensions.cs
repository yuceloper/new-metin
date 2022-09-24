﻿using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumCore.API;
using QuantumCore.Core.Logging.Enrichers;
using QuantumCore.Core.Networking;
using QuantumCore.Game;
using QuantumCore.Game.Commands;
using QuantumCore.Game.PlayerUtils;
using Serilog;
using Weikio.PluginFramework.Catalogs;

namespace QuantumCore.Extensions;

public static class ServiceExtensions
{
    private const string MessageTemplate = "[{Timestamp:HH:mm:ss.fff}][{Level:u3}][{ProcessName:u5}|{MachineName}:" +
                                           "{EnvironmentUserName}]{Caller} >> {Message:lj} " +
                                           "{NewLine:1}{Exception:1}";
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddCustomLogging();
        services.AddSingleton<IPacketManager, DefaultPacketManager>();
        services.AddSingleton<PluginExecutor>();
        services.AddSingleton<IItemManager, ItemManager>();
        services.AddSingleton<IMonsterManager, MonsterManager>();
        services.AddSingleton<IJobManager, JobManager>();
        services.AddSingleton<IAnimationManager, AnimationManager>();
        services.AddSingleton<IExperienceManager, ExperienceManager>();
        services.AddSingleton<ICommandManager, CommandManager>();
        services.AddPluginFramework()
            .AddPluginCatalog(new FolderPluginCatalog("plugins"))
            .AddPluginType<ISingletonPlugin>()
            .AddPluginType<IConnectionLifetimeListener>()
            .AddPluginType<IGameTickListener>()
            .AddPluginType<IPacketOperationListener>()
            .AddPluginType<IGameEntityLifetimeListener>();

        return services;
    }

    private static IServiceCollection AddCustomLogging(this IServiceCollection services)
    {
        var config = new LoggerConfiguration();

        // add minimum log level for the instances
#if DEBUG
        config.MinimumLevel.Verbose();
#else
            config.MinimumLevel.Information();
#endif

        // add destructuring for entities
        config.Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumCollectionCount(10)
            .Destructure.ToMaximumStringLength(100);

        // add environment variable
        config.Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName();

        // add process information
        config.Enrich.WithProcessId()
            .Enrich.WithProcessName();

        // add assembly information
        // TODO: uncomment if needed
        /* config.Enrich.WithAssemblyName() // {AssemblyName}
            .Enrich.WithAssemblyVersion(true) // {AssemblyVersion}
            .Enrich.WithAssemblyInformationalVersion(); */

        // add exception information
        config.Enrich.WithExceptionData();

        // add custom enricher for caller information
        config.Enrich.WithCaller();

        // sink to console
        config.WriteTo.Console(outputTemplate: MessageTemplate);

        // sink to rolling file
        config.WriteTo.RollingFile($"{Directory.GetCurrentDirectory()}/logs/api.log",
            fileSizeLimitBytes: 10 * 1024 * 1024,
            buffered: true,
            outputTemplate: MessageTemplate);

        // finally, create the logger
        services.AddLogging(x =>
        {
            x.ClearProviders();
            x.AddSerilog(config.CreateLogger());
        });
        return services;
    }
}