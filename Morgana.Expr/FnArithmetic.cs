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
    public class AddExpression : Expression {
        public override int NumArgs => 2;

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new IntegerExpression(ai.ToInteger() + bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ai.ToInteger() + bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new DecimalExpression(ad.ToDecimal() + bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ad.ToDecimal() + bd.ToDecimal());
                    }
                    break;

                case StringExpression as_:
                    if (b is StringExpression bs)
                        return new StringExpression(as_.ToString() + bs.ToString());
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} + {b.Explain()}");
        }

        public override string ToString() {
            return "(+)";
        }
    }

    public class SubtractExpression : Expression {
        public override int NumArgs => 2;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new IntegerExpression(ai.ToInteger() - bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ai.ToInteger() - bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new DecimalExpression(ad.ToDecimal() - bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ad.ToDecimal() - bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} - {b.Explain()}");
        }

        public override string ToString() {
            return "(-)";
        }
    }

    public class MultiplyExpression : Expression {
        public override int NumArgs => 2;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new IntegerExpression(ai.ToInteger() * bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression((decimal)ai.ToInteger() * bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new DecimalExpression(ad.ToDecimal() * (decimal)bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ad.ToDecimal() * bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} * {b.Explain()}");
        }

        public override string ToString() {
            return "(*)";
        }
    }

    public class DivideExpression : Expression {
        public override int NumArgs => 2;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new IntegerExpression(ai.ToInteger() / bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression((decimal)ai.ToInteger() / bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new DecimalExpression(ad.ToDecimal() / (decimal)bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ad.ToDecimal() / bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} / {b.Explain()}");
        }

        public override string ToString() {
            return "(/)";
        }
    }

    public class ModulusExpression : Expression {
        public override int NumArgs => 2;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new IntegerExpression(ai.ToInteger() % bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ai.ToInteger() % bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new DecimalExpression(ad.ToDecimal() % (decimal)bi.ToInteger());
                        case DecimalExpression bd:
                            return new DecimalExpression(ad.ToDecimal() % bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} % {b.Explain()}");
        }

        public override string ToString() {
            return "(%)";
        }
    }

    public class UnaryNegateExpression : Expression {
        public override int NumArgs => 1;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            switch (args[0].Evaluate(ctx)) {
                case IntegerExpression i:
                    return new IntegerExpression(-i.ToInteger());
                case DecimalExpression d:
                    return new DecimalExpression(-d.ToDecimal());
            }

            throw new ExpressionException($"incompatible types: -{args[0].Explain()}");
        }

        public override string ToString() {
            return "(-)";
        }
    }

    public class UnaryBitwiseInvertExpression : Expression {
        public override int NumArgs => 1;
        public override string ToString() => "(~)";

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            if (args[0].Evaluate(ctx) is IntegerExpression i)
                return new IntegerExpression(~i.ToInteger());

            throw new ExpressionException($"incompatible types: ~{args[0].Explain()}");
        }
    }

    public class UnaryBooleanInvertExpression : Expression {
        public override int NumArgs => 1;
        public override string ToString() => "(!)";

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args)
            => new BooleanExpression(!args[0].Evaluate(ctx).ToBoolean());
    }
}
