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
#if false
    public partial class Parser {
        public Expression ParseCase(ISymbolTable table) {
            if (!(lex.Next is TCase))
                return null;
            lex.Consume();

            Expression cond, e1, e2;
            var cases = new List<Tuple<Expression, Expression>>();

            if ((cond = ParseExpression(table, 0)) == null)
                throw new ParseException("expected condition after 'case'", lex.Next);

            if (!(lex.Next is TOf))
                throw new ParseException("expected 'of' after case condition", lex.Next);
            lex.Consume();

            for (; ; ) {
                if (lex.Next is TUnderscore) {
                    lex.Consume();
                    e1 = null;
                } else if ((e1 = ParseExpression(table, 0)) == null)
                    throw new ParseException("expected case condition", lex.Next);

                if (!(lex.Next is TArrow))
                    throw new ParseException("expected '->' after case pattern", lex.Next);
                lex.Consume();

                if ((e2 = ParseExpression(table, 0)) == null)
                    throw new ParseException("expected expression after '->'", lex.Next);

                cases.Add(Tuple.Create(e1, e2));

                if (!(lex.Next is TComma))
                    break;
                lex.Consume();
            }

            return new CaseExpression(cond, cases);
        }
    }

    /* case of */
    public class CaseExpression : Expression {
        private Expression Value { get; set; }
        List<Tuple<Expression, Expression>> Patterns { get; set; }

        public CaseExpression(Expression value, IEnumerable<Tuple<Expression, Expression>> patterns) {
            Value = value;
            Patterns = new List<Tuple<Expression, Expression>>(patterns);
        }

        public override Expression Evaluate() {
            Expression v = Value.Evaluate();
            var compare = new IsEqualToExpression();

            foreach (var p in Patterns) {
                if (p.Item1 == null)
                    return p.Item2.Evaluate();

                if (compare.Call(v, p.Item1.Evaluate()).ToBoolean())
                    return p.Item2.Evaluate();
            }

            throw new ExpressionException("case expression returned no value");
        }

        public override string ToString() {
            var cases = String.Join(", ", Patterns.Select(x => $"{x.Item1} -> {x.Item2}"));
            return $"case {Value} of {cases}";
        }
    }
#endif
}