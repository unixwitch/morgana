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
using System.IO;

namespace Morgana
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration config = null;

            try {
                var configfile = Path.Join(Directory.GetCurrentDirectory(), "config.ini");
                config = Configuration.Load(configfile);
            } catch (ConfigurationException e) {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }

            var bot = new Bot(config);
            bot.Run().GetAwaiter().GetResult();
        }
    }
}
