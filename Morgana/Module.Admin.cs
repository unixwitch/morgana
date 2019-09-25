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
    [Group("admin")]
    [Summary("Commands for managing bot admins")]
    public class AdminModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("list")]
        [Summary("List currently configured bot admins")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var gcfg = Vars.GetGuild(Context.Guild);

            if (gcfg.Admins.Count() == 0) {
                await ReplyAsync("No admins have been configured yet.");
                return;
            }

            var strings = new List<string>();
            foreach (var admin in gcfg.Admins) {
                var guild = (IGuild)Context.Guild;
                var user = await guild.GetUserAsync(admin);

                if (user == null)
                    strings.Add($"<unknown user #{admin}>");
                else
                    strings.Add(user.ToString());
            }

            var str = String.Join(", ", strings);
            await ReplyAsync($"Configured admins: {str}");
        }

        [Group("add")]
        [Summary("Add a new bot admin")]
        public class AdminAddModule : ModuleBase<SocketCommandContext> {
            public Storage Vars { get; set; }

            [Command("user")]
            [Summary("Add a specific user as a bot admin")]
            [RequireContext(ContextType.Guild)]
            public async Task AddUser([Summary("The admin to be added")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = Vars.GetGuild(guild);
                var guilduser = Context.Guild.GetUser(Context.User.Id);

                if (gcfg.Admins.Count() > 0 && gcfg.IsAdmin(guilduser)) {
                    await ReplyAsync("Sorry, only admins can use this command.");
                    return;
                }

                if (target == null) {
                    await ReplyAsync("That user doesn't seem to exist.");
                    return;
                }

                if (gcfg.AdminAdd(target))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That user is already an admin.");
                Vars.Save();
            }
        }

        [Group("remove")]
        [Summary("Remove a bot admin")]
        [RequireContext(ContextType.Guild)]
        public class AdminRemoveModule : ModuleBase<SocketCommandContext> {
            public Storage Vars { get; set; }

            [Command("user")]
            [Summary("Remove a specific user as a bot admin")]
            public async Task RemoveUser([Summary("The admin to be removed")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = Vars.GetGuild(guild);
                var guilduser = Context.Guild.GetUser(Context.User.Id);

                if (!gcfg.IsAdmin(guilduser)) {
                    await ReplyAsync("Sorry, only admins can use this command.");
                    return;
                }

                if (target == null) {
                    await ReplyAsync("That user doesn't seem to exist.");
                    return;
                }

                if (!gcfg.IsAdmin(target)) {
                    await ReplyAsync("That user is not an admin.");
                    return;
                }

                if (gcfg.Admins.Count() == 1) {
                    await ReplyAsync("You cannot remove the last admin, otherwise there would be none left.");
                    return;
                }

                gcfg.AdminRemove(target);
                Vars.Save();
            }
        }
    }
}
