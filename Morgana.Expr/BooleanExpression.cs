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
using System.Collections;
using System.Collections.Generic;

namespace Morgana.Expr {
    /* A boolean value */
    public class BooleanExpression : Expression {
        public static readonly BooleanExpression True = new BooleanExpression(true);
        public static readonly BooleanExpression False = new BooleanExpression(false);

        protected bool Value { get; set; }

        public BooleanExpression(bool v) {
            Value = v;
        }

        public new static BooleanExpression Parse(string s) {
            switch (s.ToLower()) {
                case "true":
                case "t":
                case "y":
                case "yes":
                    return new BooleanExpression(true);

                case "false":
                case "f":
                case "n":
                case "no":
                    return new BooleanExpression(false);

                default:
                    throw new FormatException($"invalid boolean value \"{s}\"");
            }
        }

        public override bool ToBoolean() {
            return Value;
        }

        public override string ToString() {
            if (Value)
                return "true";
            else
                return "false";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }
    }
}