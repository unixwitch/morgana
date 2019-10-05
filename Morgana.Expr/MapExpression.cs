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
#if false
    public partial class Parser {
        public MapExpression ParseMap(ISymbolTable table) {
            List<Tuple<Expression, Expression>> values = new List<Tuple<Expression, Expression>>();

            if (!(lex.Next is TLBrace))
                return null;
            lex.Consume();

            Expression e1, e2;
            while ((e1 = ParseExpression(table, 0)) != null) {
                if (!(lex.Next is TArrow))
                    throw new ParseException("expected '->'", lex.Next);
                lex.Consume();

                if ((e2 = ParseExpression(table, 0)) == null)
                    throw new ParseException("expected expression", lex.Next);

                values.Add(Tuple.Create(e1, e2));

                if (!(lex.Next is TComma))
                    break;
                lex.Consume();

            }

            switch (lex.Next) {
                case TRBrace s:
                    lex.Consume();
                    return new MapExpression(values);

                default:
                    throw new ParseException("expected '}' or ','", lex.Next);
            }
        }
    }

    public struct MapItem {
        public Expression Key { get; set; }
        public Expression Value { get; set; }
    }

    /* A key<>value map */
    public class MapExpression : Expression, IDictionary<Expression, Expression> {
        protected Dictionary<Expression, Expression> MapItems { get; set; }

        public ICollection<Expression> Keys => MapItems.Keys;
        ICollection<Expression> IDictionary<Expression, Expression>.Values => MapItems.Values;
        public int Count => MapItems.Count;
        public bool IsReadOnly => ((IDictionary<Expression, Expression>)MapItems).IsReadOnly;

        public Expression this[Expression key] {
            get => MapItems[key];
            set => MapItems[key] = value;
        }

        public override Expression Evaluate() {
            return this;
        }

        public MapExpression() {
            MapItems = new Dictionary<Expression, Expression>();
        }

        public MapExpression(IEnumerable<Tuple<Expression, Expression>> values) {
            MapItems = new Dictionary<Expression, Expression>();
            foreach (var value in values)
                MapItems.Add(value.Item1, value.Item2);
        }

        public override int GetHashCode() {
            return MapItems.GetHashCode();
        }

        public override string ToString() {
            return "{ " +
                String.Join(", ", MapItems.Select(x => x.Key.ToString() + " -> " + x.Value.ToString()))
            + " }";
        }

        public void Append(Expression k, Expression v) {
            MapItems.Add(k, v);
        }

        public IEnumerator<MapItem> GetEnumerator() {
            foreach (var item in MapItems)
                yield return new MapItem {
                    Key = item.Key,
                    Value = item.Value,
                };
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public bool ContainsKey(Expression key) => MapItems.ContainsKey(key);
        public void Add(Expression key, Expression value) => MapItems.Add(key, value);
        public void Add(KeyValuePair<Expression, Expression> item)
            => ((IDictionary<Expression, Expression>)MapItems).Add(item);
        public bool Remove(Expression key) => MapItems.Remove(key);
        public bool TryGetValue(Expression key, out Expression value)
            => MapItems.TryGetValue(key, out value);

        public void Clear() => MapItems.Clear();
        public bool Contains(KeyValuePair<Expression, Expression> item)
            => MapItems.Contains(item);
        public void CopyTo(KeyValuePair<Expression, Expression>[] array, int arrayIndex)
            => ((IDictionary<Expression, Expression>)MapItems).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<Expression, Expression> item)
            => ((IDictionary<Expression, Expression>)MapItems).Remove(item);

        IEnumerator<KeyValuePair<Expression, Expression>> IEnumerable<KeyValuePair<Expression, Expression>>.GetEnumerator() {
            return ((IDictionary<Expression, Expression>)MapItems).GetEnumerator();
        }
    }
#endif
}