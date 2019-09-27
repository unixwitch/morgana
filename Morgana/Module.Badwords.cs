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

namespace Morgana {

    [Group("badwords")]
    [Summary("Configure the bad words filter")]
    public class BadwordsModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("list")]
        [Summary("List the current configured bad words")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.BadwordsList.Count() == 0) {
                await ReplyAsync("No bad words are configured.");
                return;
            }

            var words = String.Join(", ", gcfg.BadwordsList.Select(x => $"`{x}`"));
            await ReplyAsync($"Current bad words list: {words}.");
        }

        [Command("add")]
        [Summary("Add a new bad word")]
        [RequireContext(ContextType.Guild)]
        public async Task Add([Summary("The bad word to add")] string word) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.BadwordAdd(word)) {
                await ReplyAsync("Done!");
                Vars.Save();
            } else
                await ReplyAsync("That word was already on the bad words list.");
        }

        [Command("remove")]
        [Summary("Remove a bad word")]
        [RequireContext(ContextType.Guild)]
        public async Task Remove([Summary("The bad word to remove")] string word) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.BadwordRemove(word)) {
                await ReplyAsync("Done!");
                Vars.Save();
            } else
                await ReplyAsync("That word is not on the bad words list.");
        }

        [Command("enable")]
        [Summary("Enable the bad words filter")]
        [RequireContext(ContextType.Guild)]
        public async Task Enable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (gcfg.BadwordsEnabled) {
                await ReplyAsync("The bad words filter is already enabled.");
                return;
            }

            gcfg.BadwordsEnabled = true;
            Vars.Save();
            await ReplyAsync("The bad words filter is now enabled.");
        }

        [Command("disable")]
        [Summary("Disable the bad words filter")]
        [RequireContext(ContextType.Guild)]
        public async Task Disable() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (!gcfg.BadwordsEnabled) {
                await ReplyAsync("The bad words filter is already disabled.");
                return;
            }

            gcfg.BadwordsEnabled = false;
            Vars.Save();
            await ReplyAsync("The bad words filter is now disabled.");
        }

        [Command("status")]
        [Summary("Show whether the bad words filter is enabled or disabled")]
        [RequireContext(ContextType.Guild)]
        public async Task Status() {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            await ReplyAsync("The bad words filter is " + (gcfg.BadwordsEnabled ? "enabled." : "disabled."));
        }

        [Command("message")]
        [Summary("Set the message used to admonish a user")]
        [RequireContext(ContextType.Guild)]
        public async Task Message([Summary("The message text; use <user> to tag the username")] string message = null) {
            var guild = Context.Guild;
            var guildUser = guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(guild);

            if (!gcfg.IsAdmin(guildUser)) {
                await ReplyAsync("Sorry, this command can only be used by admins.");
                return;
            }

            if (message == null) {
                if (gcfg.BadwordsMessage == null)
                    await ReplyAsync("No badwords message is set.");
                else
                    await ReplyAsync("The current badwords message is: " + gcfg.BadwordsMessage);
                return;
            }
            gcfg.BadwordsMessage = message;
            Vars.Save();
            await ReplyAsync("Done!");
        }
    }

    public class BadwordsFilter {
        public Storage Vars { get; set; }
        public DiscordSocketClient Client { get; set; }

        public BadwordsFilter(Storage vars, DiscordSocketClient client) {
            Vars = vars;
            Client = client;
        }

        public Task InitialiseAsync() {
//            Client.MessageReceived += FilterMessageAsync;
            return Task.CompletedTask;
        }

        public async Task<bool> FilterMessageAsync(SocketMessage p) {
            var message = p as SocketUserMessage;
            if (message == null)
                return false;

            if (message.Author.IsBot)
                return false;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null)
                return false;

            var gcfg = Vars.GetGuild(channel.Guild);
            if (!gcfg.BadwordsEnabled)
                return false;

            var bw = gcfg.CommandPrefix + "badwords";
            if (message.Content.StartsWith(bw))
                return false;

            var re = new Regex(@"\b");
            var words = re.Split(message.Content);

            foreach (var word in words) {
                if (gcfg.IsBadword(word)) {
                    await message.DeleteAsync();

                    var msg = gcfg.BadwordsMessage;
                    if (msg == null)
                        msg = "<user>, please refrain from swearing on this server.  Thanks!";

                    msg = msg.Replace("<user>", message.Author.Mention);
                    await message.Channel.SendMessageAsync(msg);
                    return true;
                }
            }

            return false;
        }
    }
}