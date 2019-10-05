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
using System.Collections;

namespace Morgana.Expr {
    public partial class Expression {
        /* join */
        public static Expression Join(ExpressionContext ctx, Expression[] args) {
            var sep = args[0].Evaluate(ctx).ToString();
            var list = args[1].Evaluate(ctx) as IEnumerable<Expression>;

            if (list == null)
                throw new ExpressionException("attempt to join non-list");

            return new StringExpression(string.Join(sep, list.WithCancellation(ctx).Select(e => {
                var f = e.Evaluate(ctx);
                if (f is NullExpression)
                    return "";
                else
                    return f.ToString();
            })));
        }

        public readonly static Expression JoinFunction = new FunctionExpression("join", 2, Join);

        public static Expression Map(ExpressionContext ctx, Expression[] args) {
            var fn = args[0].Evaluate(ctx);

            if (!(args[1].Evaluate(ctx) is ListExpression list))
                throw new ExpressionException("attempt to map a non-list");

            return new EnumerableListExpression(list.IsInfinite, list.WithCancellation(ctx).Select(e => fn.Evaluate(ctx, new Expression[] { e.Evaluate(ctx) })));
        }

        public readonly static Expression MapFunction = new FunctionExpression("map", 2, Map);

        public static Expression Sum(ExpressionContext ctx, Expression[] args) {
            if (!(args[0].Evaluate(ctx) is ListExpression list))
                throw new ExpressionException("attempt to sum a non-list");

            if (list.IsInfinite)
                throw new ExpressionException("attempt to sum infinite list");
            return new DecimalExpression(list.WithCancellation(ctx).Select(e => e.Evaluate(ctx).ToDecimal()).Sum());
        }

        public readonly static Expression SumFunction = new FunctionExpression("sum", 2, Sum);
        public static Expression Take(ExpressionContext ctx, Expression[] args) {
            long n = args[0].Evaluate(ctx).ToInteger();

            if (!(args[1].Evaluate(ctx) is ListExpression list))
                throw new ExpressionException("attempt to sum a non-list");

            return new EnumerableListExpression(false, list.WithCancellation(ctx).Take((int) n));
        }

        public readonly static Expression TakeFunction = new FunctionExpression("take", 2, Take);
    }

#if false
        /* format */
        public static Expression Format_(Expression[] args) {
            var format = args[0].Evaluate().ToString();
            var obj = args[1].Evaluate();

            return new StringExpression(obj.Format(format));
        }

        public readonly static Expression FormatFunction = new FunctionExpression("format", Format_);

        /* array ++ */
        public static Expression ArrayConcat(Expression[] args) {
            var a1 = (ListExpression)args[0].Evaluate();
            var a2 = (ListExpression)args[1].Evaluate();

            return new ConcatListExpression(a1, a2);
        }

        public readonly static Expression ArrayConcatFunction = new FunctionExpression("arrayConcat", ArrayConcat);

        public static Expression Pick(Expression[] args) {
            var n = args[0].ToInteger();
            var a = (ListExpression)(args[1].Evaluate());
            var enu = a.GetEnumerator();
            enu.MoveNext();

            for (int i = 0; i < n; i++)
                enu.MoveNext();
            return enu.Current.Evaluate();
        }

        public readonly static FunctionExpression PickFunction = new FunctionExpression("pick", Pick);
    }

    public class ArrayPrependExpression : Expression {
        public override string ToString() {
            return "(:)";
        }

        public override Expression Evaluate(Expression[] args) {
            return new LinkedListExpression(args[0].Evaluate(), args[1]);
        }
    }

#endif
}