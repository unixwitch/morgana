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
    /*
     * A statement is the top level component of a program.  Statements may contain expressions,
     * but not vice versa.
     */
    public partial class Parser {
#if false
        public void ParseStatements(ISymbolTable table) {
            while (ParseStatement(table) == true)
                ;

            if (lex.Next is null)
                return;

            throw new ParseException("parse error", lex.Next);
        }

        public bool ParseStatement(ISymbolTable table) {
            /* Statements can be preceeded by some newlines. */
            while (lex.Next is TNewline)
                lex.Consume();

            /*
             * A statement begins with an identifier.
             */

            if (lex.Next is null)
                return false;

            if (!(lex.Next is TIdentifier id))
                return false;

            var name = id.Name;
            lex.Consume();

            switch (lex.Next) {
                case TColonColon c:
                    lex.Consume();
                    ParseDeclaration(table, name);
                    return true;

                default:
                    ParseDefinition(table, name);
                    return true;
            }
        }
#endif
#if false
        public ExpressionType ParseType() {
            switch (lex.Next) {
                case TIdentifier id:
                    /* Basic type */
                    lex.Consume();
                    return ExpressionType.Parse(id.Name);

                case TLParen lp:
                    /* Function type */
                    lex.Consume();

                    List<ExpressionType> types = new List<ExpressionType>();
                    ExpressionType t1;
                    while ((t1 = ParseType()) != null) {
                        types.Add(t1);
                        if (!(lex.Next is TArrow))
                            break;
                        lex.Consume();
                    }

                    if (!(lex.Next is TRParen))
                        throw new ParseException("expected ')' after type list", lex.Next);
                    lex.Consume();

                    return new FunctionType {
                        ReturnType = types[types.Count() - 1],
                        ArgumentTypes = types.Take(types.Count() - 1).ToArray(),
                    };

                default:
                    return null;
            }
        }
#endif

#if false
        public void ParseDeclaration(ISymbolTable table, string name) {
            List<ExpressionType> types = new List<ExpressionType>();

            for (; ; ) {
                ExpressionType t;

                if ((t = ParseType()) == null)
                    throw new ParseException($"expected type name in declaration for '{name}'", lex.Next);

                types.Add(t);

                if (!(lex.Next is TArrow))
                    break;
                lex.Consume();
            }

            ExpressionType returntype = types[types.Count - 1];
            ExpressionType[] argtypes = types.Take(types.Count - 1).ToArray();

            PatternMatchingFunction p = new PatternMatchingFunction(name, returntype, argtypes);
            table.SetSymbol(name, p);

            if (!(lex.Next is TNewline))
                throw new ParseException($"expected '->' or newline in declaration for '{name}'", lex.Next);

            lex.Consume();
        }
#endif

#if false
        public Pattern ParsePattern(ISymbolTable table, ExpressionType argtype, ref int argnum) {
            switch (lex.Next) {
                case TIdentifier id:
                    lex.Consume();
                    table.SetSymbol(id.Name,
                        new UnboundArgument(id.Name, argtype.MakeFunctionType(), argnum));
                    argnum++;
                    return new NamedArgumentPattern(id.Name);

                case TInteger i:
                    lex.Consume();
                    return new ValuePattern(new IntegerExpression(i.Value));

                case TBoolean b:
                    lex.Consume();
                    return new ValuePattern(new BooleanExpression(b.Value));

                case TNull n:
                    lex.Consume();
                    return new ValuePattern(new NullExpression());

                case TUnderscore u:
                    lex.Consume();
                    return new MatchAnyPattern();

                case TLSq l:
                    lex.Consume();
                    if (lex.Next is TRSq) {
                        lex.Consume();
                        argnum++;
                        return new EmptyListPattern();
                    }

                    throw new ParseException("function pattern can only contain empty list", lex.Next);

                case TLParen l:
                    lex.Consume();

                    Pattern first;
                    if ((first = ParsePattern(table, argtype, ref argnum)) == null)
                        throw new ParseException("expected item after '(' in list pattern", lex.Next);

                    while (lex.Next is TColon) {
                        lex.Consume();
                        Pattern next;

                        if ((next = ParsePattern(table, argtype, ref argnum)) == null)
                            throw new ParseException("expected item after ':' in list pattern", lex.Next);

                        first = new OneAndRestPattern(first, next);
                    }

                    if (!(lex.Next is TRParen))
                        throw new ParseException("expected ')' after identifier in pattern", lex.Next);
                    lex.Consume();

                    return first;

                default:
                    return null;
            }
        }
#endif

#if false
        public void ParseDefinition(ISymbolTable table, string name) {
        FunctionPattern pattern = new FunctionPattern();

            Expression e = table.GetSymbol(name);

            if (e == null)
                throw new ParseException($"missing declaration for '{name}'", lex.Next);

            PatternMatchingFunction pf;
            try {
                pf = (PatternMatchingFunction)e;
            } catch (InvalidCastException) {
                throw new ParseException($"multiple incompatible definitions of '{name}'", null);
            }

            /* Optional list of patterns */
            int argnum = 0;
            var argtypes = pf.Type.ArgumentTypes;
            for (int i = 0; i < argtypes.Length; i++) {
                Pattern p;

                if ((p = ParsePattern(table, argtypes[i], ref argnum)) == null)
                    throw new ParseException($"{name}: not enough arguments for function type", null);
                pattern.Patterns.Add(p);
            }

            if (!(lex.Next is TAssign))
                throw new ParseException($"{name}: expected '=' in symbol definition", lex.Next);
            lex.Consume();

            Expression body = ParseExpression(table, 0);

            if (lex.Next != null && !(lex.Next is TNewline))
                throw new ParseException($"{name}: expected newline after function body", lex.Next);

            while (lex.Next is TNewline)
                lex.Consume();

            pf.Patterns.Add(Tuple.Create(pattern, body));
        }
#endif
    }
}