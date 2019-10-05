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
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Morgana.Expr {

    public class ExpressionException : Exception {
        public ExpressionException(string message) : base(message) { }
    }

    public class UndefinedVariableException : ExpressionException {
        public UndefinedVariableException(string message) : base(message) { }
    }

    public class ParseException : ExpressionException {
        public Token ErrorToken { get; set; }

        public ParseException(string message, Token errtok) : base(message) {
            ErrorToken = errtok;
        }

        public string PrettyFormat() {
            if (ErrorToken is null)
                return Message;

            //var location = $"line {ErrorToken.Location.Line}";
            var context = ErrorToken.Location.LineText.Trim().Replace("\t", " ");
            var marker = new String(' ', ErrorToken.Location.Column)
                    + new String('^', ErrorToken.MatchLength);

            return $"{Message}\n    {context}\n    {marker}\n";
        }
    }

    public class BadExpressionCast : ExpressionException {
        public BadExpressionCast(string message) : base(message) { }
    }

    public class EvaluationException : ExpressionException {
        public EvaluationException(string message) : base(message) { }
    }

    public class ExpressionContext {
        readonly CancellationTokenSource source;
        readonly CancellationToken token;

        public ExpressionContext() {
            source = new CancellationTokenSource();
            token = source.Token;
        }

        public void CheckCancel() {
            token.ThrowIfCancellationRequested();
        }

        public void Cancel() {
            source.Cancel();
        }
    }

    /*
     * An expression which can be evaluated.
     */
    public abstract partial class Expression {
        public abstract Expression Evaluate(ExpressionContext ctx, Expression[] args);
        public Expression Evaluate(ExpressionContext ctx) => Evaluate(ctx, new Expression[] { });

        public virtual int NumArgs { get { return 0; } }

        public abstract override string ToString();

        public static Expression Parse(string expression) {
            var syms = new SymbolTable();
            AddFunctions(syms);
            return Parse(expression, syms);
        }

        public static Expression Parse(string expression, ISymbolTable table) {
            var lexer = new Lexer(expression);
            var parser = new Parser(lexer);
            var parsed = parser.ParseExpression(table, 0);
            if (parsed == null)
                throw new ParseException($"failed to parse expression: \"{expression}\"", null);

            if (lexer.Next != null)
                throw new ParseException("unexpected token", lexer.Next);

            return parsed;
        }

        public virtual string Stringify() {
            return ToString();
        }

        public virtual string Explain() {
            return $"{ToString()}";
        }

        public virtual string Format(string format) {
            throw new ExpressionException($"cannot format: {Explain()}");
        }

        public virtual bool ToBoolean() {
            throw new ExpressionException($"cannot convert {Explain()} to boolean");
        }

        public virtual long ToInteger() {
            throw new ExpressionException($"cannot convert {Explain()} to integer");
        }

        public virtual decimal ToDecimal() {
            throw new ExpressionException($"cannot convert {Explain()} to decimal");
        }
    }
}
