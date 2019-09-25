/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

#if false
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;

namespace Morgana {
    [Group("practice")]
    [Summary("Announce that you're going to practice")]
    public class PracticeModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command]
        [Summary("Announce that you're going to practice")]
        public async Task Practice() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (gcfg.PracticeChannel == 0) {
                await ReplyAsync("Sorry, I can't announce your practice because the practice channel hasn't been configured.");
                return;
            }

            if (gcfg.PracticeMessage == "") {
                await ReplyAsync("Sorry, I can't announce your practice because the practice message hasn't been configured.");
                return;
            }

            var channel = guild.GetTextChannel(gcfg.PracticeChannel);
            var text = gcfg.PracticeMessage.Replace("<user>", Context.User.Mention);
        }

    }
}
#endif