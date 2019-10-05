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
    public class IsLessThanExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(<)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ai.ToInteger() < bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ai.ToInteger() < bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ad.ToDecimal() < bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ad.ToDecimal() < bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} < {b.Explain()}");
        }
    }

    public class IsLessThanEqualExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(<=)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ai.ToInteger() <= bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression((decimal)ai.ToInteger() <= bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ad.ToDecimal() <= (decimal)bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ad.ToDecimal() <= bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} <= {b.Explain()}");
        }
    }

    public class IsEqualToExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(==)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            /* null can be equal to itself but nothing else */
            if (a is NullExpression || b is NullExpression)
                return new BooleanExpression(a is NullExpression && b is NullExpression);

#if false
            if (a.Type.ReturnType == ListType.Instance && b.Type.ReturnType == ListType.Instance) {
                /* Lists are equal if they are of the same size and all their values are equal */
                var compare = new IsEqualToExpression();
                for (; ; ) {
                    if (a.HasNext == false && b.HasNext == false)
                        return new BooleanExpression(true);
                    if (a.HasNext != b.HasNext)
                        return new BooleanExpression(false);
                    if (compare.Call(a.Item, b.Item).ToBoolean() == false)
                        return new BooleanExpression(false);
                    a = a.Next.Evaluate();
                    b = b.Next.Evaluate();
                }
            }
#endif

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ai.ToInteger() == bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ai.ToInteger() == bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ad.ToDecimal() == bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ad.ToDecimal() == bd.ToDecimal());
                    }
                    break;

                case BooleanExpression ab:
                    if (b is BooleanExpression bb)
                        return new BooleanExpression(ab.ToBoolean() == bb.ToBoolean());
                    break;

                case StringExpression as_:
                    if (b is StringExpression bs)
                        return new BooleanExpression(as_.ToString() == bs.ToString());
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} == {b.Explain()}");
        }
    }

    public class IsNotEqualToExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(!=)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return new BooleanExpression(!new IsEqualToExpression().Evaluate(ctx, args).ToBoolean());
        }
    }

    public class IsGreaterThanExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(>)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ai.ToInteger() > bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ai.ToInteger() > bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ad.ToDecimal() > bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ad.ToDecimal() > bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} > {b.Explain()}");
        }
    }

    public class IsGreaterThanEqualExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(>=)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            switch (a) {
                case IntegerExpression ai:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ai.ToInteger() >= bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ai.ToInteger() >= bd.ToDecimal());
                    }
                    break;

                case DecimalExpression ad:
                    switch (b) {
                        case IntegerExpression bi:
                            return new BooleanExpression(ad.ToDecimal() >= bi.ToInteger());
                        case DecimalExpression bd:
                            return new BooleanExpression(ad.ToDecimal() >= bd.ToDecimal());
                    }
                    break;
            }

            throw new ExpressionException($"incompatible types: {a.Explain()} >= {b.Explain()}");
        }
    }
    public class BooleanOrExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(||)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return new BooleanExpression(args[0].Evaluate(ctx).ToBoolean() || args[1].Evaluate(ctx).ToBoolean());
        }
    }

    public class BooleanAndExpression : Expression {
        public override int NumArgs => 2;
        public override string ToString() {
            return "(&&)";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return new BooleanExpression(args[0].Evaluate(ctx).ToBoolean() && args[1].Evaluate(ctx).ToBoolean());
        }
    }
}