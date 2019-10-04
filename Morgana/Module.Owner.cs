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
    [Group("owner")]
    [Summary("Commands for managing bot owners")]
    public class OwnerModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("list", RunMode = RunMode.Async)]
        [Summary("List currently configured bot owners")]
        [RequireBotOwner]
        public async Task List() {
            var owners = await Vars.GetOwnersAsync();

            if (owners.Count() == 0) {
                await ReplyAsync("No bot owners have been configured yet.");
                return;
            }

            var strings = owners
                .Select(o => Context.Client.GetUser(o))
                .Select(u => $"{u.Username}#{u.Discriminator}");

            var str = Format.Sanitize(string.Join(", ", strings));
            await ReplyAsync($"Configured bot owners: {str}.");
        }

        [Command("add", RunMode = RunMode.Async)]
        [Summary("Add a new bot owner")]
        [RequireBotOwner]
        public async Task AddUser([Summary("The bot owner to be added")] IUser target) {
            if (target == null) {
                await ReplyAsync("That user doesn't seem to exist.");
                return;
            }

            if (await Vars.OwnerAddAsync(target.Id))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("That user is already a bot owner.");
        }

        [Command("remove")]
        [Summary("Remove a bot owner")]
        [RequireBotOwner]
        public async Task RemoveUser([Summary("The owner to be removed")] IUser target) {
            var owners = await Vars.GetOwnersAsync();

            if (owners.Count() == 1) {
                await ReplyAsync("You cannot remove the last bot owner, otherwise there would be none left.");
                return;
            }

            if (await Vars.OwnerRemoveAsync(target.Id))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("That user is not a bot owner.");
        }
    }
}
