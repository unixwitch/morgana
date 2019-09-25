/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;

namespace Morgana {
    public class CommandHandler {
        public DiscordSocketClient _client { get; set; }
        public CommandService _commands { get; set; }
        public IServiceProvider _services { get; set; }
        public Storage _config { get; set; }

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, Storage config) {
            _client = client;
            _commands = commands;
            _services = services;
            _config = config;
        }

        public async Task HandleCommand(SocketMessage p) {
            var message = p as SocketUserMessage;
            if (message == null)
                return;

            if (message.Author.IsBot)
                return;

            int argpos = 0;
            var channel = message.Channel as SocketGuildChannel;
            if (channel != null) {
                var gcfg = _config.GetGuild(channel.Guild);
                if (gcfg.CommandPrefix == null)
                    gcfg.CommandPrefix = "~";

                if (!(message.HasStringPrefix(gcfg.CommandPrefix, ref argpos)) || message.HasMentionPrefix(_client.CurrentUser, ref argpos))
                    return;
            }

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argpos, _services);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async Task InitializeAsync() {
            _client.MessageReceived += HandleCommand;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}