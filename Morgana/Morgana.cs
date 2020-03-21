/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019, 2020 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using CacheManager.Core;
using CacheManager.MicrosoftCachingMemory;
using Newtonsoft.Json;
using System.Linq;
using EFCoreSecondLevelCacheInterceptor;

namespace Morgana {
    public class RequireBotOwnerAttribute : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {
            var config = services.GetRequiredService<StorageContext>();

            if (await config.IsOwnerAsync(context.User.Id))
                return await Task.FromResult(PreconditionResult.FromSuccess());

            return await Task.FromResult(PreconditionResult.FromError("This command can only be used by global bot owners."));
        }
    }

    public class RequireBotAdminAttribute : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {
            if (context.Guild == null)
                return await Task.FromResult(PreconditionResult.FromError("This command cannot be used in a direct message."));

            var config = services.GetRequiredService<StorageContext>();

            if (!(context.User is IGuildUser guser))
                return await Task.FromResult(PreconditionResult.FromError("This command cannot be used in a direct message."));

            var gcfg = config.GetGuild(context.Guild);

            if (await config.IsOwnerAsync(guser.Id))
                return await Task.FromResult(PreconditionResult.FromSuccess());

            if (await gcfg.IsAdminAsync(guser))
                return await Task.FromResult(PreconditionResult.FromSuccess());

            return await Task.FromResult(PreconditionResult.FromError("This command can only be used by bot administrators."));
        }
    }

    public class Bot {
        Configuration _config;

        public Bot(Configuration config) {
            _config = config;
        }

        public Task Log(LogMessage msg) {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }

        public async Task Run() {
            var clientConfig = new DiscordSocketConfig {
                MessageCacheSize = 1024
            };
            var client = new DiscordSocketClient(clientConfig);
            client.Log += Log;

            using var services =
                new ServiceCollection()
                    .AddLogging()
                    //.AddSingleton<MemoryCacheServiceProvider>()
                    .AddEFSecondLevelCache(options => {
                        //options.UseMemoryCacheProvider();
                        options.UseCustomCacheProvider<MemoryCacheServiceProvider>();
                        options.CacheAllQueries(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(30));
                    })
                    .AddSingleton(typeof(ICacheManagerConfiguration),
                        new CacheManager.Core.ConfigurationBuilder()
                            .WithJsonSerializer()
                            .WithMicrosoftMemoryCacheHandle(instanceName: "morgana")
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                            .EnableStatistics()
                            .EnablePerformanceCounters()
                            .Build())
                    .AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>))
                    .AddDbContext<StorageContext>(options => _config.ConfigureDb(options))
                    .AddSingleton(client)
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<BadwordsFilter>()
                    .AddSingleton<InfobotService>()
                    .AddSingleton<AuditLogger>()
                    .AddSingleton<PicMover>()
                    .AddSingleton<SpellingService>()
                    .AddSingleton(_config)
                    .BuildServiceProvider();

            var db = services.GetService<StorageContext>();
            if ((await db.GetOwnersAsync()).Count() == 0) {
                if (_config.InitialOwner == null) {
                    Console.WriteLine("Warning: no bot owners are defined and general:initial_owner was not set in the configuration.");
                    Console.WriteLine("This bot will not have any owners.");
                } else {
                    await db.OwnerAddAsync(_config.InitialOwner.Value);
                    Console.WriteLine($"Added initial bot owner {_config.InitialOwner.Value}.");
                }
            }

            services.GetRequiredService<CommandService>().Log += Log;
            await services.GetRequiredService<CommandHandler>().InitializeAsync();
            await services.GetRequiredService<BadwordsFilter>().InitialiseAsync();
            await services.GetRequiredService<AuditLogger>().InitialiseAsync();
            await services.GetRequiredService<PicMover>().InitialiseAsync();

            await client.LoginAsync(TokenType.Bot, _config.Token);
            await client.StartAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(-1));
        }
    }
}
