﻿/*
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
        public async Task SpellCheckAsync([Summary("The word to check")] string word) {
            if (Context.Guild != null) {
                var gcfg = Vars.GetGuild(Context.Guild);
                if (await gcfg.IsBadwordsEnabledAsync() && await gcfg.IsBadwordAsync(word))
                    return;
            }

            var results = (await Speller.CheckWord(word)).Take(10).ToList();
            var str = Format.Sanitize(string.Join(", ", results.Select(r => r.term)));

            if (results.Count() == 0)
                await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, I couldn't find any suggestions for that word.");
            else if (results[0].term == word)
                await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, \"{Format.Sanitize(word)}\" seems to be spelt correctly.  Other suggestions I found: {str}.");
            else
                await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)}, suggestions for \"{word}\": {str}.");
        }
    }

    public class SpellingService {
        private SymSpell _speller;

        public SpellingService() {
            _speller = new SymSpell(82765, 3);
            var assembly = typeof(Morgana.SpellingService).GetTypeInfo().Assembly;

            Stream dict = assembly.GetManifestResourceStream("Morgana.dictionary_en.txt");
            _speller.LoadDictionary(dict, 0, 1);

            //Stream bigram_dict = assembly.GetManifestResourceStream("Morgana.bigram_dictionary_en");
            //_speller.LoadBigramDictionary(bigram_dict, 0, 2);
        }

        public Task<List<SymSpell.SuggestItem>> CheckWord(string word) {
            return Task.Run(() => _speller.Lookup(word, SymSpell.Verbosity.All, 3));
        }
    }
}
