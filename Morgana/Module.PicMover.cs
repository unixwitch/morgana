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
        public PicMover Mover { get; set; }

        [Command("from")]
        [Summary("Set the channel to look for pinned pictures in")]
        [RequireBotAdmin]
        public async Task From(ITextChannel channel) {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            await gcfg.SetPinFromAsync(channel);
            await ReplyAsync("Done!");
        }

        [Command("to")]
        [Summary("Set the channel to move pinned pictures to")]
        [RequireBotAdmin]
        public async Task To(ITextChannel channel) {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            await gcfg.SetPinToAsync(channel);
            await ReplyAsync("Done!");
        }

        [Command("enable")]
        [Summary("Enable the pic mover")]
        [RequireBotAdmin]
        public async Task Enable() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (await gcfg.IsPinnerEnabledAsync())
                await ReplyAsync("The pinned picture mover is already enabled.");
            else {
                await gcfg.SetPinnerEnabledAsync(true);
                await ReplyAsync("Done!");
            }
        }

        [Command("disable")]
        [Summary("Disable the pic mover")]
        [RequireBotAdmin]
        public async Task Disable() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!await gcfg.IsPinnerEnabledAsync())
                await ReplyAsync("The pinned picture mover is already disabled.");
            else {
                await gcfg.SetPinnerEnabledAsync(false);
                await ReplyAsync("Done!");
            }
        }

        [Command("check")]
        [Summary("Check for outstanding pins to move")]
        [RequireBotAdmin]
        public async Task Check() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!await gcfg.IsPinnerEnabledAsync())
                await ReplyAsync("The pinned picture mover is not enabled on this server.");
            else {
                await ReplyAsync("Okay, I'll take a look.");
                await Mover.CheckPinsForGuild(guild);
            }
        }

        [Command("status")]
        [Summary("Show the pic mover configuration")]
        [RequireBotAdmin]
        public async Task Status() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            string status = "The picture mover is " + (await gcfg.IsPinnerEnabledAsync() ? "enabled." : "disabled.");

            var fromchan = await gcfg.GetPinFromAsync();
            var tochan = await gcfg.GetPinToAsync();

            if (fromchan != null && tochan != null)
                status += $"  Pictures will be moved from {MentionUtils.MentionChannel(fromchan.Id)} to {MentionUtils.MentionChannel(tochan.Id)}.";
            else if (fromchan != null)
                status += $"  Pictures would be moved from {MentionUtils.MentionChannel(fromchan.Id)}, but no destination channel is configured.";
            else if (tochan != null)
                status += $"  Pictures would be moved to {MentionUtils.MentionChannel(tochan.Id)}, but no source channel is configured.";
            else
                status += " Neither the source nor destination channels are configured.";

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

        public async Task CheckPinsForGuild(IGuild guild) {
            var gcfg = Vars.GetGuild(guild);

            if (!await gcfg.IsPinnerEnabledAsync())
                return;

            var fromchan = await gcfg.GetPinFromAsync();
            var tochan = await gcfg.GetPinToAsync();

            if (fromchan == null || tochan == null)
                return;

            var pins = await fromchan.GetPinnedMessagesAsync();
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

            if (!await gcfg.IsPinnerEnabledAsync())
                return;

            var fromchan = await gcfg.GetPinFromAsync();
            if (fromchan.Id != msg.Channel.Id)
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

            if (!await gcfg.IsPinnerEnabledAsync())
                return;

            var fromchan = await gcfg.GetPinFromAsync();
            var tochan = await gcfg.GetPinToAsync();

            if (fromchan == null || tochan == null)
                return;

            var channel = Client.GetChannel(msg.ChannelID) as ITextChannel;
            if (channel == null)
                return;

            var message = await channel.GetMessageAsync(msg.MessageID);
            if (message.Channel.Id != fromchan.Id)
                return;
            if (!(message is IUserMessage umsg))
                return;

            if (!umsg.IsPinned)
                return;

            if (umsg.Attachments.Count() == 0)
                return;

            await umsg.UnpinAsync();

            var first = message.Attachments.First();
            var rest = message.Attachments.Skip(1);
            var text = message.Content;
            var user = message.Author.ToString();

            var firstEmbed =
                new EmbedBuilder()
                    .WithDescription($"From {Format.Sanitize(user)}: {text}")
                    .WithImageUrl(first.Url)
                    .Build();

            await tochan.SendMessageAsync(embed: firstEmbed);

            foreach (var att in rest) {
                var embed =
                    new EmbedBuilder()
                        .WithDescription($"From {Format.Sanitize(user)} (cont)")
                        .WithImageUrl(att.Url)
                        .Build();
                await tochan.SendMessageAsync(embed: embed);
            }
        }
    }
}
