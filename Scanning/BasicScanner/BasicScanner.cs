using System;
using System.Collections.Generic;

namespace AilurusLang.Scanning.BasicScanner
{
    static class Extentions
    {
        public static string SubstringRange(this String str, int start, int end)
        {
            return str.Substring(start, end - start);
        }
    }

    class Scanner
    {
        readonly static KeywordDictionary Keywords = new KeywordDictionary();

        string Source { get; set; }
        string SourceFileName { get; set; }
        int Start { get; set; } = 0;
        int Current { get; set; } = 0;
        long Line { get; set; } = 1;
        long Column { get; set; } = 1;

        long LastLineBreak { get; set; } = 0;

        public bool HadError { get; set; } = false;

        bool IsAtEnd { get => Current >= Source.Length; }
        char CurrentChar { get => IsAtEnd ? '\0' : Source[Current]; }
        char PreviousChar { get => Source[Current - 1]; }
        char NextChar
        {
            get
            {
                var next = Current + 1;
                if (IsAtEnd)
                {
                    return '\0';
                }
                else if (next >= Source.Length)
                {
                    return '\0';
                }
                else
                {
                    return Source[next];
                }
            }
        }

        char Advance()
        {
            char val = CurrentChar;
            if (val == '\n')
            {
                Line++;
                LastLineBreak = Current;

            }
            Current++;
            return val;
        }

        bool Match(char expected)
        {
            if (IsAtEnd)
            {
                return false;
            }

            if (CurrentChar != expected)
            {
                return false;
            }

            Current++;
            return true;
        }

        Token CreateToken(TokenType type)
        {
            var text = Source.SubstringRange(Start, Current);
            return new Token(type, text, Line, Column, SourceFileName);
        }

        Token CreateToken(TokenType type, string literal)
        {
            var text = Source.SubstringRange(Start, Current);
            return new Token(type, text, Line, Column, SourceFileName, literal);
        }

        void ResetScanner()
        {
            Source = null;
            SourceFileName = null;
            Current = 0;
            Start = 0;
            Line = 1;
            Column = 1;
            LastLineBreak = 0;
            HadError = false;
        }

        public List<Token> Scan(string source, string sourceFile)
        {
            ResetScanner();
            Source = source;
            SourceFileName = sourceFile;

            List<Token> tokens = new List<Token>();

            while (!IsAtEnd)
            {
                UpdateStart();

                try
                {
                    var token = ScanToken();
                    if (token != null)
                    {
                        tokens.Add(token);
                    }
                }
                catch (SyntaxError s)
                {
                    Console.WriteLine(s);
                    HadError = true;
                }
            }

            return tokens;
        }

        void UpdateStart()
        {
            if (Start < LastLineBreak)
            {
                Column = LastLineBreak;
            }

            Start = Current;
        }

        Token ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': return CreateToken(TokenType.LeftParen);
                case ')': return CreateToken(TokenType.RightParen);
                case '[': return CreateToken(TokenType.LeftBracket);
                case ']': return CreateToken(TokenType.RightBracket);
                case '{': return CreateToken(TokenType.LeftBrace);
                case '}': return CreateToken(TokenType.RightBrace);
                case ',': return CreateToken(TokenType.Comma);
                case '.': return CreateToken(TokenType.Dot);
                case '+': return CreateToken(TokenType.Plus);
                case ';': return CreateToken(TokenType.Semicolon);
                case '?': return CreateToken(TokenType.QuestionMark);
                case '*': return CreateToken(TokenType.Star);
                case '@': return CreateToken(TokenType.At);
                case '$':
                    if (Match('$'))
                    {
                        return CreateToken(TokenType.DollarDollar);
                    }
                    else
                    {
                        return CreateToken(TokenType.DollarSign);
                    }

                case '!':
                    if (Match('='))
                    {
                        return CreateToken(TokenType.BangEqual);
                    }
                    else
                    {
                        return CreateToken(TokenType.Bang);
                    }
                case '=':
                    if (Match('='))
                    {
                        return CreateToken(TokenType.EqualEqual);
                    }
                    else if (Match('>'))
                    {
                        return CreateToken(TokenType.FatArrow);
                    }
                    else
                    {
                        return CreateToken(TokenType.Equal);
                    }
                case '>':
                    if (Match('='))
                    {
                        return CreateToken(TokenType.GreaterEqual);
                    }
                    else
                    {
                        return CreateToken(TokenType.Greater);
                    }
                case '<':
                    if (Match('='))
                    {
                        return CreateToken(TokenType.LessEqual);
                    }
                    else if (Match('-'))
                    {
                        return CreateToken(TokenType.BackwardArrow);
                    }
                    else
                    {
                        return CreateToken(TokenType.Less);
                    }
                case '-':
                    if (Match('>'))
                    {
                        return CreateToken(TokenType.Arrow);
                    }
                    else
                    {
                        return CreateToken(TokenType.Minus);
                    }
                case '&':
                    if (Match('&'))
                    {
                        return CreateToken(TokenType.AmpAmp);
                    }
                    else
                    {
                        return CreateToken(TokenType.Amp);
                    }
                case '|':
                    if (Match('|'))
                    {
                        return CreateToken(TokenType.BarBar);
                    }
                    else
                    {
                        return CreateToken(TokenType.Bar);
                    }
                case '^':
                    if (Match('^'))
                    {
                        return CreateToken(TokenType.CarrotCarrot);
                    }
                    else
                    {
                        return CreateToken(TokenType.Carrot);
                    }
                case ':':
                    if (Match(':'))
                    {
                        return CreateToken(TokenType.ColonColon);
                    }
                    else
                    {
                        return CreateToken(TokenType.Colon);
                    }

