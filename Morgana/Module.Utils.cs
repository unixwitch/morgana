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
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Morgana {
    public class UtilsModule : ModuleBase<SocketCommandContext> {
        [Command("ping")]
        [Summary("Check whether I'm still alive")]
        public Task Ping() => ReplyAsync("I LIVE!");

        [Command("time")]
        [Summary("Find out what the current time is")]
        public Task Time() => ReplyAsync($"The current time in UTC is {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
    }
}
