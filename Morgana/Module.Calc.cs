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
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Morgana.Expr;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Morgana {
    public class CalcModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("calc", RunMode = RunMode.Async)]
        [Summary("Evaluate an expression")]
        public async Task CalcAsync([Summary("The expression to evaluate")][Remainder] string expr) {
            try {
                var ctx = new ExpressionContext();

                var e = Expression.Parse(expr);
                var r = await Task.Run(() => TryEvaluate(ctx, e));
                await ReplyAsync(MentionUtils.MentionUser(Context.User.Id) + $" `{r}`");
            } catch (ParseException e) {
                await ReplyAsync(MentionUtils.MentionUser(Context.User.Id) + " ```" + e.PrettyFormat() + "```");
            } catch (OperationCanceledException) {
                await ReplyAsync(MentionUtils.MentionUser(Context.User.Id) + ", evaluation of that expression took too long.");
            } catch (Exception e) {
                await ReplyAsync(MentionUtils.MentionUser(Context.User.Id) + e.Message);
            }
        }

        public string TryEvaluate(ExpressionContext ctx, Expression e) {
            var fTask = Task.Run(() => PrettyFormat(ctx, e));
            var delayCancelSource = new CancellationTokenSource();
            var delay = Task.Run(async () => {
                try {
                    await Task.Delay(TimeSpan.FromSeconds(1), delayCancelSource.Token);
                    ctx.Cancel();
                } catch (OperationCanceledException) { }
            });

            try {
                fTask.Wait();
                delayCancelSource.Cancel();
                return fTask.Result;
            } catch (AggregateException ae) when (ae.InnerException is OperationCanceledException) {
                throw new OperationCanceledException();
            }
        }

        public static string PrettyFormat(ExpressionContext ctx, Expression e) {
            ctx.CheckCancel();
            e = e.Evaluate(ctx);

            if (!(e is IEnumerable<Expression> list))
                return e.ToString();

            var items = list.Take(11).Select(e => PrettyFormat(ctx, e));
            if (items.Count() == 11)
                return "[" + string.Join(", ", items) + ", ...]";
            else
                return "[" + string.Join(", ", items) + "]";
        }
    }
}
