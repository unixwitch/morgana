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
using Microsoft.Extensions.DependencyInjection;
using System.Resources;

namespace Morgana {

    [Group("badwords")]
    [Summary("Configure the bad words filter")]
    public class BadwordsModule : ModuleBase<SocketCommandContext> {
        public StorageContext DB { get; set; }

        [Command("list")]
        [Summary("List the current configured bad words")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task List() {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);

            var words = await gcfg.GetBadwordsAsync();
            if (!words.Any()) {
                await ReplyAsync("No bad words are configured.");
                return;
            }

            var str = string.Join(", ", 
                words.Select(x => 
                    x.IsRegex ? $"`/{Format.Sanitize(x.Badword)}/`" : $"`{Format.Sanitize(x.Badword)}`"));
            await ReplyAsync($"Current bad words list: {str}.");
        }

        [Command("add")]
        [Summary("Add a new bad word")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Add([Summary("The bad words to add")] params string[] words) {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            var existing = new List<(string, bool)>();
            int added = 0;

            var bwords = new List<(string, bool)>();
            foreach (var word in words.Select(w => w.ToLower())) {
                if (word.StartsWith('/')) {
                    if (!word.EndsWith('/')) {
                        await ReplyAsync($"{mu}, I couldn't add `{Format.Sanitize(word)}`, because it started with a " +
                            $"slash but did not end with a slash.  If you wanted to add a regex, use `/word/`." +
                            $"  Otherwise, remove the slash as it will never match anyway.");
                        return;
                    }

                    var rw = word.Substring(1, word.Length - 2);
                    bwords.Add((rw, true));
                } else {
                    bwords.Add((word, false));
                }
            }

            foreach (var bw in bwords) {
                var (w, isregex) = bw;
                if (await gcfg.BadwordAddAsync(w, isregex))
                    added++;
                else
                    existing.Add(bw);
            }

            if (existing.Count() == 0)
                await ReplyAsync($"{mu}, I added {added} words to the bad words list.");
            else {
                var existingstr = string.Join(", ", 
                    existing.Select(x => {
                        var (w, isregex) = x;
                        return isregex ? $"`/{Format.Sanitize(w)}/`" : $"`{Format.Sanitize(w)}`";
                    }));
                await ReplyAsync($"{mu}, I added {added} words to the bad words list.  The following words are already in the filter: {existingstr}.");
            }
        }

        [Command("remove")]
        [Summary("Remove a bad word")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Remove([Summary("The bad words to remove")] params string[] words) {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            var notfound = new List<(string, bool)>();
            int removed = 0;

            foreach (var word in words.Select(w => w.ToLower())) {
                if (word.StartsWith('/')) {
                    if (!word.EndsWith('/')) {
                        notfound.Add((word, true));
                    } else {
                        var rw = word.Substring(1, word.Length - 2);
                        if (await gcfg.BadwordRemoveAsync(rw, true))
                            removed++;
                        else
                            notfound.Add((rw, true));
                    }
                } else {
                    if (await gcfg.BadwordRemoveAsync(word, false))
                        removed++;
                    else
                        notfound.Add((word, false));
                }
            }

            if (notfound.Count() == 0)
                await ReplyAsync($"{mu}, I removed {removed} words from the bad words list.");
            else {
                var notfoundstr = string.Join(", ",
                    notfound.Select(x => {
                        var (w, isregex) = x;
                        return isregex ? $"`/{Format.Sanitize(w)}/`" : $"`{Format.Sanitize(w)}`";
                    }));
                await ReplyAsync($"Removed {removed} words from the bad words list.  The following words are not in the filter: {notfoundstr}.");
            }
        }

        [Command("enable")]
        [Summary("Enable the bad words filter")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Enable() {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);

            if (await gcfg.IsBadwordsEnabledAsync()) {
                await ReplyAsync("The bad words filter is already enabled.");
                return;
            }

            await gcfg.SetBadwordsEnabledAsync(true);
            await ReplyAsync("The bad words filter is now enabled.");
        }

        [Command("disable")]
        [Summary("Disable the bad words filter")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Disable() {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);

            if (!await gcfg.IsBadwordsEnabledAsync()) {
                await ReplyAsync("The bad words filter is already disabled.");
                return;
            }

            await gcfg.SetBadwordsEnabledAsync(true);
            await ReplyAsync("The bad words filter is now disabled.");
        }

        [Command("status")]
        [Summary("Show whether the bad words filter is enabled or disabled")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Status() {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);

            await ReplyAsync("The bad words filter is " + (await gcfg.IsBadwordsEnabledAsync() ? "enabled." : "disabled."));
        }

        [Command("message")]
        [Summary("Set the message used to admonish a user")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Message([Summary("The message text; use <user> to tag the username")][Remainder] string message = null) {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(guild);

            if (message == null) {
                var msg = await gcfg.GetBadwordsMessageAsync();
                if (msg == null)
                    await ReplyAsync("No badwords message is set.");
                else
                    await ReplyAsync("The current badwords message is: " + msg);
                return;
            }

            await gcfg.SetBadwordsMessageAsync(message);
            await ReplyAsync("Done!");
        }
    }

    public class BadwordsFilter {
        private IServiceProvider _svcs;
        public DiscordSocketClient Client { get; set; }

        public BadwordsFilter(IServiceProvider svcs, DiscordSocketClient client) {
            _svcs = svcs;
            Client = client;
        }

        public Task InitialiseAsync() {
            return Task.CompletedTask;
        }

        public async Task<bool> FilterMessageAsync(SocketMessage p) {
            var db = _svcs.GetRequiredService<StorageContext>();

            var message = p as SocketUserMessage;
            var guilduser = message.Author as SocketGuildUser;

            if (message == null)
                return false;

            if (message.Author.IsBot)
                return false;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null)
                return false;

            var gcfg = db.GetGuild(channel.Guild);
            if (!await gcfg.IsBadwordsEnabledAsync())
                return false;

            var bw = await gcfg.GetCommandPrefixAsync() + "badwords";
            if (message.Content.StartsWith(bw))
                return false;

            if (await gcfg.IsAdminAsync(guilduser))
                return false;

            var re = new Regex(@"\b");
            var words = re.Split(message.Content);

            if (await gcfg.MatchAnyBadwordAsync(words)) {
                await message.DeleteAsync();

                var msg = await gcfg.GetBadwordsMessageAsync();
                if (msg == null)
                    msg = "<user>, please refrain from swearing on this server.  Thanks!";

                msg = msg.Replace("<user>", message.Author.Mention);
                await message.Channel.SendMessageAsync(msg);
                return true;
            }

            return false;
        }
    }
}