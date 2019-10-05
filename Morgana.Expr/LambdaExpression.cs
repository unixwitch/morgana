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
        public Expression ParseLambda(ISymbolTable table) {
            if (!(lex.Next is TBackslash))
                return null;
            lex.Consume();

            var ntable = new SymbolTable(table);
            var args = new List<string>();

            while (lex.Next is TIdentifier id) {
                args.Add(id.Name);
                lex.Consume();
            }

            if (!(lex.Next is TArrow))
                throw new ParseException("expected '->'", lex.Next);
            lex.Consume();

            var lambda = new LambdaExpression(args.Count);
            for (int i = 0; i < args.Count; ++i)
                ntable.SetSymbol(args[i], new LambdaArgument(lambda, i));

            var expr = ParseExpression(ntable, 0);
            lambda.Expr = expr ?? throw new ParseException("expected expression", lex.Next);

            return lambda;
        }
    }

    public class LambdaArgument : Expression {
        LambdaExpression _expr;
        int _argnum;

        public LambdaArgument(LambdaExpression expr, int i) {
            _expr = expr;
            _argnum = i;
        }

        public override int NumArgs => _expr.Args[_argnum].NumArgs;
        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) => _expr.Args[_argnum].Evaluate(ctx, args);
        public override string ToString() => $"<arg#{_argnum}>";
    }

    public class LambdaExpression : Expression {
        public List<Expression> Args { get; protected set; }  = new List<Expression>();
        public Expression Expr { get; set; }
        int _numargs;
        public override int NumArgs => _numargs;

        public LambdaExpression(int nargs) {
            _numargs = nargs;
        }

        public override string ToString() => "\\->" + Expr.ToString();

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            if (args.Length < _numargs)
                return this;

            Args.Clear();

            for (int i = 0; i < args.Length; ++i)
                Args.Add(args[i]);

            return Expr.Evaluate(ctx, new Expression[] { });
        }
    }
}