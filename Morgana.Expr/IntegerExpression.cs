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

namespace Morgana.Expr {
    /* An integer value */
    public class IntegerExpression : Expression {
        public long Value { get; protected set; }

        public IntegerExpression(long v) {
            Value = v;
        }

        public IntegerExpression(int v) {
            Value = v;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] _) {
            return this;
        }

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override long ToInteger() => Value;
        public override decimal ToDecimal() => Value;

        public new static IntegerExpression Parse(string v) {
            try {
                return new IntegerExpression(Int64.Parse(v));
            } catch (FormatException e) {
                throw new ExpressionException(e.Message);
            }
        }
    }
}
