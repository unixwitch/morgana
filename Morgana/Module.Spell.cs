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
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Diagnostics;
using CacheManager.Core;
using EFSecondLevelCache.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.IO;

namespace Morgana {
    public class SpellModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }
        public SpellingService Speller { get; set; }

        [Command("spell", RunMode = RunMode.Async)]
        [Summary("Check the spelling of a word")]
        public async Task SpellCheckAsync([Summary("The word or sentence to check")][Remainder] string word) {
            if (word.IndexOfAny(new char[] { ' ', '\n', '\r', '\t' }) == -1) {
                List<string> badwords = new List<string>();
                if (Context.Guild != null) {
                    var gcfg = Vars.GetGuild(Context.Guild);
                    badwords = await gcfg.GetBadwordsAsync();
                }

                var results = (await Speller.CheckWord(word)).Where(w => !badwords.Contains(w.term)).Select(w => w.term).Take(10).ToList();
                if (results.Count() == 0)
                    await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, I couldn't find any suggestions for \"{Format.Sanitize(word)}\".");
                else if (results[0] == word)
                    await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, \"{Format.Sanitize(word)}\" seems to be spelt correctly.  Other suggestions I found: {string.Join(", ", results.Skip(1).Select(w => "`" + Format.Sanitize(w) + "`"))}.");
                else
                    await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, suggestions for \"{Format.Sanitize(word)}\": {string.Join(", ", results.Select(w => "`" + Format.Sanitize(w) + "`"))}.");
            } else {
                var result = string.Join(" ", (await Speller.CheckSentence(word)).Select(w => w.term));
                await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, how about this: `{Format.Sanitize(result)}`");
            }
        }
    }

    public class SpellingService {
        private SymSpell _speller;

        public SpellingService() {
            _speller = new SymSpell(82765, 3);
            var assembly = typeof(Morgana.SpellingService).GetTypeInfo().Assembly;

            Stream dict = assembly.GetManifestResourceStream("Morgana.dictionary_en.txt");
            _speller.LoadDictionary(dict, 0, 1);

            Stream bigram_dict = assembly.GetManifestResourceStream("Morgana.bigram_dictionary_en.txt");
            _speller.LoadBigrams(bigram_dict, 0, 2);
        }

        public Task<List<SymSpell.SuggestItem>> CheckWord(string word) {
            return Task.Run(() => _speller.Lookup(word, SymSpell.Verbosity.All, 3));
        }
        public Task<List<SymSpell.SuggestItem>> CheckSentence(string word) {
            return Task.Run(() => _speller.LookupCompound(word, 3));
        }
    }
}
