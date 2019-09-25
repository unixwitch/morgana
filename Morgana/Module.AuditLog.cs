/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Discord;
using Microsoft.VisualBasic;
using System.Net.Sockets;

namespace Morgana {

    [Group("audit")]
    [Summary("Configure the audit log")]
    public class AuditLogModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("channel")]
        [Summary("Set the audit log channel")]
        [RequireContext(ContextType.Guild)]
        public async Task Channel(IChannel channel) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            gcfg.AuditChannel = channel.Id;
            Vars.Save();
            await ReplyAsync("Done!");
        }

        [Command("enable")]
        [Summary("Enable the audit log")]
        [RequireContext(ContextType.Guild)]
        public async Task Enable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.AuditEnabled) {
                if (gcfg.AuditChannel == 0)
                    await ReplyAsync("The audit log is already enabled, but it won't work until a channel is configured.");
                else
                    await ReplyAsync("The audit log is already enabled.");
                return;
            }

            gcfg.AuditEnabled = true;
            Vars.Save();

            if (gcfg.AuditChannel == 0)
                await ReplyAsync("The audit log is now enabled, but it won't work until a channel is configured.");
            else
                await ReplyAsync("The audit log is now enabled.");
        }

        [Command("disable")]
        [Summary("Disable the audit log")]
        [RequireContext(ContextType.Guild)]
        public async Task Disable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (!gcfg.AuditEnabled) {
                await ReplyAsync("The audit log is already disabled.");
                return;
            }

            gcfg.AuditEnabled = false;
            Vars.Save();
            await ReplyAsync("The audit log is now disabled.");
        }

        [Command("status")]
        [Summary("Show whether the audit log is enabled or disabled")]
        [RequireContext(ContextType.Guild)]
        public async Task Status() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            var message = "The audit log is " + (gcfg.AuditEnabled ? "enabled." : "disabled.");
            if (gcfg.AuditChannel == 0)
                message += "  No channel has been configured.";
            else {
                var channel = guild.GetChannel(gcfg.AuditChannel);
                message += $"  The log will be sent to {channel}.";
            }

            await ReplyAsync(message);
        }
    }

    public class AuditLogger {
        public Storage Vars { get; set; }
        public DiscordSocketClient Client { get; set; }

        public AuditLogger(Storage vars, DiscordSocketClient client) {
            Vars = vars;
            Client = client;
        }

        public Task InitialiseAsync() {
            Client.MessageDeleted += MessageDeletedAsync;
            Client.MessageUpdated += MessageUpdatedAsync;
            Client.UserJoined += UserJoinedAsync;
            Client.UserLeft += UserLeftAsync;
            Client.UserBanned += UserBannedAsync;
            return Task.CompletedTask;
        }

        public async Task UserJoinedAsync(SocketGuildUser user) {
            var guild = user.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.AuditEnabled || gcfg.AuditChannel == 0)
                return;

            var usertext = $"{user.Username}#{user.Discriminator} [{user.Id}]";
            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
//                    .WithTitle("**User joined**")
                    .AddField("**User joined**", usertext)
                    .Build();

            var auditchannel = guild.GetTextChannel(gcfg.AuditChannel);
            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task UserLeftAsync(SocketGuildUser user) {
            var guild = user.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (gcfg.Admins.Contains(user.Id)) {
                gcfg.Admins.Remove(user.Id);
                Vars.Save();
            }

            if (!gcfg.AuditEnabled || gcfg.AuditChannel == 0)
                return;

            var usertext = $"{user.Username}#{user.Discriminator} [{user.Id}]";
            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
//                    .WithTitle("**User left**")
                    .AddField("**User left**", usertext)
                    .Build();

            var auditchannel = guild.GetTextChannel(gcfg.AuditChannel);
            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task UserBannedAsync(SocketUser user, SocketGuild guild) {
            var gcfg = Vars.GetGuild(guild);

            if (gcfg.Admins.Contains(user.Id)) {
                gcfg.Admins.Remove(user.Id);
                Vars.Save();
            }

            if (!gcfg.AuditEnabled || gcfg.AuditChannel == 0)
                return;

            var usertext = $"{user.Username}#{user.Discriminator} [{user.Id}]";
            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
//                    .WithTitle("**User banned**")
                    .AddField("**User banned**", usertext)
                    .Build();

            var auditchannel = guild.GetTextChannel(gcfg.AuditChannel);
            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> msg, ISocketMessageChannel ichannel) {
            var message = await msg.GetOrDownloadAsync();

            if (message == null)
                return;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            var guild = channel.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.AuditEnabled || gcfg.AuditChannel == 0)
                return;

            if (message.Author.Id == Client.CurrentUser.Id)
                return;

            var beforetext = message.ToString();
            foreach (var att in message.Attachments)
                beforetext += "\n" + att.Url;

            var user = $"{message.Author.Username}#{message.Author.Discriminator} [{message.Author.Id}]";

            var embed =
                new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithTitle("**Message deleted**")
                    .AddField("**User**", user)
                    .AddField("**Message**", beforetext)
                    .Build();

            var auditchannel = guild.GetTextChannel(gcfg.AuditChannel);
            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel ichannel) {
            var beforeMessage = await before.GetOrDownloadAsync();

            if (beforeMessage == null)
                return;

            var channel = beforeMessage.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            var guild = channel.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.AuditEnabled || gcfg.AuditChannel == 0)
                return;

            if (beforeMessage.Author.Id == Client.CurrentUser.Id)
                return;

            var beforetext = beforeMessage.ToString();
            foreach (var att in beforeMessage.Attachments)
                beforetext += "\n" + att.Url;
            if (beforetext.Length > 900)
                beforetext = beforetext.Substring(0, 900);

            var aftertext = after.ToString();
            foreach (var att in after.Attachments)
                aftertext += "\n" + att.Url;
            if (aftertext.Length > 900)
                aftertext = aftertext.Substring(0, 900);

            var user = $"{beforeMessage.Author.Username}#{beforeMessage.Author.Discriminator} [{beforeMessage.Author.Id}]";

            var embed =
                new EmbedBuilder()
                    .WithAuthor(beforeMessage.Author)
                    .WithTitle("**Message edited**")
                    .AddField("**User**", user)
                    .AddField("**Before**", beforetext)
                    .AddField("**After**", aftertext)
                    .Build();

            var auditchannel = guild.GetTextChannel(gcfg.AuditChannel);
            await auditchannel.SendMessageAsync(embed: embed);
        }
    }
}