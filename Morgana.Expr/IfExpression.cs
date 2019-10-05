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

namespace Morgana.Expr {
    public partial class Parser {
        public Expression ParseIf(ISymbolTable table) {
            if (!(lex.Next is TIf))
                return null;
            lex.Consume();

            Expression cond, e1, e2;

            if ((cond = ParseExpression(table, 0)) == null)
                throw new ParseException("expected condition after 'if'", lex.Next);

            if (!(lex.Next is TThen))
                throw new ParseException("expected 'then' after if condition", lex.Next);
            lex.Consume();

            if ((e1 = ParseExpression(table, 0)) == null)
                throw new ParseException("expected expression after 'then'", lex.Next);

            if (!(lex.Next is TElse))
                throw new ParseException("expected 'else' after then expression", lex.Next);
            lex.Consume();

            if ((e2 = ParseExpression(table, 0)) == null)
                throw new ParseException("expected expression after 'else'", lex.Next);

            return new IfExpression(cond, e1, e2);
        }
    }

    /* If/then/else */
    public class IfExpression : Expression {
        private Expression If { get; set; }
        private Expression Then { get; set; }
        private Expression Else { get; set; }

        public IfExpression(Expression if_, Expression then_, Expression else_) {
            If = if_;
            Then = then_;
            Else = else_;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) => If.Evaluate(ctx).ToBoolean() ? Then.Evaluate(ctx) : Else.Evaluate(ctx);

        public override string ToString()
            => $"if {If.ToString()} then {Then.ToString()} else {Else.ToString()}";
    }
}