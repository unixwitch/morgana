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
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Channels;

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

        protected struct PinnedMessage {
            public ulong GuildID;
            public ulong ChannelID;
            public ulong MessageID;
        };

        private Channel<PinnedMessage> pendingChan = Channel.CreateUnbounded<PinnedMessage>();

        public PicMover(Storage vars, DiscordSocketClient client) {
            Vars = vars;
            Client = client;
        }

        public Task InitialiseAsync() {
            Client.MessageUpdated += MessageUpdatedAsync;
            _ = CheckPins();
            _ = RunPinner();
            return Task.CompletedTask;
        }

        protected async Task CheckPins() {
            for (; ;) {
                await Task.Delay(TimeSpan.FromSeconds(3600));

                if (Client.ConnectionState != ConnectionState.Connected)
                    continue;

                foreach (var guild in Client.Guilds) {
                    try {
                        await CheckPinsForGuild(guild);
                    } catch (Exception e) {
                        Console.WriteLine("CheckPinsForGuild failed: " + e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }
            }
        }

        protected async Task CheckPinsForGuild(IGuild guild) {
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.DoPins || (gcfg.PinFrom == 0) || (gcfg.PinTo == 0))
                return;

            var fromchannel = await guild.GetTextChannelAsync(gcfg.PinFrom);
            var pins = await fromchannel.GetPinnedMessagesAsync();
            foreach (var message in pins) {
                await ConsiderPinning(guild, message);
            }
        }

        protected async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel ichannel) {
            var channel = after.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            var guild = channel.Guild;
            await ConsiderPinning(guild, after);
        }

        protected async Task ConsiderPinning(IGuild guild, IMessage msg) {
            var gcfg = Vars.GetGuild(guild);
            if (!gcfg.DoPins || gcfg.PinFrom != msg.Channel.Id)
                return;

            if (!(msg is IUserMessage umsg))
                return;

            if (!msg.IsPinned)
                return;

            if (msg.Attachments.Count() == 0)
                return;

            await pendingChan.Writer.WriteAsync(new PinnedMessage {
                GuildID = guild.Id,
                ChannelID = msg.Channel.Id,
                MessageID = msg.Id
            });
        }

        protected async Task RunPinner() {
            for (; ;) {
                var msg = await pendingChan.Reader.ReadAsync();
                try {
                    await PinMessage(msg);
                } catch (Exception e) {
                    Console.WriteLine("Pinner failed: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        protected async Task PinMessage(PinnedMessage msg) {
            var guild = Client.GetGuild(msg.GuildID);
            var gcfg = Vars.GetGuild(guild);
            if (!gcfg.DoPins || (gcfg.PinFrom == 0) || (gcfg.PinTo == 0))
                return;

            var channel = Client.GetChannel(msg.ChannelID) as ITextChannel;
            if (channel == null)
                return;

            var message = await channel.GetMessageAsync(msg.MessageID);
            if (message.Channel.Id != gcfg.PinFrom)
                return;
            if (!(message is IUserMessage umsg))
                return;

            if (!umsg.IsPinned)
                return;

            if (umsg.Attachments.Count() == 0)
                return;

            if (channel.Id != gcfg.PinFrom)
                return;

            var tochan = guild.GetTextChannel(gcfg.PinTo);
            if (tochan == null)
                return;

            await umsg.UnpinAsync();

            var first = message.Attachments.First();
            var rest = message.Attachments.Skip(1);
            var text = message.Content;
            var user = message.Author.ToString();

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
