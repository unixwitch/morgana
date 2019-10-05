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
    /* A decimal (floating-point) value */
    public class DecimalExpression : Expression {
        public decimal Value { get; protected set; }

        public DecimalExpression(decimal v) {
            Value = v;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) => this;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override decimal ToDecimal() => Value;

        public new static DecimalExpression Parse(string v) {
            try {
                return new DecimalExpression(decimal.Parse(v));
            } catch (FormatException e) {
                throw new ExpressionException(e.Message);
            }
        }
    }
}