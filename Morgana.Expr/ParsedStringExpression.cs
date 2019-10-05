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
    public class ParsedStringExpression : Expression {
        private IEnumerable<Expression> _exprs;

        protected ParsedStringExpression(IEnumerable<Expression> value) {
            _exprs = value;
        }

        public static ParsedStringExpression Parse(ISymbolTable table, string str) {
            List<Expression> concats = new List<Expression>();

            /* 
             * A parsed string is a normal string containing embedded expressions marked
             * with $, and optionally enclosed in {}.  The {} are not required if the
             * expression is a single identifier:
             * 
             *   p"The value of $a + $b is ${a + b}."
             *   
             * Internally this is represented as a list of expressions which are evaluated
             * then concatenated with ToString() to produce a string.
             */

            int i;
            for (; ; ) {
                /* Everything until the first $ is a string. */
                if ((i = str.IndexOf('$')) == -1) {
                    /* No more $ in the string, so finish here. */
                    concats.Add(new StringExpression(str));
                    break;
                }

                /* The text up to the $ is a normal string; store and remove it. */
                concats.Add(new StringExpression(str.Substring(0, i)));
                str = str.Substring(i + 1);

                /* 
                 * If the next character is {, then we expect a complete expressions;
                 * otherwise, only a single identifier.
                 */
                if (str[0] == '{') {
                    Lexer l = new Lexer(str.Substring(1));
                    Parser p = new Parser(l);
                    Expression e = p.ParseExpression(table, 0);
                    if (e == null)
                        throw new ParseException("could not parse expression", null);
                    concats.Add(e);
                    if (!(l.Next is TRBrace))
                        throw new ParseException("missing '}' after expression", null);
                    l.Consume();

                    str = str.Substring(l.Chars + 1);
                } else {
                    Lexer l = new Lexer(str);
                    Parser p = new Parser(l);
                    if (!(l.Next is TIdentifier id))
                        throw new ParseException("expected identified or '{' after $", null);
                    l.Consume();

                    var expr = table.GetSymbol(id.Name);
                    if (expr == null)
                        throw new ParseException($"'{id.Name}' undefined", null);
                    concats.Add(expr);
                    str = str.Substring(l.Chars);
                }
            }

            return new ParsedStringExpression(concats);
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return new StringExpression(
                String.Join("", _exprs.Select(e => e.Evaluate(ctx).ToString())));
        }

        public override string ToString() {
            return "p\""
                + String.Join("", _exprs.Select(e => "${" + e.ToString() + "}"))
                + '"';
        }
    }
}