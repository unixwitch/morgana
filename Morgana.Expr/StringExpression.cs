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
    public class StringExpression : Expression {
        protected String Value { get; set; }

        public StringExpression(string v) {
            Value = v;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }

        public override string ToString() {
            return Value;
        }

        public override string Stringify() {
            return '"' + Value.Replace("\\", "\\\\").Replace("\"", "\\\"") + '"';
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }
    }
}