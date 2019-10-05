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
     * This is the lexer (tokeniser) for expressions.
     */

    public class FilePosition {
        public string LineText;
        public int Line;
        public int Column;
    }

    /* Base type for a token */
    public class Token {
        public string Match;
        public FilePosition Location;
        public int MatchLength;
    }

    /* Operator tokens */
    public class TNewline : Token { }     // \r or \n
    public class TComment : Token { }     // --, never returned to user
    public class TColonColon : Token { }  // ::
    public class TPlus : Token { }        // +
    public class TMinus : Token { }       // -
    public class TStar : Token { }        // *
    public class TSlash : Token { }       // /
    public class TLParen : Token { }      // (
    public class TRParen : Token { }      // )
    public class TLBrace : Token { }      // {
    public class TRBrace : Token { }      // }
    public class TTilde : Token { }       // ~
    public class TLSq : Token { }         // [
    public class TRSq : Token { }         // ]
    public class TComma : Token { }       // ,
    public class TBackslash : Token { }   // \
    public class TLT : Token { }          // <
    public class TGT : Token { }          // >
    public class TLTE : Token { }         // <=
    public class TGTE : Token { }         // >=
    public class TEq : Token { }          // ==
    public class TAssign : Token { }      // :=
    public class TColon : Token { }       // :
    public class TDotDot : Token { }      // ..
    public class TDot : Token { }         // .
    public class TPlusPlus : Token { }    // ++
    public class TPercent : Token { }     // %
    public class TAndAnd : Token { }      // &&
    public class TOrOr : Token { }        // ||
    public class TBang : Token { }        // !
    public class TNotEq : Token { }       // !=
    public class TBangBang : Token { }    // !!
    public class TArrow : Token { }       // ->
    public class TUnderscore : Token { }  // _
    public class TDollar : Token { }      // $
    public class TNull : Token { }        // null
    public class TIf : Token { }          // if
    public class TThen : Token { }        // then
    public class TElse : Token { }        // else
    public class TCase : Token { }        // case
    public class TOf : Token { }          // of
    public class TWith : Token { }        // with

    /* Identifier */
    public class TIdentifier : Token {
        public string Name { get; private set; }
        public TIdentifier(string name) {
            Name = name;
        }
    }

    /* Literal integer */
    public class TInteger : Token {
        public long Value { get; private set; }
        public TInteger(long value) {
            Value = value;
        }
    }

    /* Literal decimal */
    public class TDecimal : Token {
        public decimal Value { get; private set; }
        public TDecimal(decimal value) {
            Value = value;
        }
    }

    /* Literal boolean */
    public class TBoolean : Token {
        public bool Value { get; private set; }
        public TBoolean(bool value) {
            Value = value;
        }
    }

    /* Literal string */
    public class TString : Token {
        public enum Type {
            QuotedString,
            ParsedString,
        };

        public string Value { get; private set; }
        public Type StringType { get; private set; }

        public TString(Type typ, string value) {
            Value = value;
            StringType = typ;
        }
    }

    public class Lexer {
        /*
         * The result of a match operator.
         */
        public class MatchResult {
            public Token Matched;
            public int MatchLength;
        };

        /*
         * Base class for all matchers.
         */
        public abstract class Matcher {
            public abstract MatchResult Match(string text);
        }

        /*
         * Match a token using a regex.
         */
        public class RegexMatcher : Matcher {
            protected string Regex { get; set; }
            protected Func<Match, Token> Action { get; set; }

            public RegexMatcher(string regex, Func<Match, Token> action) {
                Regex = regex;
                Action = action;
            }

            public override MatchResult Match(string text) {
                Regex r = new Regex("^" + Regex, RegexOptions.Singleline);
                Match m = r.Match(text);

                if (!m.Success)
                    return null;

                return new MatchResult {
                    Matched = Action(m),
                    MatchLength = m.Value.Length,
                };
            }
        };

        /*
         * Match a literal double quoted string.
         */
        public class QuotedStringMatcher : Matcher {
            enum State { Start1, InString, Backslash };

            public override MatchResult Match(string text) {
                if (text.Length < 1)
                    return null;

                string result = "";
                var state = State.InString;

                var typ = TString.Type.QuotedString;
                int skip = 0;
                if (text[0] == 'p') {
                    typ = TString.Type.ParsedString;
                    skip++;
                }

                if (text[skip] != '"')
                    return null;

                for (int i = skip + 1; i < text.Length; i++) {
                    switch (state) {
                        case State.InString:
                            switch (text[i]) {
                                case '\\':
                                    state = State.Backslash;
                                    break;

                                case '"':
                                    return new MatchResult {
                                        Matched = new TString(typ, result),
                                        MatchLength = 1 + i,
                                    };

                                default:
                                    result += text[i];
                                    break;
                            }
                            break;

                        case State.Backslash:
                            state = State.InString;

                            switch (text[i]) {
                                case '\\':
                                case '"':
                                    result += text[i];
                                    break;
                                case 'n':
                                    result += '\n';
                                    break;
                                case 'r':
                                    result += '\r';
                                    break;
                                default:
                                    throw new ParseException($"unrecognised escape \\{text[i]} in string", null);
                            }
                            break;
                    }
                }

                throw new ParseException("unexpected end of string", null);
            }
        }

        static readonly List<Matcher> Matchers = new List<Matcher> {
            new QuotedStringMatcher(),
            new RegexMatcher(@"{([a-zA-Z0-9_ -]+)}", x => new TIdentifier(x.Groups[1].Value)),

            new RegexMatcher(@"\r?\n",  x => new TNewline()),
            new RegexMatcher(@"--",     x => new TComment()),
            new RegexMatcher(@"::",     x => new TColonColon()),

            /* Operators */
            new RegexMatcher(@"\+\+",   x => new TPlusPlus()),
            new RegexMatcher(@"!!",     x => new TBangBang()),
            new RegexMatcher(@"->",     x => new TArrow()),
            new RegexMatcher(@"\+",     x => new TPlus()),
            new RegexMatcher(@"-",      x => new TMinus()),
            new RegexMatcher(@"/",      x => new TSlash()),
            new RegexMatcher(@"\*",     x => new TStar()),
            new RegexMatcher(@"\(",     x => new TLParen()),
            new RegexMatcher(@"\)",     x => new TRParen()),
            new RegexMatcher(@"\{",     x => new TLBrace()),
            new RegexMatcher(@"\}",     x => new TRBrace()),
            new RegexMatcher(@"~",      x => new TTilde()),
            new RegexMatcher(@"<=",     x => new TLTE()),
            new RegexMatcher(@">=",     x => new TGTE()),
            new RegexMatcher(@"==",     x => new TEq()),
            new RegexMatcher(@"!=",     x => new TNotEq()),
            new RegexMatcher(@"=",      x => new TAssign()),
            new RegexMatcher(@"&&",     x => new TAndAnd()),
            new RegexMatcher(@"\|\|",   x => new TOrOr()),
            new RegexMatcher(@":",      x => new TColon()),
            new RegexMatcher(@"<",      x => new TLT()),
            new RegexMatcher(@">",      x => new TGT()),
            new RegexMatcher(@"%",      x => new TPercent()),
            new RegexMatcher(@"!",      x => new TBang()),
            new RegexMatcher(@"\$",     x => new TDollar()),
            new RegexMatcher(@"\[",     x => new TLSq()),
            new RegexMatcher(@"\]",     x => new TRSq()),
            new RegexMatcher(@",",      x => new TComma()),
            new RegexMatcher(@"_",      x => new TUnderscore()),
            new RegexMatcher(@"\.\.",   x => new TDotDot()),
            new RegexMatcher(@"\\",     x => new TBackslash()),

            /* Literals */
            new RegexMatcher(@"[0-9]+\.(?!\.)([0-9]+)?", x => new TDecimal(Decimal.Parse(x.Value))),
            new RegexMatcher(@"\.[0-9]+", x => new TDecimal(Decimal.Parse(x.Value))),
            new RegexMatcher(@"[0-9]+", x => new TInteger(Int64.Parse(x.Value))),
            new RegexMatcher(@"true", x => new TBoolean(true)),
            new RegexMatcher(@"false", x => new TBoolean(false)),
            new RegexMatcher(@"null", x => new TNull()),
            new RegexMatcher(@"if", x => new TIf()),
            new RegexMatcher(@"then", x => new TThen()),
            new RegexMatcher(@"else", x => new TElse()),
            new RegexMatcher(@"case", x => new TCase()),
            new RegexMatcher(@"of", x => new TOf()),
            new RegexMatcher(@"with", x => new TWith()),
            new RegexMatcher(@"\.",     x => new TDot()),

            /* Identifiers */
            new RegexMatcher(@"[a-zA-Z_]['a-zA-Z0-9_$]*", x => new TIdentifier(x.Value)),
        };

        public string Input { get; set; }
        public int Chars { get; protected set; } = 0;
        protected Token _currentToken = null;
        protected string _currentline;
        protected int lineno = 0;
        protected int linelen;

        public Lexer(string input) {
            Input = input;
            NewLine();
        }

        protected Token Lex() {
            Token token;

            while ((token = LexOnce()) != null) {
                /*
                 * For comments, remove and skip to end of line; we never return
                 * TComment to the caller because comments can occur almost anywhere
                 * and it would be impossible to handle.
                 */
                if (token is TComment) {
                    var end = Input.IndexOf('\n');
                    if (end == -1)
                        break;
                    int i = Input.Length;
                    Input = Input.Substring(end);
                    Chars += (i - Input.Length);
                    NewLine();
                    continue;
                }

                if (token is TNewline)
                    NewLine();

                return token;
            }

            return null;
        }

        protected void NewLine() {
            ++lineno;
            linelen = Input.Length;

            var end = Input.IndexOf('\n');
            if (end == -1)
                _currentline = Input;
            else
                _currentline = Input.Substring(end);
        }

        public Token LexOnce() {
            int i = Input.Length;
            Input = Input.TrimStart(' ', '\t');
            Chars += (i - Input.Length);

            /* If empty string, we're at EOF */
            if (Input.Length == 0)
                return null;

            /* Try all matchers and return the first match */
            foreach (var matcher in Matchers) {
                var match = matcher.Match(Input);
                if (match == null)
                    continue;

                Input = Input.Substring(match.MatchLength);

                match.Matched.MatchLength = match.MatchLength;
                match.Matched.Location = new FilePosition {
                    LineText = _currentline,
                    Line = lineno,
                    Column = linelen - (Input.Length + match.MatchLength),
                };

                Chars += match.MatchLength;
                return match.Matched;
            }

            throw new ParseException($"invalid input character(s) U+{(int)Input[0]:x4} starting at: {Input}", null);
        }

        public Token Next {
            get {
                if (_currentToken is null)
                    _currentToken = Lex();
                return _currentToken;
            }
        }

        public void Consume() {
            if (_currentToken is null)
                throw new ExpressionException("nothing left to consume in lexer");
            _currentToken = null;
        }
    }
}
