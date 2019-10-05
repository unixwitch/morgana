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
    public interface ISymbolTable {
        Expression GetSymbol(string name);
        void SetSymbol(string name, Expression value);

        ISymbolTable Globals { get; }
        void SetGlobalSymbol(string name, Expression value);

        ISymbolTable Combine(ISymbolTable other);
    }

    /*
     * An ISymbolTable that supports nested scopes and global variables.
     * The top-most EmitSymbolTable is the global symbol table.  Symbols set using
     * SetSymbol() in nested scopes do not affect parent scopes; symbols set using
     * SetGlobalSymbol() are propagated up to the global scope.
     */
    public class SymbolTable : ISymbolTable {
        protected Dictionary<string, Expression> Symbols { get; set; }
        protected ISymbolTable Parent { get; set; }
        public ISymbolTable Globals {
            get {
                if (Parent == null)
                    return this;
                return Parent.Globals;
            }
        }

        public SymbolTable() {
            Symbols = new Dictionary<string, Expression>();
        }

        public SymbolTable(ISymbolTable parent) {
            Symbols = new Dictionary<string, Expression>();
            Parent = parent;
        }

        public Expression GetSymbol(string name) {
            if (Symbols.TryGetValue(name, out Expression value))
                return value;
            if (Parent != null)
                return Parent.GetSymbol(name);
            return null;
        }

        public void SetSymbol(string name, Expression value) {
            Symbols[name] = value;
        }

        public void SetGlobalSymbol(string name, Expression value) {
            if (Parent != null)
                Parent.SetGlobalSymbol(name, value);
            else
                SetSymbol(name, value);
        }

        public ISymbolTable Combine(ISymbolTable other) {
            var ret = new SymbolTable(this);

            foreach (var s in Symbols)
                ret.SetSymbol(s.Key, s.Value);

            return ret;
        }

        public override string ToString() {
            return "<symbols " + String.Join(",", Symbols.Keys) + ">";
        }
    }
}
