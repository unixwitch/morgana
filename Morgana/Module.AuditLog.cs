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
        [RequireBotAdmin]
        public async Task Channel(ITextChannel channel) {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            await gcfg.SetAuditChannelAsync(channel);
            await ReplyAsync("Done!");
        }

        [Command("enable")]
        [Summary("Enable the audit log")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Enable() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            var chan = await gcfg.GetAuditChannelAsync();

            if (await gcfg.IsAuditEnabledAsync()) {
                if (chan == null)
                    await ReplyAsync("The audit log is already enabled, but it won't work until a channel is configured.");
                else
                    await ReplyAsync("The audit log is already enabled.");
                return;
            }

            await gcfg.SetAuditEnabledAsync(true);

            if (chan == null)
                await ReplyAsync("The audit log is now enabled, but it won't work until a channel is configured.");
            else
                await ReplyAsync("The audit log is now enabled.");
        }

        [Command("disable")]
        [Summary("Disable the audit log")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Disable() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (!await gcfg.IsAuditEnabledAsync()) {
                await ReplyAsync("The audit log is already disabled.");
                return;
            }

            await gcfg.SetAuditEnabledAsync(false);
            await ReplyAsync("The audit log is now disabled.");
        }

        [Command("status")]
        [Summary("Show whether the audit log is enabled or disabled")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Status() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            var enabled = await gcfg.IsAuditEnabledAsync();
            var chan = await gcfg.GetAuditChannelAsync();

            var message = "The audit log is " + (enabled ? "enabled." : "disabled.");
            if (chan == null)
                message += "  No channel has been configured.";
            else {
                message += $"  The log will be sent to {MentionUtils.MentionChannel(chan.Id)}.";
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
            Client.GuildMemberUpdated += GuildMemberUpdatedAsync;
            return Task.CompletedTask;
        }

        protected async Task<ITextChannel> GetAuditChannelForGuildAsync(SocketGuild guild) {
            var gcfg = Vars.GetGuild(guild);

            var enabled = await gcfg.IsAuditEnabledAsync();
            if (!enabled)
                return null;

            var chan = await gcfg.GetAuditChannelAsync();
            if (chan == null)
                return null;

            return chan;
        }

        public async Task GuildMemberUpdatedAsync(SocketGuildUser before, SocketGuildUser after) {
            var guild = before.Guild;

            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;

            if (before.Nickname != after.Nickname) {
                await auditchannel.SendMessageAsync(embed:
                    new EmbedBuilder()
                        .WithAuthor(after)
                        .WithTitle("User nickname changed")
                        .AddField("**Old nickname**", Format.Sanitize(before.Nickname ?? before.Username), true)
                        .AddField("**New nickname**", Format.Sanitize(after.Nickname ?? after.Username), true)
                        .WithFooter($"User ID: {after.Id}")
                        .Build());
            }

            if (!before.Roles.Select(r => r.Id).OrderBy(id => id).SequenceEqual(after.Roles.Select(r => r.Id).OrderBy(id => id))) {
                await auditchannel.SendMessageAsync(embed:
                    new EmbedBuilder()
                        .WithAuthor(after)
                        .WithTitle("User roles changed")
                        .AddField("**Old role list**", String.Join(", ", before.Roles.Select(r => $"`{Format.Sanitize(r.Name)}`")))
                        .AddField("**New role list**", String.Join(", ", after.Roles.Select(r => $"`{Format.Sanitize(r.Name)}`")))
                        .WithFooter($"User ID: {after.Id}")
                        .Build());
            }
        }

        public async Task UserJoinedAsync(SocketGuildUser user) {
            var guild = user.Guild;

            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;

            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle("**User joined**")
                    .WithFooter($"User ID: {user.Id}")
                    .Build();

            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task UserLeftAsync(SocketGuildUser user) {
            var guild = user.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (await gcfg.IsAdminUserAsync(user.Id)) {
                await gcfg.AdminUserRemoveAsync(user.Id);
            }

            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;

            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle("**User left**")
                    .WithFooter($"User ID: {user.Id}")
                    .Build();

            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task UserBannedAsync(SocketUser user, SocketGuild guild) {
            var gcfg = Vars.GetGuild(guild);
            var guser = user as SocketGuildUser;

            if (await gcfg.IsAdminUserAsync(guser.Id)) {
                await gcfg.AdminUserRemoveAsync(guser.Id);
            }

            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;

            var embed =
                new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle("**User banned**")
                    .WithFooter($"User ID: {user.Id}")
                    .Build();

            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> msg, ISocketMessageChannel ichannel) {
            var message = await msg.GetOrDownloadAsync();

            if (!(message.Channel is SocketGuildChannel channel))
                return;

            var guild = channel.Guild;

            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;

            if (message == null)
                return;

            if (message.Author.Id == Client.CurrentUser.Id)
                return;

            var beforetext = message.ToString();
            foreach (var att in message.Attachments)
                beforetext += "\n" + att.Url;

            var embed =
                new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .AddField("**Message deleted**", "In " + MentionUtils.MentionChannel(message.Channel.Id))
                    .AddField("**Message**", Format.Sanitize(beforetext))
                    .WithFooter($"User ID: {message.Author.Id}")
                    .Build();

            await auditchannel.SendMessageAsync(embed: embed);
        }

        public async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel ichannel) {
            var beforeMessage = await before.GetOrDownloadAsync();

            if (beforeMessage == null)
                return;

            var channel = beforeMessage.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            if (beforeMessage.Content == after.Content)
                return;

            var guild = channel.Guild;
            var auditchannel = await GetAuditChannelForGuildAsync(guild);
            if (auditchannel == null)
                return;


            if (beforeMessage.Author.Id == Client.CurrentUser.Id)
                return;

            var beforetext = beforeMessage.ToString();
            foreach (var att in beforeMessage.Attachments)
                beforetext += "\n" + att.Url;
            if (beforetext == null)
                beforetext = "<could not retrieve message>";

            if (beforetext.Length > 900)
                beforetext = beforetext.Substring(0, 900);

            var aftertext = after.ToString();
            foreach (var att in after.Attachments)
                aftertext += "\n" + att.Url;
            if (aftertext.Length > 900)
                aftertext = aftertext.Substring(0, 900);

            var embed =
                new EmbedBuilder()
                    .WithAuthor(beforeMessage.Author)
                    .AddField("**Message edited**", "In " + MentionUtils.MentionChannel(after.Channel.Id))
                    .AddField("**Before**", Format.Sanitize(beforetext ?? "<unknown>"))
                    .AddField("**After**", Format.Sanitize(aftertext ?? "<unknown>"))
                    .WithFooter($"User ID: {after.Author.Id}")
                    .Build();

            await auditchannel.SendMessageAsync(embed: embed);
        }
    }
}