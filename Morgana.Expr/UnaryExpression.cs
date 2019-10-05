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

namespace Morgana.Expr {
    /* Represents a parsed unary expression */
    internal class UnaryOperator {
        public Token Token { get; set; }
        public int Precedence { get; set; }
        public Expression Expression { get; set; }

        public UnaryOperator(Token tok, int prec, Expression expr) {
            Token = tok;
            Precedence = prec;
            Expression = expr;
        }
    }

    public partial class Parser {
        /* Match a unary operator and return it; does not consume the token */
        internal UnaryOperator MatchUnaryOperator() {
#if false
            var tok = lex.Next;
            switch (tok) {
                case null:
                    return null;
                case TMinus t:
                    return new UnaryOperator(tok, 8, new UnaryNegateExpression());
                case TTilde t:
                    return new UnaryOperator(tok, 8, new UnaryBitwiseInvertExpression());
                case TBang t:
                    return new UnaryOperator(tok, 8, new UnaryBooleanInvertExpression());
            }
#endif
            return null;
        }
    }
}
