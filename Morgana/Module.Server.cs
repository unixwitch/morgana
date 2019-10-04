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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;

namespace Morgana {
    [Group("server")]
    [Summary("Commands for managing the bot's servers")]
    public class ServerModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("list", RunMode = RunMode.Async)]
        [Summary("List currently active servers.")]
        [RequireBotOwner]
        public async Task List() {
            var guilds = Context.Client.Guilds;

            var header = $"{"Server ID",-22} {"Connected",-9} {"Synced",-6} Name\n";

            var strings = guilds
                .Select(g => Format.Sanitize($"{g.Id,-22} {g.IsConnected,-9} {g.IsSynced,-6} {g.Name}\n"));

            var str = "```" + header + string.Join(", ", strings) + "```";
            await ReplyAsync(str);
        }
    }
}
