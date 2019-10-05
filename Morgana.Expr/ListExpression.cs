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
using System.Collections;

namespace Morgana.Expr {
    public partial class Parser {
        public Expression ParseList(ISymbolTable table) {
            List<Expression> values = new List<Expression>();
            Expression value;

            if (!(lex.Next is TLSq))
                return null;

            lex.Consume();

            while ((value = ParseExpression(table, 0)) != null) {
                values.Add(value);

                /* [1, 3 .. 9] */
                if (lex.Next is TDotDot) {
                    if (values.Count() > 2)
                        throw new ParseException("expected 1 or 2 values", lex.Next);
                    lex.Consume();

                    Expression start = values[0];
                    Expression next = null;
                    if (values.Count() == 2)
                        next = values[1];
                    Expression end = ParseExpression(table, 0);

                    if (!(lex.Next is TRSq))
                        throw new ParseException("expected ']'", lex.Next);
                    lex.Consume();

                    long start_ = start.ToInteger();
                    long next_ = next != null ? next.ToInteger() : start_ + 1;
                    long? end_ = end?.ToInteger();
                    return new ListGeneratorExpression(start_, next_, end_);
                }
                if (!(lex.Next is TComma))
                    break;
                lex.Consume();
            }

            switch (lex.Next) {
                case TRSq _:
                    lex.Consume();
                    var list = new EnumerableListExpression(false, values);
                    return list;

                default:
                    throw new ParseException("expected ']' or ','", lex.Next);
            }
        }
    }

    /* A list expression. */
    public abstract class ListExpression : Expression, IEnumerable<Expression> {
        public abstract bool IsInfinite { get; }

        public abstract IEnumerator<Expression> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override string ToString() {
            var items = this.Take(11).Select(x => x.ToString());
            if (items.Count() > 10)
                return "[" + String.Join(", ", items.Take(10)) + ", ...]";
            else
                return "[" + String.Join(", ", items) + "]";
        }
    }

    /* A list that wraps an enumerable */
    public class EnumerableListExpression : ListExpression
    {
        protected IEnumerable<Expression> _values { get; set; }
        bool _isinf;
        public override bool IsInfinite => _isinf;

        public EnumerableListExpression(bool isinfinite, IEnumerable<Expression> values)
        {
            _isinf = isinfinite;
            _values = values;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }

        public override IEnumerator<Expression> GetEnumerator() {
            foreach (var v in _values)
                yield return v;
        }
    }

    public class PrependedListExpression : ListExpression {
        private Expression _item;
        private ListExpression _list;

        public override bool IsInfinite => _list.IsInfinite;

        public PrependedListExpression(Expression item, ListExpression list) {
            _item = item;
            _list = list;
        }

        public override IEnumerator<Expression> GetEnumerator() {
            yield return _item;

            if (!(_list is IEnumerable<Expression> list))
                throw new InvalidOperationException("Attempt to append non-list");

            foreach (var i in list)
                yield return i;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }
    }

    public class ListPrependExpression : Expression {
        public override int NumArgs => 2;

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            if (!(b is ListExpression list))
                throw new ExpressionException("attempt to prepend to non-list");

            return new PrependedListExpression(a, list);
        }

        public override string ToString() => "(:)";
    }

    public class ConcatedListExpression : ListExpression {
        private ListExpression _list1;
        private ListExpression _list2;

        public override bool IsInfinite => _list1.IsInfinite || _list2.IsInfinite;

        public ConcatedListExpression(ListExpression list1, ListExpression list2) {
            _list1 = list1;
            _list2 = list2;
        }

        public override IEnumerator<Expression> GetEnumerator() {
            foreach (var i in _list1)
                yield return i;
            foreach (var i in _list2)
                yield return i;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }
    }

    public class ListConcatExpression : Expression {
        public override int NumArgs => 2;

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var a = args[0].Evaluate(ctx);
            var b = args[1].Evaluate(ctx);

            if (!(a is ListExpression list1))
                throw new ExpressionException("attempt to concat to non-list");
            if (!(b is ListExpression list2))
                throw new ExpressionException("attempt to concat non-list");

            return new ConcatedListExpression(list1, list2);
        }

        public override string ToString() => "(++)";
    }
}