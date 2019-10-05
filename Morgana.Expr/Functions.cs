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
    public partial class Expression {
        public static void AddFunctions(ISymbolTable table) {
            //            table.SetSymbol("pick", PickFunction);
            //            table.SetSymbol("format", FormatFunction);
            table.SetSymbol("map", MapFunction);
            table.SetSymbol("sum", SumFunction);
            table.SetSymbol("take", TakeFunction);
        }
    }

#if false
        public class FunctionCompositionExpression : Expression {

            private Expression _function1;
            private Expression _function2;

            private FunctionCompositionExpression(Expression f1, Expression f2, FunctionType type) {
                _function1 = f1;
                _function2 = f2;
                Type = type;
            }

            public override Expression BindArguments(Expression[] args)
                => new FunctionCompositionExpression(
                    BindArgument(_function1, args),
                    BindArgument(_function2, args),
                    Type);

            public static FunctionCompositionExpression Compose(Expression f1, Expression f2) {
                var type = new FunctionType {
                    ReturnType = f1.Type.ReturnType,
                    ArgumentTypes = f2.Type.ArgumentTypes,
                };

                return new FunctionCompositionExpression(f1, f2, type);
            }

            public override Expression Evaluate() {
                return this;
            }

            public override Expression Invoke(Expression[] args) {
                return CallExpression.Create(_function1,
                    CallExpression.Create(_function2, args)).Evaluate();
            }

            public override string ToString() {
                return $"({_function1.ToString()} . {_function2.ToString()})";
            }

        }
    }
#endif

    public class Function2Bind1Expression : Expression {
        public override int NumArgs => 1;
        private Expression _function;
        private Expression _argument;


        public static Function2Bind1Expression Bind(Expression fn, Expression arg) {
            return new Function2Bind1Expression(fn, arg);
        }

        private Function2Bind1Expression(Expression fn, Expression arg) {
            _function = fn;
            _argument = arg;
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            if (args.Length == 0)
                return this;
            else if (args.Length > 1)
                throw new InvalidOperationException("Attempt to evaluate Function2Bind1Expression with too many arguments");

            var nargs = new Expression[args.Length + 1];
            nargs[0] = _argument;
            args.CopyTo(nargs, 1);
            return _function.Evaluate(ctx, nargs);
        }

        public override string ToString() {
            return $"({_function.ToString()} {_argument.ToString()})";
        }
    }

    public class Function2Bind2Expression : Expression {
        public override int NumArgs => 1;
        private Expression _function;
        private Expression _argument;

        private Function2Bind2Expression(Expression fn, Expression arg) {
            _function = fn;
            _argument = arg;
        }

        public static Function2Bind2Expression Bind(Expression fn, Expression arg) {
            return new Function2Bind2Expression(fn, arg);
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            if (args.Length == 0)
                return this;
            else if (args.Length > 1)
                throw new InvalidOperationException("Attempt to evaluate Function2Bind2Expression with too many arguments");

            var nargs = new Expression[args.Length + 1];
            args.CopyTo(nargs, 1);
            nargs[0] = nargs[1];
            nargs[1] = _argument;

            return _function.Evaluate(ctx, nargs);
        }

        public override string ToString() {
            return $"({_function.ToString()} ? {_argument.ToString()})";
        }
    }

#if false
        public abstract class Pattern {
            public abstract bool Match(IList<Expression> args, Expression arg);
        }

        public class ValuePattern : Pattern {
            Expression value;

            public ValuePattern(Expression v) {
                value = v;
            }

            public override bool Match(IList<Expression> args, Expression arg) {
                var compare = new IsEqualToExpression();
                var isequal = compare.Invoke(new Expression[] { value.Evaluate(), arg });
                return isequal.ToBoolean();
            }
        }

        public class MatchAnyPattern : Pattern {
            public override bool Match(IList<Expression> args, Expression arg) {
                return true;
            }
        }

        public class NamedArgumentPattern : Pattern {
            string name;

            public NamedArgumentPattern(string n) {
                name = n;
            }

            public override bool Match(IList<Expression> args, Expression arg) {
                args.Add(arg);
                return true;
            }
        }
#endif

#if false
        public class EmptyListPattern : Pattern {
            public override bool Match(IList<Expression> args, Expression arg) {
                if (arg.Type.ReturnType != ListType.Instance)
                    return false;

                if (arg.HasItem)
                    return false;

                args.Add(arg);
                return true;
            }
        }

        public class OneAndRestPattern : Pattern {
            Pattern First;
            Pattern Rest;

            public OneAndRestPattern(Pattern first, Pattern rest) {
                First = first;
                Rest = rest;
            }

            public override bool Match(IList<Expression> args, Expression arg) {
                if (arg.Type.ReturnType != ListType.Instance)
                    return false;

                if (!arg.HasItem)
                    return false;

                if (!First.Match(args, arg.Item))
                    return false;

                Expression argnext;
                if (arg.HasNext)
                    argnext = arg.Next;
                else
                    /* empty list */
                    argnext = new LinkedListExpression();

                if (!Rest.Match(args, argnext))
                    return false;

                return true;
            }
        }

        class FunctionPattern {
            public List<Pattern> Patterns { get; set; }

            public FunctionPattern() {
                Patterns = new List<Pattern>();
            }

            public bool Match(IList<Expression> retargs, Expression[] args) {
                for (int i = 0; i < args.Length; ++i) {
                    if (!Patterns[i].Match(retargs, args[i]))
                        return false;
                }
                return true;
            }
        }

        class PatternMatchingFunction : Expression {
            public string Name;
            public List<Tuple<FunctionPattern, Expression>> Patterns;

            public PatternMatchingFunction(string name, ExpressionType returntype, ExpressionType[] argtypes) {
                Name = name;
                Patterns = new List<Tuple<FunctionPattern, Expression>>();
            }

            public override Expression Evaluate() {
                return this;
            }

            public override string ToString() {
                return Name;
            }

            public override Expression Invoke(Expression[] args) {
                return null;
            }
            Expression[] eargs = new Expression[args.Length];
            for (int i = 0; i < args.Length; ++i) {
                eargs[i] = args[i].Evaluate();
            }

            Tuple<List<Expression>, Expression> match = MatchPattern(eargs);
            if (match == null)
                throw new ExpressionException("could not match function argument");

            List<Expression> c = match.Item1;
            Expression body = BindArgument(match.Item2, c.ToArray()).LazyEvaluate();

            while (body is CallExpression ce && ce.Function == this) {
                /* Tail recursion */
                for (int i = 0; i < args.Length; ++i) {
                    eargs[i] = ce.Arguments[i].Evaluate();
                }

                match = MatchPattern(eargs);
                if (match == null)
                    throw new ExpressionException("could not match function argument");

                c = match.Item1;
                body = BindArgument(match.Item2, c.ToArray()).LazyEvaluate();
            }

            return body.Evaluate();

        public Tuple<List<Expression>, Expression> MatchPattern(Expression[] args) {
                var c = new List<Expression>(args.Length);

                foreach (var p in Patterns) {
                    if (p.Item1.Match(c, args))
                        return Tuple.Create(c, p.Item2);

                    c.Clear();
                }

                return null;
            }
        }
#endif
}
