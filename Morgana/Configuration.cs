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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Morgana {
    public class ConfigurationException : Exception {
        public ConfigurationException(string reason) : base(reason) { }
    }

    public class Configuration {
        public string Token { get; set; }
        public ulong? InitialOwner { get; set; }

        public enum DBProviderType {
            SQLite,
            MSSQL,
            PGSQL,
            MySQL
        }

        public DBProviderType DbProvider { get; set; }
        public string DbConnection { get; set; }

        public void ConfigureDb(DbContextOptionsBuilder options) {
            switch (DbProvider) {
                case DBProviderType.SQLite:
                    options.UseSqlite(DbConnection);
                    break;

                case DBProviderType.MSSQL:
                    options.UseSqlServer(DbConnection);
                    break;

                case DBProviderType.MySQL:
                    options.UseMySQL(DbConnection);
                    break;

                case DBProviderType.PGSQL:
                    options.UseNpgsql(DbConnection);
                    break;
            }
        }

        public static Configuration Load(string filename) {
            IConfigurationRoot config;
            try {
                config = new ConfigurationBuilder().AddIniFile(filename).Build();
            } catch (Exception e) {
                throw new ConfigurationException($"{filename}: {e.Message}");
            }

            Configuration cfg = new Configuration {
                Token = config["general:token"],
                DbConnection = config["database:connection"]
            };

            var owner = config["general:initial_owner"];
            if (owner != null) {
                try {
                    cfg.InitialOwner = ulong.Parse(owner);
                } catch (FormatException) {
                    throw new ConfigurationException($"{filename}: invalid initial_owner '{owner}' (expected a user id)");
                }
            }

            if (cfg.Token == null)
                throw new ConfigurationException($"{filename}: missing required option general:token");

            if (cfg.DbConnection == null)
                throw new ConfigurationException($"{filename}: missing required option database:connection");

            switch (config["database:type"]) {
                case null:
                    throw new ConfigurationException($"{filename}: missing required option database:type");

                case "sqlite":
                    cfg.DbProvider = DBProviderType.SQLite;
                    break;
                case "pgsql":
                    cfg.DbProvider = DBProviderType.PGSQL;
                    break;
                case "mysql":
                    cfg.DbProvider = DBProviderType.MySQL;
                    break;
                case "mssql":
                    cfg.DbProvider = DBProviderType.MSSQL;
                    break;
                default:
                    throw new ConfigurationException($"{filename}: unknown database provider \"{config["database:connection"]}\"");
            }

            return cfg;
        }
    }
}