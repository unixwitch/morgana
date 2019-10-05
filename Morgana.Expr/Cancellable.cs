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
using System.Linq;
using System.Collections.Generic;

namespace Morgana.Expr {
    static class CancelExtention {
        public static IEnumerable<T> WithCancellation<T>(this IEnumerable<T> en, ExpressionContext ctx) {
            foreach (var item in en) {
                ctx.CheckCancel();
                yield return item;
            }
        }
    }
}