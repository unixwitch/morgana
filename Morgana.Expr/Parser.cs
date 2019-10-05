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
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Morgana.Expr {
    public partial class Parser {
        private Lexer lex;

        public Parser(Lexer l) {
            lex = l;
        }

        public Expression ParseExpression(ISymbolTable table, int prec) {
            Expression e, e2;

            /* An expression must begin with a value of some kind */
            if ((e = ParseValue(table)) == null)
                return null;

            for (; ; ) {
                /* 
                 * A binary operator is <expr> <op> <expr>, with variable precedence.
                 */
                BinaryOperator binop;
                if ((binop = MatchBinaryOperator()) != null) {
                    if (binop.Precedence < prec)
                        break;
                    lex.Consume();
                    /* On the right hand side we should have another expression */
                    int q = (binop.Association == BinaryOperator.AssociationType.Right)
                        ? binop.Precedence : 1 + binop.Precedence;

                    var rhs = ParseExpression(table, q);

                    /* Left-binding operator: (3+) */
                    if (rhs == null) {
                        e = Function2Bind1Expression.Bind(binop.Expression, e);
                    } else {
                        e = CallExpression.Create(binop.Expression, e, rhs);
                    }

                    continue;
                }

                /*
                 * A value followed by a $ is an explicit application (function call)
                 * with very low binding precedence: sum $ take 5 [1..]
                 */
                if (prec == 0 && lex.Next is TDollar) {
                    lex.Consume();
                    if ((e2 = ParseExpression(table, 0)) != null) {
                        e = CallExpression.Create(e, e2);
                        continue;
                    }
                    throw new ParseException("expected expression", lex.Next);
                }

                /*
                 * A value directly followed by another value is an application (function call)
                 * with high binding precedence: sum [1..10]
                 */
                if (11 >= prec && (e2 = ParseExpression(table, 12)) != null) {
                    e = CallExpression.Create(e, e2);
                    continue;
                }

                /*
                 * If we didn't match anything else, return the expression.
                 */
                return e;
            }

            return e;
        }

        public Expression ParseValue(ISymbolTable table) {
            /* A value can begin with a unary operator */
            var uniop = MatchUnaryOperator();
            if (uniop != null) {
                lex.Consume();
                var value = ParseExpression(table, uniop.Precedence);
                if (value != null)
                    return CallExpression.Create(uniop.Expression, value);

                return null;
            }

            switch (lex.Next) {
                case null:
                    return null;

                /* LParen could be a nested expression or partial operator application */
                case TLParen lp:
                    lex.Consume();

                    Expression s = ParseExpression(table, 0);
                    if (s != null) {
                        var rp = lex.Next;

                        switch (rp) {
                            /* Nested expression */
                            case TRParen p:
                                lex.Consume();
                                return s;
                            default:
                                throw new ParseException("expected closing ')' after '('", rp);
                        }
                    }

                    /* Not an expression, maybe a partial application */
                    var binop = MatchBinaryOperator();
                    if (binop != null) {
                        lex.Consume();

                        /* Optional rhs */
                        s = ParseExpression(table, 0);

                        var rp = lex.Next;
                        if (rp.GetType() != typeof(TRParen))
                            throw new ParseException("expected closing ')' after '('", rp);
                        lex.Consume();

                        if (s != null)
                            return Function2Bind2Expression.Bind(binop.Expression, s);
                        else
                            return binop.Expression;
                    }

                    throw new ParseException("expected expression after '('", lex.Next);

                /* If/then/else */
                case TIf i:
                    return ParseIf(table);

#if false
                /* case/of */
                case TCase c:
                    return ParseCase(table);
#endif

                /* List literal */
                case TLSq l:
                    return ParseList(table);

#if false
                /* Map literal */
                case TLBrace l:
                    return ParseMap(table);
#endif

                /* Lambda function, \x -> x+2 */
                case TBackslash b:
                    return ParseLambda(table);

                case TInteger i:
                    lex.Consume();
                    return new IntegerExpression(i.Value);

                case TDecimal d:
                    lex.Consume();
                    return new DecimalExpression(d.Value);

                case TBoolean b:
                    lex.Consume();
                    return new BooleanExpression(b.Value);

                case TString str:
                    lex.Consume();
                    switch (str.StringType) {
                        case TString.Type.ParsedString:
                            return ParsedStringExpression.Parse(table, str.Value.ToString());
                        default:
                            return new StringExpression(str.Value);
                    }

                case TNull n:
                    lex.Consume();
                    return new NullExpression();

                /* A value can be a variable name */
                case TIdentifier i:
                    lex.Consume();
                    var expr = table.GetSymbol(i.Name);
                    if (expr == null)
                        throw new ParseException($"'{i.Name}' undefined", i);
                    return expr;

                default:
                    return null;
            }
        }
    }
}