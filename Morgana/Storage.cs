/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019, 2020 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Text.RegularExpressions;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.Extensions.Options;

namespace Morgana {
    public static class OptionNames {
        // General options.
        public static readonly string CommandPrefix = "commandprefix";
        public static readonly string InfobotPrefix = "infobot-prefix";

        // Badwords filter
        public static readonly string BadwordsEnabled = "badwords-enabled";
        public static readonly string BadwordsMessage = "badwords-message";

        // Auditing
        public static readonly string AuditEnabled = "audit-enabled";
        public static readonly string AuditChannel = "audit-channel";

        // Pinning
        public static readonly string PinFrom = "pin-from";
        public static readonly string PinTo = "pin-to";
        public static readonly string PinEnabled = "pin-enabled";
    };

    public class StorageContext : DbContext {
        public DbSet<GuildAdmin> GuildAdmins { get; set; }
        public DbSet<GuildAdminRole> GuildAdminRoles { get; set; }
        public DbSet<GuildOption> GuildOptions { get; set; }
        public DbSet<GuildManagedRole> GuildManagedRoles { get; set; }
        public DbSet<GuildBadword> GuildBadwords { get; set; }
        public DbSet<GuildFactoid> GuildFactoids { get; set; }
        public DbSet<BotOwner> BotOwners { get; set; }

        public StorageContext(DbContextOptions<StorageContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb) {
            mb.Entity<GuildAdmin>()
                .HasIndex(ga => new { ga.GuildId, ga.AdminId })
                .IsUnique();

            mb.Entity<GuildOption>()
                .HasIndex(go => new { go.GuildId, go.Option })
                .IsUnique();

            mb.Entity<GuildManagedRole>()
                .HasIndex(gmr => new { gmr.GuildId, gmr.RoleId })
                .IsUnique();

            mb.Entity<GuildBadword>()
                .HasIndex(gbw => new { gbw.GuildId, gbw.Badword, gbw.IsRegex })
                .IsUnique();

            mb.Entity<GuildFactoid>()
                .HasIndex(gf => new { gf.GuildId, gf.Name })
                .IsUnique();

            mb.Entity<BotOwner>()
                .HasIndex(bo => bo.OwnerId)
                .IsUnique();
        }

        /*
         * Return the config for a specific guild.
         */
        public GuildConfig GetGuild(IGuild guild) {
            return new GuildConfig(this, guild);
        }

        /*
         * Bot owners.
         */
        public async Task<IList<ulong>> GetOwnersAsync() {
            return await BotOwners
                .AsQueryable()
                .Select(o => ulong.Parse(o.OwnerId))
                .ToListAsync();
        }

