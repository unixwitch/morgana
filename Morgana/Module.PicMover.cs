/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Morgana {
    [Group("picmover")]
    [Summary("Configure the pinned picture mover")]
    public class PicMoverModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("from")]
        [Summary("Set the channel to look for pinned pictures in")]
        public async Task From(ITextChannel channel) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            gcfg.PinFrom = channel.Id;
            Vars.Save();
            await ReplyAsync("Done!");
        }

        [Command("to")]
        [Summary("Set the channel to move pinned pictures to")]
        public async Task To(ITextChannel channel) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            gcfg.PinTo = channel.Id;
            Vars.Save();
            await ReplyAsync("Done!");
        }

        [Command("enable")]
        [Summary("Enable the pic mover")]
        public async Task Enable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.DoPins)
                await ReplyAsync("The pinned picture mover is already enabled.");
            else {
                gcfg.DoPins = true;
                Vars.Save();
                await ReplyAsync("Done!");
            }
        }

        [Command("disable")]
        [Summary("Disable the pic mover")]
        public async Task Disable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (!gcfg.DoPins)
                await ReplyAsync("The pinned picture mover is already disabled.");
            else {
                gcfg.DoPins = false;
                Vars.Save();
                await ReplyAsync("Done!");
            }
        }

        [Command("status")]
        [Summary("Show the pic mover configuration")]
        public async Task Status() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            string status = "The picture mover is " + (gcfg.DoPins ? "enabled." : "disabled.");

            IGuildChannel fromchan = null;
            if (gcfg.PinFrom != 0)
                fromchan = guild.GetTextChannel(gcfg.PinFrom);

            IGuildChannel tochan = null;
            if (gcfg.PinTo != 0)
                tochan = guild.GetTextChannel(gcfg.PinTo);

            if (fromchan != null && tochan != null)
                status += $"  Pictures will be moved from {MentionUtils.MentionChannel(fromchan.Id)} to {MentionUtils.MentionChannel(tochan.Id)}.";
            else if (fromchan != null)
                status += $"  Pictures would be moved from {MentionUtils.MentionChannel(fromchan.Id)}, but no destination channel is configured.";
            else if (tochan != null)
                status += $"  Pictures would be moved to {MentionUtils.MentionChannel(tochan.Id)}, but no source channel is configured.";
            else
                status += "Neither the source nor destination channels are configured.";

            await ReplyAsync(status);
        }
    }

    public class PicMover {
        public Storage Vars { get; set; }
        public DiscordSocketClient Client { get; set; }

        public PicMover(Storage vars, DiscordSocketClient client) {
            Vars = vars;
            Client = client;
        }

        public Task InitialiseAsync() {
            Client.MessageUpdated += MessageUpdatedAsync;
            return Task.CompletedTask;
        }

        public async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel ichannel) {
            if (!after.IsPinned)
                return;

            if (after.Attachments.Count() == 0)
                return;

            var message = after as SocketUserMessage;
            if (message == null)
                return;

            var channel = after.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            var guild = channel.Guild;
            var gcfg = Vars.GetGuild(guild);
            if (!gcfg.DoPins || (gcfg.PinFrom == 0) || (gcfg.PinTo == 0))
                return;

            if (channel.Id != gcfg.PinFrom)
                return;

            var tochan = guild.GetTextChannel(gcfg.PinTo);
            if (tochan == null)
                return;

            await message.UnpinAsync();

            var first = after.Attachments.First();
            var rest = after.Attachments.Skip(1);
            var text = after.Content;
            var user = after.Author.ToString();

            var firstEmbed =
                new EmbedBuilder()
                    .WithDescription($"From {user}: {text}")
                    .WithImageUrl(first.Url)
                    .Build();

            await tochan.SendMessageAsync(embed: firstEmbed);

            foreach (var att in rest) {
                var embed =
                    new EmbedBuilder()
                        .WithDescription($"From {user} (cont)")
                        .WithImageUrl(att.Url)
                        .Build();
                await tochan.SendMessageAsync(embed: embed);
            }
        }
    }
}
