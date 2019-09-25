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
using Microsoft.Extensions.Configuration;

namespace Morgana {
    public class ConfigurationException : Exception {
        public ConfigurationException(string reason) : base(reason) { }
    } 

    public class Configuration {
        public string Token { get; set; }

        public static Configuration Load(string filename) {
            IConfigurationRoot config;
            try {
                config = new ConfigurationBuilder().AddIniFile(filename).Build();
            } catch (Exception e) {
                throw new ConfigurationException($"{filename}: {e.Message}");
            }

            Configuration cfg = new Configuration {
                Token = config["auth:token"]
            };

            if (cfg.Token == null)
                throw new ConfigurationException($"{filename}: missing required option auth:token");

            return cfg;
        }
    }
}