        // This takes a ulong so we can add the initial owner before the client has started.
        public async Task<bool> OwnerAddAsync(ulong id) {
            try {
                var o = new BotOwner { OwnerId = id.ToString() };
                BotOwners.Add(o);
                await SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (e.InnerException is PostgresException sqlex && sqlex.SqlState == "23505") {
                return false;
            }
        }

        public async Task<bool> OwnerRemoveAsync(ulong id) {
            BotOwner ga = null;

            try {
                ga = await BotOwners
                    .AsQueryable()
                    .NotCacheable()
                    .Where(o => o.OwnerId == id.ToString())
                    .SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            BotOwners.Remove(ga);
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsOwnerAsync(ulong id) {
            return (await GetOwnersAsync()).Where(a => a == id).Any();
        }
    }

    public class StorageContextFactory : IDesignTimeDbContextFactory<StorageContext> {
        public StorageContext CreateDbContext(string[] args) {
            var _config = Configuration.Load(Path.Join(Directory.GetCurrentDirectory(), "config.ini"));
            var options = new DbContextOptionsBuilder<StorageContext>();
            _config.ConfigureDb(options);
            return new StorageContext(options.Options);
        }
    }

    public class BotOwner {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string OwnerId { get; set; }
    }

    public class GuildAdmin {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(20)]
        public string AdminId { get; set; }
    }

    public class GuildAdminRole {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoleId { get; set; }
    }

    public class GuildOption {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Option { get; set; }

        [Required]
        [MaxLength(256)]
        public string Value { get; set; }
    }

    public class GuildManagedRole {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoleId { get; set; }
    }

    public class GuildBadword {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        public bool IsRegex { get; set; }

        [Required]
        [MaxLength(64)]
        public string Badword { get; set; }
    }

    public class GuildFactoid {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Value { get; set; }
    }

    /*
     * Configuration for one specific guild.
     */
    public class GuildConfig {
        private StorageContext _db;
        private IGuild _guild;

        public GuildConfig(StorageContext db, IGuild guild) {
            _db = db;
            _guild = guild;
        }

        /*
         * Internal option handling.
         */

        /*
         * Get the string value of an option for this guild.
         */
        protected async Task<string> GetOptionAsync(string option) {
            try {
                // Retrieve all the existing options so we can cache them
                // using a single query.
                var opts = await _db.GuildOptions
                    .AsQueryable()
                    .Where(opt => opt.GuildId == _guild.Id.ToString())
                    .ToListAsync();

                // See if the requests option exists.
                return opts
                    .Where(opt => opt.Option == option)
                    .Select(opt => opt.Value)
                    .Single();
            } catch (InvalidOperationException) {
                // Option not set, return null.
                return null;
            }
        }

        /*
         * Set or change a string option for this guild.
         */
        protected async Task SetOptionAsync(string option, string value) {
            GuildOption opt;

            try {
                // Try to find an existing option by this name.
                opt = await _db.GuildOptions
                    .AsQueryable()
                    .Where(opt => opt.GuildId == _guild.Id.ToString() && opt.Option == option)
                    .SingleAsync();
                opt.Value = value;
            } catch (InvalidOperationException) {
                // Not existing option, add a new one.
                opt = new GuildOption { 
                    GuildId = _guild.Id.ToString(),
                    Option = option,
                    Value = value
                };
                await _db.GuildOptions.AddAsync(opt);
            }

            await _db.SaveChangesAsync();
        }

        /*
         * Get a bool option for this guild, or default true if it's
         * not set.
         */
        protected async Task<bool> GetOptionBoolOrTrue(string option) {
            return await GetOptionAsync(option) switch
            {
                null => true,
                "true" => true,
                _ => false,
            };
        }

        /*
         * Get a bool option for this guild, or default false if it's
         * not set.
         */
        protected async Task<bool> GetOptionBoolOrFalse(string option) {
            return await GetOptionAsync(option) switch
            {
                null => false,
                "true" => true,
                _ => false,
            };
        }

        /*
         * Set a boolean option for this guild.
         */
        protected Task SetOptionBoolAsync(string option, bool value) {
            return SetOptionAsync(option, value ? "true" : "false");
        }

        /*
         * Admins.
         */

        /*
         * Get the admin users for this guild.
         */
        public async Task<IList<ulong>> GetAdminUsersAsync() {
            return await _db.GuildAdmins
                    .AsQueryable()
                    .Where(ga => ga.GuildId == _guild.Id.ToString())
                    .Select(ga => ulong.Parse(ga.AdminId))
                    .ToListAsync();
        }

        /*
         * Add an admin user for this guild.
         */
        public async Task<bool> AdminUserAddAsync(ulong id) {
            try {
                var o = new GuildAdmin { 
                    GuildId = _guild.Id.ToString(),
                    AdminId = id.ToString()
                };
                await _db.GuildAdmins.AddAsync(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (
                    e.InnerException is PostgresException sqlex 
                    && sqlex.SqlState == "23505") {
                // User is already an admin.
                return false;
            }
        }

        /*
         * Remove an admin user from this guild.
         */
        public async Task<bool> AdminUserRemoveAsync(ulong id) {
            GuildAdmin ga = null;

            try {
                ga = await _db.GuildAdmins
                    .AsQueryable()
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.AdminId == id.ToString())
                    .NotCacheable()
                    .SingleAsync();
            } catch (InvalidOperationException) {
                // User is not an admin.
                return false;
            }

            _db.GuildAdmins.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        /*
         * Check if a user is an admin for this guild, without checking
         * their roles.
         */
        public async Task<bool> IsAdminUserAsync(ulong id) {
            return (await GetAdminUsersAsync()).Where(i => i == id).Any();
        }

        /*
         * Get admin roles for this guild.
         */
        public async Task<IList<ulong>> GetAdminRolesAsync() {
            return await _db.GuildAdminRoles
                    .AsQueryable()
                    .Where(ga => ga.GuildId == _guild.Id.ToString())
                    .Select(ga => ulong.Parse(ga.RoleId))
                    .ToListAsync();
        }

        /*
         * Add an admin role for this guild.
         */
        public async Task<bool> AdminRoleAddAsync(ulong id) {
            try {
                var o = new GuildAdminRole { 
                    GuildId = _guild.Id.ToString(),
                    RoleId = id.ToString()
                };
                await _db.GuildAdminRoles.AddAsync(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (
                    e.InnerException is PostgresException sqlex
                    && sqlex.SqlState == "23505") {
                // Role is already an admin role.
                return false;
            }
        }

        /*
         * Remove an admin role for this guild.
         */
        public async Task<bool> AdminRoleRemoveAsync(ulong id) {
            GuildAdminRole ga = null;

            try {
                ga = await _db.GuildAdminRoles
                    .AsQueryable()
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.RoleId == id.ToString())
                    .NotCacheable()
                    .SingleAsync();
            } catch (InvalidOperationException) {
                // Role not an admin.
                return false;
            }

            _db.GuildAdminRoles.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        /*
         * Check if a role is admin for this guild.
         */
        public async Task<bool> IsAdminRoleAsync(ulong id) {
            return (await GetAdminRolesAsync()).Where(i => i == id).Any();
        }

        /*
         * Check if a user is admin for this guild.
         */
        public async Task<bool> IsAdminAsync(IGuildUser user) {
            // Check admin users.
            if ((await GetAdminUsersAsync()).Where(i => i == user.Id).Any())
                return true;

            // Check admin roles.
            if ((await GetAdminRolesAsync()).Where(i => user.RoleIds.Contains(i)).Any())
                return true;

            // Not an admin.
            return false;
        }

        /*
         * Managed roles.
         */

        /*
         * Get the managed roles for this guild.
         */
        public Task<List<IRole>> GetManagedRolesAsync() {
            return
                _db.GuildManagedRoles
                    .AsQueryable()
                    .Where(mr => mr.GuildId == _guild.Id.ToString())
                    .Select(mr => _guild.GetRole(ulong.Parse(mr.RoleId)))
                    .ToListAsync();
        }

        /*
         * Add a managed role for this guild.
         */
        public async Task<bool> ManagedRoleAddAsync(IRole role) {
            try {
                var o = new GuildManagedRole { 
                    GuildId = _guild.Id.ToString(), 
                    RoleId = role.Id.ToString() 
                };
                await _db.GuildManagedRoles.AddAsync(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (
                    e.InnerException is PostgresException sqlex 
                    && sqlex.SqlState == "23505") {
                // Role is already managed.
                return false;
            }
        }

        /*
         * Remove a managed role for this guild.
         */
        public async Task<bool> ManagedRoleRemoveAsync(IRole role) {
            GuildManagedRole ga = null;

            // Check if the role is already managed.
            try {
                ga = await _db.GuildManagedRoles
                    .AsQueryable()
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.RoleId == role.Id.ToString())
                    .NotCacheable()
                    .SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            // Exists, so remove it.
            _db.GuildManagedRoles.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        /*
         * Return the managed roles for this guild.
         */
        public async Task<bool> IsManagedRoleAsync(IRole role) {
            return (await GetManagedRolesAsync()).Where(r => r.Id == role.Id).Any();
        }

        /*
         * General guild options.
         */

        // Get the command prefix for this guild.
        public Task<string> GetCommandPrefixAsync() 
            => GetOptionAsync(OptionNames.CommandPrefix);

        // Change the command prefix for this guild.
        public Task SetCommandPrefixAsync(string p) 
            => SetOptionAsync(OptionNames.CommandPrefix, p);

        // Get the infobot prefix for this guild.
        public async Task<string> GetInfobotPrefixAsync()
            => (await GetOptionAsync(OptionNames.InfobotPrefix)) ?? "??";

        // Set the infobot prefix for this guild.
        public Task SetInfobotPrefixAsync(string p)
            => SetOptionAsync(OptionNames.InfobotPrefix, p);

        /*
         * Badwords filter.
         */

        // Test if badwords filter is enabled.
        public Task<bool> IsBadwordsEnabledAsync() 
            => GetOptionBoolOrFalse(OptionNames.BadwordsEnabled);

        // Enable or disable badwords filter.
        public Task SetBadwordsEnabledAsync(bool v)
            => SetOptionBoolAsync(OptionNames.BadwordsEnabled, v);

        // Get the message used to admonish users.
        public Task<string> GetBadwordsMessageAsync()
            => GetOptionAsync(OptionNames.BadwordsMessage);

        // Set the message used to admonish users.
        public Task SetBadwordsMessageAsync(string m)
            => SetOptionAsync(OptionNames.BadwordsMessage, m);

        /*
         * Return the badwords for this guild.
         */
        public Task<List<GuildBadword>> GetBadwordsAsync() {
            return _db.GuildBadwords
                .AsQueryable()
                .Where(bw => bw.GuildId == _guild.Id.ToString())
                .ToListAsync();
        }

        /*
         * Add a new badword to this guild.
         */
        public async Task<bool> BadwordAddAsync(string w, bool isregex) {
            try {
                var o = new GuildBadword {
                    GuildId = _guild.Id.ToString(),
                    Badword = w.ToLower(),
                    IsRegex = isregex
                };
                await _db.GuildBadwords.AddAsync(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (
                    e.InnerException is PostgresException sqlex
                    && sqlex.SqlState == "23505") {
                // Badword already exists.
                return false;
            }
        }

        /*
         * Remove a configure badword from this guild.
         */
        public async Task<bool> BadwordRemoveAsync(string w, bool isregex) {
            GuildBadword ga = null;

            // Check if the badword actually exists.
            try {
                ga = await _db.GuildBadwords
                    .AsQueryable()
                    .Where(a => a.GuildId == _guild.Id.ToString())
                    .Where(a => a.IsRegex == isregex)
                    .Where(a => a.Badword == w.ToLower())
                    .NotCacheable()
                    .SingleAsync();
            } catch (InvalidOperationException) {
                // Doesn't exist.
                return false;
            }

            // Exists, so remove it.
            _db.GuildBadwords.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        /*
         * Check if the given word or regexp or configured as a badword in this guild.
         */
        public async Task<bool> IsBadwordAsync(string w, bool isregex) {
            var badwords = await GetBadwordsAsync();

            return (await GetBadwordsAsync())
                .Where(bw => bw.IsRegex == isregex && bw.Badword.ToLower() == w.ToLower())
                .Any();
        }

        /*
         * Check if any of the given strings are badwords for this guild.
         */
        public async Task<bool> MatchAnyBadwordAsync(string[] ws) {
            var badwords = await GetBadwordsAsync();

            // Check static strings.
            var strings = badwords
                .Where(bw => bw.IsRegex == false)
                .Select(bw => bw.Badword);

            if (ws.Any(w => strings.Contains(w.ToLower())))
                return true;

            // Check regular expressions.
            var regexs = badwords
                .Where(bw => bw.IsRegex == true)
                .Select(bw => bw.Badword);

            if (ws.Any(w => regexs.Any(r => Regex.Match(w.ToLower(), r).Success)))
                return true;

            // No match.
            return false;
        }

        /*
         * Check if the given word is a badword for this guild.
         */
        public async Task<bool> MatchBadwordAsync(string w) {
            var badwords = await GetBadwordsAsync();

            // Check static strings.
            var strings = badwords
                .Where(bw => bw.IsRegex == false)
                .Select(bw => bw.Badword);
            if (strings.Contains(w))
                return true;

            // Check regular expressions.
            var regexs = badwords.Where(bw => bw.IsRegex == true).Select(bw => bw.Badword);
            if (regexs.Any(r => Regex.Match(w.ToLower(), r).Success))
                return true;

            // No match.
            return false;
        }

        /*
         * Audit log.
         */
        public Task<bool> IsAuditEnabledAsync() 
            => GetOptionBoolOrFalse(OptionNames.AuditEnabled);

        public Task SetAuditEnabledAsync(bool v) 
            => SetOptionBoolAsync(OptionNames.AuditEnabled, v);

        public async Task<ITextChannel> GetAuditChannelAsync() {
            var chanId = await GetOptionAsync(OptionNames.AuditChannel);
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }

        public Task SetAuditChannelAsync(ITextChannel c) 
            => SetOptionAsync(OptionNames.AuditChannel, c.Id.ToString());

        /*
         * Picture pinner.
         */
        public async Task<ITextChannel> GetPinFromAsync() {
            var chanId = await GetOptionAsync(OptionNames.PinFrom);
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }

        public Task SetPinFromAsync(ITextChannel chan) 
            => SetOptionAsync(OptionNames.PinFrom, chan.Id.ToString());

        public async Task<ITextChannel> GetPinToAsync() {
            var chanId = await GetOptionAsync(OptionNames.PinTo);
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }
        public Task SetPinToAsync(ITextChannel chan) 
            => SetOptionAsync(OptionNames.PinTo, chan.Id.ToString());

        public Task<bool> IsPinnerEnabledAsync() 
            => GetOptionBoolOrFalse(OptionNames.PinEnabled);

        public Task SetPinnerEnabledAsync(bool v) 
            => SetOptionBoolAsync(OptionNames.PinEnabled, v);

        /*
         * Infobot.
         */

        /*
         * Get the value of an existing factoid, or return null if it doesn't exist.
         */
        public async Task<string> FactoidGetAsync(string name) {
            try {
                return (await _db.GuildFactoids
                    .AsQueryable()
                    .Where(f => f.GuildId == _guild.Id.ToString() && f.Name == name.ToLower())
                    .SingleAsync())
                    .Value;
            } catch (InvalidOperationException) {
                return null;
            }
        }

        /*
         * Add a factoid or change the value of an existing factoid.
         */
        public async Task FactoidSetAsync(string name, string value) {
            GuildFactoid f;

            // Check if the factoid already exists.
            try {
                f = await _db.GuildFactoids
                    .AsQueryable()
                    .Where(f => f.GuildId == _guild.Id.ToString() && f.Name == name.ToLower())
                    .NotCacheable()
                    .SingleAsync();
                // It exists, so change its value.
                f.Value = value;
            } catch (InvalidOperationException) {
                // It doesn't exist, so add a new one.
                f = new GuildFactoid { 
                    GuildId = _guild.Id.ToString(),
                    Name = name.ToLower(),
                    Value = value
                };
                await _db.GuildFactoids.AddAsync(f);
            }

            await _db.SaveChangesAsync();
        }

        /*
         * Remove an existing factoid from the database.
         */
        public async Task<bool> FactoidRemoveAsync(string key) {
            GuildFactoid f = null;

            // Check if the factoid exists.
            try {
                f = await _db.GuildFactoids
                    .AsQueryable()
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.Name == key.ToLower())
                    .NotCacheable()
                    .SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            // Exists, so remove it.
            _db.GuildFactoids.Remove(f);
            await _db.SaveChangesAsync();
            return true;
        }

    }
}
