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

#if false
namespace Morgana.Expr {
    public abstract class ExpressionType {
        public abstract string Name { get; }
        public override string ToString() => Name;

        public abstract ExpressionType ReturnType { get; }
        public abstract IEnumerable<ExpressionType> ArgumentTypes { get; }

        public static bool operator ==(ExpressionType lhs, ExpressionType rhs) {
            return lhs?.Name == rhs?.Name;
        }
        public static bool operator !=(ExpressionType lhs, ExpressionType rhs) => !(lhs == rhs);
    }

    public class AnyType : ExpressionType {
        public readonly static NullType Instance = new NullType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();

        public override string Name { get; } = "any";
    }

    public class NullType : ExpressionType {
        public readonly static NullType Instance = new NullType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();

        public override string Name { get; } = "nulltype";
    }

    public class BooleanType : ExpressionType {
        public readonly static BooleanType Instance = new BooleanType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();
        public override string Name { get; } = "boolean";
    }

    public class StringType : ExpressionType {
        public readonly static StringType Instance = new StringType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();
        public override string Name { get; } = "string";
    }

    public class IntegerType : ExpressionType {
        public readonly static IntegerType Instance = new IntegerType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();
        public override string Name { get; } = "integer";
    }

    public class DecimalType : ExpressionType {
        public readonly static DecimalType Instance = new DecimalType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();
        public override string Name { get; } = "decimal";
    }

    public class DateTimeType : ExpressionType {
        public readonly static DateTimeType Instance = new DateTimeType();
        public override ExpressionType ReturnType => Instance;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();
        public override string Name { get; } = "datetime";
    }

    public class ListType : ExpressionType {
        public ExpressionType ElementType { get; }

        public override ExpressionType ReturnType => this;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();

        public ListType(ExpressionType elms) {
            ElementType = elms;
        }

        public override string Name {
            get {
                return $"list<{ElementType.Name}>";
            }
        }
    }

    public class MapType : ExpressionType {
        public ExpressionType KeyType { get; }
        public ExpressionType ValueType { get; }
        public override ExpressionType ReturnType => this;
        public override IEnumerable<ExpressionType> ArgumentTypes => new List<ExpressionType>();

        public MapType(ExpressionType ks, ExpressionType vs) {
            KeyType = ks;
            ValueType = vs;
        }

        public override string Name {
            get {
                return $"map<{KeyType.Name}, {ValueType.Name}>";
            }
        }
    }

    public class FunctionType : ExpressionType {
        public override string Name { get; } = "function";

        public override ExpressionType ReturnType { get; }
        public override IEnumerable<ExpressionType> ArgumentTypes { get; }

        public FunctionType(ExpressionType ret, IEnumerable<ExpressionType> args) {
            ReturnType = ret;
            ArgumentTypes = args;
        }

        public override string ToString() {
            if (ArgumentTypes.Count() > 0)
                return "(" + String.Join(" -> ", ArgumentTypes.Select(x => x.ToString()))
                    + " -> " + ReturnType.ToString() + ")";
            else
                return ReturnType.ToString();
        }
    }
}
#endif