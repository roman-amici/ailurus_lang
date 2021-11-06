using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;
using AilurusLang.Parsing.Errors;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.Parsers
{
    public class RecursiveDescentParser
    {
        private static readonly Token EofToken = new Token(TokenType.EOF, string.Empty, 0, 0, string.Empty);

        List<Token> Tokens { get; set; }
        int Current { get; set; }

        bool IsValid { get; set; }

        bool IsAtEnd { get => Current >= Tokens.Count; }
        Token Peek { get => IsAtEnd ? EofToken : Tokens[Current]; }
        Token Previous { get => Tokens[Current - 1]; }

        // Error Handling
        void RaiseError(Token token, string errorMessage)
        {
            IsValid = false;
            throw new ParsingError(token, errorMessage);
        }

        void ReportWarning(Token token, string warning)
        {
            Console.WriteLine($"{token.SourceFile}:{token.Line}:{token.Column} - {warning}");
        }

        // Token Stream querying
        bool Check(TokenType type)
        {
            return IsAtEnd ? false : Peek.Type == type;
        }

        Token Advance()
        {
            if (!IsAtEnd)
            {
                Current++;
            }

            return Previous;
        }

        bool Match(params TokenType[] toMatch)
        {
            foreach (var token in toMatch)
            {
                if (Check(token))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        void Consume(TokenType type, string errorMessage)
        {
            if (!Check(type))
            {
                RaiseError(Peek, errorMessage);
            }
        }

        public ExpressionNode ParseExpression(List<Token> tokens)
        {
            Tokens = tokens;
            try
            {
                return Expression();
            }
            catch (ParsingError error)
            {
                Console.WriteLine(error);
            }

            return null;
        }

        // Base rule for things like identifiers and constants
        ExpressionNode Primary()
        {
            // Constants
            if (Match(TokenType.Number))
            {
                return DetermineNumberLiteral(Previous);
            }
            if (Match(TokenType.Str))
            {
                return new Literal()
                {
                    Value = Previous.Identifier,
                    ValueType = StringType.Instance,
                };
            }
            if (Match(TokenType.False))
            {
                return new Literal()
                {
                    Value = false,
                    ValueType = BooleanType.Instance
                };
            }
            if (Match(TokenType.True))
            {
                return new Literal()
                {
                    Value = true,
                    ValueType = BooleanType.Instance
                };
            }
            if (Match(TokenType.Null))
            {
                return new Literal()
                {
                    Value = null,
                    ValueType = NullType.Instance
                };
            }

            // Grouping
            if (Match(TokenType.LeftParen))
            {
                var inner = Expression();
                return new Grouping()
                {
                    Inner = inner
                };
            }

            // Identifier-based
            if (Match(TokenType.Identifier))
            {
                var name = Previous;
                if (Check(TokenType.RightBrace))
                {
                    return StructInitializer(name);
                }
                else
                {
                    return new Variable()
                    {
                        Name = name
                    };
                }
            }

            if (Match(TokenType.If))
            {
                return IfExpression();
            }

            RaiseError(Previous, "Unrecognized token");
            return null;
        }

        StructInitialization StructInitializer(Token name)
        {
            Consume(TokenType.LeftBrace, "Expected '{' after struct name");

            var initializers = new List<(Token, ExpressionNode)>();
            while (Peek.Type != TokenType.RightBrace && !IsAtEnd)
            {
                Consume(TokenType.Identifier, "Expected member name.");
                var member = Previous;
                Consume(TokenType.Colon, "Expected ':' after member name.");
                var expression = Expression();

                initializers.Add((member, expression));
            }

            if (IsAtEnd)
            {
                RaiseError(name, "Unterminated struct initializer");
            }

            return new StructInitialization()
            {
                StructName = name,
                Initializers = initializers
            };
        }

        ExpressionNode IfExpression()
        {
            var predicate = Or();
            Consume(TokenType.Then, "Expected 'then' in if expression");
            var trueExpr = Expression();
            Consume(TokenType.Else, "Expected 'else' in if expression");
            var falseExpr = Expression();

            return new IfExpression()
            {
                Predicate = predicate,
                TrueExpr = trueExpr,
                FalseExpr = falseExpr
            };
        }

        ExpressionNode Unary()
        {
            if (Match(
                TokenType.Bang,
                TokenType.Minus,
                TokenType.At
            ))
            {
                var op = Previous;
                var expr = Primary();

                return new Unary()
                {
                    Operator = op,
                    Expr = expr
                };
            }

            // This will be 'call' instead eventually
            return Primary();
        }

        ExpressionNode Factor()
        {
            var left = Unary();

            while (Match(
                TokenType.Star,
                TokenType.Slash,
                TokenType.Mod
            ))
            {
                var op = Previous;
                var right = Unary();
                left = new Binary()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        ExpressionNode Term()
        {
            var left = Factor();

            while (Match(TokenType.Minus, TokenType.Plus))
            {
                var op = Previous;
                var right = Factor();
                left = new Binary()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        ExpressionNode Bitwise()
        {
            var left = Term();

            while (Match(
                TokenType.Amp,
                TokenType.Bar,
                TokenType.Carrot
            ))
            {
                var op = Previous;
                var right = Term();

                left = new Binary()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        ExpressionNode Comparison()
        {
            var left = Bitwise();

            while (Match(TokenType.Less,
                TokenType.LessEqual,
                TokenType.Greater,
                TokenType.GreaterEqual
            ))
            {
                var op = Previous;
                var right = Bitwise();

                left = new Binary()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        ExpressionNode Equality()
        {
            var left = Comparison();

            while (Match(TokenType.BangEqual, TokenType.EqualEqual))
            {
                var op = Previous;
                var right = Comparison();

                left = new Binary()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;

        }

        ExpressionNode And()
        {
            var left = Equality();

            while (Match(TokenType.AmpAmp))
            {
                var op = Previous;
                var right = Equality();

                left = new BinaryShortCircut()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;

        }

        ExpressionNode Or()
        {
            var left = And();

            while (Match(TokenType.BarBar))
            {
                var op = Previous;
                var right = And();

                left = new BinaryShortCircut()
                {
                    Operator = op,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        ExpressionNode Expression()
        {
            // Assignment goes here eventually.
            return Or();
        }

        #region Helper Functions

        Literal DetermineNumberLiteral(Token token)
        {
            if (token.Identifier.Contains("."))
            {
                // For now, just use one int and one float type
                var value = double.Parse(token.Identifier);
                return new Literal()
                {
                    Value = value,
                    ValueType = DoubleType.Instance,
                };
            }
            else
            {
                var value = int.Parse(token.Identifier);
                return new Literal()
                {
                    Value = value,
                    ValueType = IntType.InstanceSigned,
                };
            }
        }

        #endregion
    }
}