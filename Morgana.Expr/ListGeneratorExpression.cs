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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgana.Expr {
    public class ListGeneratorExpression : ListExpression {
        private long _start;
        private long _increment;
        private long? _end;

        public override bool IsInfinite => _end == null || (_end > _start && _increment <= 0) || (_end < _start && _increment >= 0);

        public ListGeneratorExpression(long first, long second, long? last) {
            _start = first;
            _increment = second - first;
            _end = last;
        }

        public override IEnumerator<Expression> GetEnumerator() {
            for (long i = _start; _end == null || i <= _end; i += _increment)
                yield return new IntegerExpression(i);
        }

        public override Expression Evaluate(ExpressionContext ctx, Expression[] args) {
            return this;
        }

        public override string ToString() {
            if (_end != null)
                if (_increment == 1)
                    return $"[{_start}..{_end}]";
                else
                    return $"[{_start}, {_start + _increment}..{_end}]";
            else
                return $"[{_start}, {_start + _increment}..]";
        }
    }
}
