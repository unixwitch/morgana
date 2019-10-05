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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgana.Expr {
    /* An empty (null) value */
    public class NullExpression : Expression {
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) => this;
        public override string ToString() => "null";
    }
}