                case '/':
                    if (Match('/'))
                    {
                        ConsumeSingleLineComment();
                        return null;
                    }
                    else if (Match('*'))
                    {
                        ConsumeMultilineComment();
                        return null;
                    }
                    else
                    {
                        return CreateToken(TokenType.Slash);
                    }
                case '"':
                    return ScanString();
                case '\'':
                    return ScanChar();

                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    return null;

                default:
                    if (IsAlpha(c))
                    {
                        return ScanIdentifier();
                    }
                    else if (IsDigit(c))
                    {
                        return ScanNumber();
                    }
                    throw new SyntaxError($"Unrecognized Token {c}", SourceFileName, Line, Column);
            }
        }

        void ConsumeSingleLineComment()
        {
            while (CurrentChar != '\n' && !IsAtEnd)
            {
                Advance();
            }
        }

        void ConsumeMultilineComment()
        {
            while (true)
            {
                if (CurrentChar == '*' && NextChar == '/')
                {
                    // Consume the * and the /
                    Advance();
                    Advance();
                    return;
                }
                Advance();
            }
        }

        Token ScanChar()
        {
            while (CurrentChar != '\'' && !IsAtEnd)
            {
                Advance();
            }

            if (IsAtEnd)
            {
                throw new SyntaxError("Unterminated char literal.", SourceFileName, Line, Column);
            }

            //Consume the closing "'" char
            Advance();

            var text = Source.SubstringRange(Start + 1, Current - 1);
            if (text.Length < 0)
            {
                throw new SyntaxError("Emptry char literal.", SourceFileName, Line, Column);
            }

            return CreateToken(TokenType.CharConstant, text);
        }

        Token ScanString()
        {
            while (CurrentChar != '"' && !IsAtEnd)
            {
                Advance();
            }

            if (IsAtEnd)
            {
                throw new SyntaxError("Unterminated string.", SourceFileName, Line, Column);
            }

            // Consume the closeing '"' char
            Advance();

            // Skip the opening and closing '"'
            var text = Source.SubstringRange(Start + 1, Current - 1);
            return CreateToken(TokenType.StringConstant, text);
        }

        bool IsAlpha(char c)
        {
            var isLower = c >= 'a' && c <= 'z';
            var isUpper = c >= 'A' && c <= 'Z';
            var isUnderscore = c == '_';

            return isLower || isUpper || isUnderscore;
        }

        bool IsDigit(char c)
        {
            return '0' <= c && c <= '9';
        }

        bool IsHexDigit(char c)
        {
            return IsDigit(c) ||
                'a' <= c && c <= 'f' ||
                'A' <= c && c <= 'F';
        }

        bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        Token ScanIdentifier()
        {
            while (IsAlphaNumeric(CurrentChar))
            {
                Advance();
            }

            var identifier = Source.SubstringRange(Start, Current);

            if (Keywords.ContainsKey(identifier))
            {
                return CreateToken(Keywords[identifier]);
            }
            else
            {
                return CreateToken(TokenType.Identifier, identifier);
            }
        }

        void MatchInteger()
        {
            while (IsDigit(CurrentChar))
            {
                Advance();
            }
        }

        Token ScanNumber()
        {
            if (PreviousChar == '0')
            {
                if (CurrentChar == 'x' || CurrentChar == 'X')
                {
                    Advance();
                    while (IsHexDigit(CurrentChar))
                    {
                        Advance();
                    }
                }
                else if (CurrentChar == 'b' || CurrentChar == 'B')
                {
                    Advance();
                    while (CurrentChar == '0' || CurrentChar == '1')
                    {
                        Advance();
                    }
                }
                else // Still consume a decimal if it starts with 0
                {
                    MatchInteger();

                    if (Match('.'))
                    {
                        MatchInteger();
                    }
                }
            }
            else
            {
                MatchInteger();

                if (Match('.'))
                {
                    MatchInteger();
                }
            }

            // Consume number literal type specifier
            if (Match('i') || Match('u') || Match('f'))
            {
                MatchInteger();
            }

            var numberString = Source.SubstringRange(Start, Current);
            return CreateToken(TokenType.Number, numberString);
        }

    }
}