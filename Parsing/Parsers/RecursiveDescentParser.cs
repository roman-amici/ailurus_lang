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

        public bool IsValid { get; set; }

        bool IsAtEnd { get => Current >= Tokens.Count; }
        Token Peek { get => IsAtEnd ? EofToken : Tokens[Current]; }
        Token Previous { get => Tokens[Current - 1]; }

        void Reset()
        {
            Current = 0;
            IsValid = true;
            Tokens = null;
        }

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

        Token Consume(TokenType type, string errorMessage)
        {
            if (!Check(type))
            {
                RaiseError(Peek, errorMessage);
            }
            return Advance();
        }

        public List<StatementNode> Parse(List<Token> tokens)
        {
            Reset();
            Tokens = tokens;

            var statements = new List<StatementNode>();
            while (!IsAtEnd)
            {
                statements.Add(ParseStatement());
            }

            return statements;
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
                    DataType = StringType.Instance,
                    SourceStart = Previous,
                };
            }
            if (Match(TokenType.False))
            {
                return new Literal()
                {
                    Value = false,
                    DataType = BooleanType.Instance,
                    SourceStart = Previous
                };
            }
            if (Match(TokenType.True))
            {
                return new Literal()
                {
                    Value = true,
                    DataType = BooleanType.Instance,
                    SourceStart = Previous
                };
            }
            if (Match(TokenType.Null))
            {
                return new Literal()
                {
                    Value = null,
                    DataType = NullType.Instance,
                    SourceStart = Previous
                };
            }

            // Grouping
            if (Match(TokenType.LeftParen))
            {
                var sourceStart = Previous;
                var inner = Expression();
                return new Grouping()
                {
                    Inner = inner,
                    SourceStart = sourceStart
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
                        Name = name,
                        SourceStart = Previous
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
            var sourceStart = Previous;
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
                Initializers = initializers,
                SourceStart = sourceStart
            };
        }

        ExpressionNode IfExpression()
        {
            var sourceStart = Previous;
            var predicate = Or();
            Consume(TokenType.Then, "Expected 'then' in if expression");
            var trueExpr = Expression();
            Consume(TokenType.Else, "Expected 'else' in if expression");
            var falseExpr = Expression();

            return new IfExpression()
            {
                Predicate = predicate,
                TrueExpr = trueExpr,
                FalseExpr = falseExpr,
                SourceStart = sourceStart
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
                    Expr = expr,
                    SourceStart = Previous
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
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
                    Right = right,
                    SourceStart = op
                };
            }

            return left;
        }

        ExpressionNode Assignment()
        {
            var expr = Or();

            if (Match(TokenType.Equal))
            {
                var equalsToken = Previous;
                var rvalue = Assignment(); // right associative

                if (expr is Variable v)
                {
                    return new Assign()
                    {
                        Name = v.Name,
                        Assignment = rvalue,
                        SourceStart = equalsToken
                    };
                }
                // Todo: Struct assignment
                else
                {
                    RaiseError(equalsToken, $"Cannot assign to {expr.SourceStart.Lexeme}.");
                }
            }

            return expr;
        }

        ExpressionNode Expression()
        {
            // Assignment goes here eventually.
            return Assignment();
        }

        #region Declarations

        TypeName TypeName()
        {
            var name = Consume(TokenType.Identifier, "Expected type name after ':'");
            bool isPtr = false;
            if (Match(TokenType.Ptr))
            {
                isPtr = true;
            }

            return new TypeName()
            {
                Name = name,
                IsPtr = isPtr
            };
        }

        #endregion

        #region Statements

        StatementNode ExpressionStatement()
        {
            var sourceStart = Peek;
            var expr = Expression();

            Consume(TokenType.Semicolon, "Expected ';' after expression");

            return new ExpressionStatement()
            {
                Expr = expr,
                SourceStart = sourceStart
            };
        }

        StatementNode LetStatement()
        {
            TypeName assertedType = null;
            ExpressionNode initializer = null;
            bool isMutable = false;

            var name = Consume(TokenType.Identifier, "Expected name after 'let'");

            // TODO: Parse static and volatile as well
            if (Match(TokenType.Mut))
            {
                isMutable = true;
            }

            if (Match(TokenType.Colon))
            {
                assertedType = TypeName();
            }

            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }

            if (initializer == null && assertedType == null)
            {
                throw new ParsingError(name, "Variable delaration must have either an explicit type or an assignment statement.");
            }

            return new LetStatement()
            {
                Name = name,
                Initializer = initializer,
                AssertedType = assertedType,
                IsMutable = isMutable
            };
        }

        StatementNode ParseStatement()
        {
            if (Match(TokenType.Let))
            {
                return LetStatement();
            }

            return ExpressionStatement();
        }

        #endregion

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
                    DataType = DoubleType.Instance,
                    SourceStart = token
                };
            }
            else
            {
                var value = int.Parse(token.Identifier);
                return new Literal()
                {
                    Value = value,
                    DataType = IntType.InstanceSigned,
                    SourceStart = token
                };
            }
        }

        #endregion
    }
}