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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Morgana.Expr {
    internal class BinaryOperator {
        public enum AssociationType { Left, Right };
        public Token Token { get; set; }
        public int Precedence { get; set; }
        public AssociationType Association { get; set; }
        public Expression Expression { get; }

        public BinaryOperator(Token tok, int prec, AssociationType assoc, Expression e) {
            Token = tok;
            Precedence = prec;
            Association = assoc;
            Expression = e;
        }
    }

    public partial class Parser {
        /* Match a binary operator and return it; does not consume the token */
        internal BinaryOperator MatchBinaryOperator() {
            var tok = lex.Next;
            switch (tok) {
                case null:
                    return null;
#if false
                case TBangBang t:
                    return new BinaryOperator(tok, 9, BinaryOperator.AssociationType.Left,
                        Expression.FlipExpression.Flip(Expression.PickFunction));
#endif
#if false
                case TDot t:
                    return new BinaryOperator(tok, 9, BinaryOperator.AssociationType.Right,
                        new Expression.FunctionCompositionExpression());
#endif
                case TStar t:
                    return new BinaryOperator(tok, 7, BinaryOperator.AssociationType.Left,
                        new MultiplyExpression());

                case TSlash t:
                    return new BinaryOperator(tok, 7, BinaryOperator.AssociationType.Left,
                        new DivideExpression());

                case TPercent t:
                    return new BinaryOperator(tok, 7, BinaryOperator.AssociationType.Left,
                        new ModulusExpression());

                case TPlus t:
                    return new BinaryOperator(tok, 6, BinaryOperator.AssociationType.Left,
                        new AddExpression());

                case TMinus t:
                    return new BinaryOperator(tok, 6, BinaryOperator.AssociationType.Left,
                        new SubtractExpression());

                case TPlusPlus t:
                    return new BinaryOperator(tok, 5, BinaryOperator.AssociationType.Right,
                        new ListConcatExpression());

                case TColon c:
                    return new BinaryOperator(tok, 5, BinaryOperator.AssociationType.Right,
                        new ListPrependExpression());

                case TLT t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsLessThanExpression());

                case TLTE t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsLessThanEqualExpression());

                case TEq t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsEqualToExpression());

                case TNotEq t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsNotEqualToExpression());

                case TGT t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsGreaterThanExpression());

                case TGTE t:
                    return new BinaryOperator(tok, 4, BinaryOperator.AssociationType.Left,
                        new IsGreaterThanEqualExpression());

                case TAndAnd t:
                    return new BinaryOperator(tok, 3, BinaryOperator.AssociationType.Left,
                        new BooleanAndExpression());

                case TOrOr t:
                    return new BinaryOperator(tok, 2, BinaryOperator.AssociationType.Left,
                        new BooleanOrExpression());
            }

            return null;
        }
    }
}
