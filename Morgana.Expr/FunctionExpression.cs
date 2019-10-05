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

namespace Morgana.Expr {
    /* A function.  XXX remove this */
    public class FunctionExpression : Expression {
        public string Name { get; protected set; } = "<function>";
        public override int NumArgs { get; }

        protected Func<ExpressionContext, Expression[], Expression> Method { get; private set; }

        public FunctionExpression(string name, int nargs, Func<ExpressionContext, Expression[], Expression> method) {
            Method = method;
            Name = name;
            NumArgs = nargs;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return Method.Invoke(ctx, args);
        }

        public override string ToString() {
            return Name;
        }
    }
}