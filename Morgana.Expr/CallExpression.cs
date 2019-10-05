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
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Morgana.Expr {
    public class CallExpression : Expression {
        public Expression Function { get; }
        public Expression Argument { get; }

        public CallExpression(Expression function, Expression arg) {
            Function = function;
            Argument = arg;

            if (arg == null)
                throw new InvalidOperationException("attempt to call null argument");
        }

        public override string ToString() {
            return $"({Function.ToString()} {Argument.ToString()})";
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            var nargs = new Expression[args.Length + 1];
            nargs[0] = Argument;
            Array.Copy(args, 0, nargs, 1, args.Length);
            var evf = Function.Evaluate(ctx, nargs);
            return evf;
        }

        public static Expression Create(Expression function, params Expression[] args) {
            Expression e = function;

            foreach (var a in args)
                e = new CallExpression(e, a);
            return e;
        }
    }
}