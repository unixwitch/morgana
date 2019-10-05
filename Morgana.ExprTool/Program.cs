/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using Mono.Terminal;
using Morgana.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Morgana.ExprTool {
    class Program {
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

        static void Main(string[] args) {
            var editor = new LineEditor("Morgana.ExprTool");
            string s;
            while ((s = editor.Edit("> ", "")) != null) {
                try {
                    var ctx = new ExpressionContext();
                    var e = Expression.Parse(s);
                    Console.WriteLine(e.ToString());

                    var fTask = Task.Run(() => PrettyFormat(ctx, e));
                    var delayCancelSource = new CancellationTokenSource();
                    var delay = Task.Run(async () => {
                        try {
                            await Task.Delay(TimeSpan.FromSeconds(2), delayCancelSource.Token);
                            ctx.Cancel();
                        } catch (OperationCanceledException) { }
                    });

                    try {
                        fTask.Wait();
                        delayCancelSource.Cancel();
                        Console.WriteLine(fTask.Result);
                    } catch (AggregateException ae) when (ae.InnerException is OperationCanceledException) {
                        Console.WriteLine("Evaluation timed out.");
                    }
                } catch (ParseException e) {
                    Console.WriteLine(e.PrettyFormat());
                } catch (ExpressionException e) {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
