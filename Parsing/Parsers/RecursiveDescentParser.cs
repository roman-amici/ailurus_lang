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

        void Warning(string warningMessage, Token t)
        {
            Console.WriteLine($"{t.SourceFile}:{t.Line}:{t.Column} - Warning: {warningMessage}");
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

        QualifiedName ConsumeQualifiedName()
        {
            var name = new List<Token>();

            do
            {
                var namePart = Consume(TokenType.Identifier, "Expected identifier.");
                name.Add(namePart);
            } while (Match(TokenType.ColonColon));

            return new QualifiedName()
            {
                Name = name
            };
        }

        QualifiedName MatchQualifiedName()
        {
            var startIndex = Current;
            var name = new List<Token>();
            do
            {
                if (!Match(TokenType.Identifier))
                {
                    Current = startIndex; // Rewind the index
                    return null;
                }
                name.Add(Previous);
            } while (Match(TokenType.ColonColon));

            return new QualifiedName() { Name = name };
        }

        QualifiedName ConsumeMultiImport(out List<Token> names)
        {
            names = new List<Token>();

            var qualifiedNameParts = new List<Token>();

            bool namesStart = false;
            do
            {
                if (Match(TokenType.LeftBrace))
                {
                    namesStart = true;
                }
                else
                {
                    var namePart = Consume(TokenType.Identifier, "Expected identifier.");
                    qualifiedNameParts.Add(namePart);
                }
            } while (Match(TokenType.ColonColon) && !namesStart);

            if (!namesStart)
            {
                RaiseError(Previous, "Unexpected token.");
            }

            do
            {
                var name = Consume(TokenType.Identifier, "Expected identifier.");
                names.Add(name);
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightBrace, "Expected '}' in import declaration.");

            return new QualifiedName()
            {
                Name = qualifiedNameParts
            };
        }

        public Module Parse(List<Token> tokens)
        {
            Reset();
            Tokens = tokens;

            var module = new Module();
            while (!IsAtEnd)
            {
                try
                {
                    var declaration = Declaration();
                    if (declaration is TypeDeclaration t)
                    {
                        module.TypeDeclarations.Add(t);
                    }
                    else if (declaration is FunctionDeclaration f)
                    {
                        module.FunctionDeclarations.Add(f);
                    }
                    else if (declaration is ModuleVariableDeclaration v)
                    {
                        module.VariableDeclarations.Add(v);
                    }
                    else if (declaration is SubmoduleDeclaration s)
                    {
                        module.SubmoduleDeclarations.Add(s);
                    }
                    else if (declaration is ImportDeclaration i)
                    {
                        module.ImportDeclarations.Add(i);
                    }
                    else if (declaration is MultiImportDeclaration m)
                    {
                        foreach (var name in m.Names)
                        {
                            module.ImportDeclarations.Add(new ImportDeclaration(m.BaseName, name));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                catch (ParsingError e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            }

            if (tokens.Count > 0)
            {
                module.SourceStart = tokens[0];
            }

            return module;
        }

        // Base rule for things like identifiers and constants
        ExpressionNode Primary()
        {
            // Constants
            if (Match(TokenType.Number))
            {
                return DetermineNumberLiteral(Previous);
            }
            if (Match(TokenType.CharConstant))
            {
                var charValue = Previous;
                if (!char.TryParse(charValue.Identifier, out char result))
                {
                    RaiseError(charValue, $"Unrecognized char literal '{charValue.Identifier}'.");
                }

                return new Literal()
                {
                    Value = result,
                    DataType = CharType.Instance,
                    SourceStart = Previous
                };
            }
            if (Match(TokenType.StringConstant))
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

            // Grouping or Tuple
            if (Match(TokenType.LeftParen))
            {
                var paren = Previous;
                var inner = Expression();

                if (Match(TokenType.Comma))
                {
                    return TupleLiteral(paren, inner);
                }
                else
                {
                    // Grouping
                    Consume(TokenType.RightParen, "Unbalanced parenthesis.");
                    return inner;
                }
            }

            if (Match(TokenType.Struct))
            {
                return StructInitializer();
            }

            // Identifier-based
            var qualifiedName = MatchQualifiedName();
            if (qualifiedName != null)
            {
                return new Variable()
                {
                    Name = qualifiedName,
                    SourceStart = qualifiedName.SourceStart
                };
            }

            if (Match(TokenType.If))
            {
                return IfExpression();
            }

            if (Match(TokenType.LeftBracket))
            {
                return ArrayLiteral();
            }

            RaiseError(Peek, "Unexpected token");
            return null;
        }

        AST.TupleExpression TupleLiteral(Token openParen, ExpressionNode firstElement)
        {
            var elements = new List<ExpressionNode>() { firstElement };

            if (Match(TokenType.RightParen))
            {
                RaiseError(Previous, "Expected expression after ','.");
            }

            do
            {
                elements.Add(Expression());
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expected ')' for tuple.");

            return new AST.TupleExpression()
            {
                SourceStart = openParen,
                Elements = elements
            };
        }

        ArrayLiteral ArrayLiteral()
        {
            var sourceStart = Previous;
            var elementsOrFillExpression = ArrayElements();

            ArrayLiteral literal;

            if (Match(TokenType.Semicolon))
            {
                var firstSemicolon = Previous;
                var lengthOrFillExpression = Expression();

                // Element literals, and fill initialization is present
                if (Match(TokenType.Semicolon))
                {
                    var length = Expression();
                    literal = new ArrayLiteral()
                    {
                        FillLength = length,
                        FillExpression = lengthOrFillExpression,
                        Elements = elementsOrFillExpression,
                        SourceStart = sourceStart
                    };
                }
                else
                {
                    //Fill initialization is present
                    if (elementsOrFillExpression.Count != 1)
                    {
                        RaiseError(firstSemicolon, "Expected a single expression for array fill initialize but found a list of expressions.");
                    }

                    literal = new ArrayLiteral()
                    {
                        FillExpression = elementsOrFillExpression[0],
                        FillLength = lengthOrFillExpression,
                        SourceStart = sourceStart
                    };
                }
            }
            else
            {
                // Just element expressions is present
                literal = new ArrayLiteral()
                {
                    Elements = elementsOrFillExpression,
                    SourceStart = sourceStart
                };
            }

            Consume(TokenType.RightBracket, "Expected ']' in array literal.");
            return literal;
        }

        List<ExpressionNode> ArrayElements()
        {

            var elements = new List<ExpressionNode>();
            do
            {
                elements.Add(Expression());
            } while (Match(TokenType.Comma));

            return elements;
        }

        StructInitialization StructInitializer()
        {
            var name = ConsumeQualifiedName();
            var sourceStart = Previous;
            Consume(TokenType.LeftBrace, "Expected '{' after struct name");

            var initializers = new List<(Token, ExpressionNode)>();
            if (!Check(TokenType.RightBrace))
            {
                do
                {
                    Consume(TokenType.Identifier, "Expected member name.");
                    var member = Previous;
                    Consume(TokenType.Colon, "Expected ':' after member name.");
                    var expression = Expression();
                    initializers.Add((member, expression));
                } while (Match(TokenType.Comma));
            }

            if (IsAtEnd)
            {
                RaiseError(name.SourceStart, "Unterminated struct initializer");
            }

            Consume(TokenType.RightBrace, "Expected '}' after struct initializer.");

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
            return FinishIfExpression(sourceStart, predicate); // Split in two to deal with if statement ambiguity
        }

        ExpressionNode FinishIfExpression(Token sourceStart, ExpressionNode predicate)
        {
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
            if (Match(TokenType.AddrOf, TokenType.VarAddrOf))
            {
                var op = Previous;
                var expr = Unary();

                if (expr is Get || expr is Variable || expr is ArrayIndex)
                {
                    return new AddrOfExpression()
                    {
                        OperateOn = expr,
                        SourceStart = op,
                        VarAddr = op.Type == TokenType.VarAddrOf
                    };
                }
            }
            else if (Match(TokenType.New))
            {
                var op = Previous;
                var expr = Unary();

                return new NewAlloc()
                {
                    Expr = expr,
                    SourceStart = op
                };
            }
            else if (Match(TokenType.Var))
            {
                var op = Previous;
                var expr = Unary();

                return new VarCast()
                {
                    Expr = expr,
                    SourceStart = op
                };
            }
            else if (Match(
                TokenType.Bang,
                TokenType.Minus,
                TokenType.LenOf,
                TokenType.At,
                TokenType.New
            ))
            {
                var op = Previous;
                var expr = Unary();

                return new Unary()
                {
                    Operator = op,
                    Expr = expr,
                    SourceStart = Previous
                };
            }

            return Call();
        }

        ExpressionNode ArgumentList(ExpressionNode callee)
        {
            var callStart = Previous;
            var argumentList = new List<ExpressionNode>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    argumentList.Add(Expression());
                } while (Match(TokenType.Comma));
            }

            var callEnd = Consume(TokenType.RightParen, "Expected ')' after function arguments.");

            return new Call()
            {
                Callee = callee,
                ArgumentList = argumentList,
                SourceStart = callStart,
                RightParen = callEnd
            };
        }

        ExpressionNode Call()
        {
            var expr = Primary();

            while (true) // Use to match calls one after another
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = ArgumentList(expr); //Embed the callee expression in the Call Expression
                }
                else if (Match(TokenType.Dot))
                {
                    var dot = Previous;
                    var fieldName = Consume(TokenType.Identifier, "Expected identifier after '.'.");
                    expr = new Get()
                    {
                        CallSite = expr,
                        FieldName = fieldName,
                        SourceStart = dot
                    };
                }
                else if (Match(TokenType.LeftBracket))
                {
                    var bracket = Previous;
                    var indexExpression = Expression();
                    expr = new ArrayIndex()
                    {
                        CallSite = expr,
                        IndexExpression = indexExpression,
                        SourceStart = bracket
                    };
                    Consume(TokenType.RightBracket, "Expected ']' after index expression.");
                }
                else
                {
                    break;
                }
            }

            return expr;
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

            if (Match(TokenType.Equal, TokenType.BackwardArrow))
            {
                var assignmentToken = Previous;

                // Dereference and then assign vs just assign
                var pointerAssignment = assignmentToken.Type == TokenType.BackwardArrow;

                var rvalue = Assignment(); // right associative

                if (expr is Variable v)
                {
                    return new Assign()
                    {
                        Name = v.Name,
                        Assignment = rvalue,
                        SourceStart = assignmentToken,
                        PointerAssign = pointerAssignment
                    };
                }
                else if (expr is TupleExpression tuple)
                {
                    if (pointerAssignment)
                    {
                        RaiseError(assignmentToken, "Pointer assignment is not supported on tuple destructuring.");
                    }

                    return new TupleDestructure()
                    {
                        AssignmentTarget = tuple,
                        Value = rvalue,
                        SourceStart = assignmentToken
                    };
                }
                else if (expr is Get g)
                {
                    return new SetExpression()
                    {
                        FieldName = g.FieldName,
                        CallSite = g.CallSite,
                        Value = rvalue,
                        SourceStart = assignmentToken,
                        PointerAssign = pointerAssignment
                    };
                }
                else if (expr is ArrayIndex a)
                {
                    return new ArraySetExpression()
                    {
                        ArrayIndex = a,
                        Value = rvalue,
                        SourceStart = assignmentToken,
                        PointerAssign = pointerAssignment
                    };
                }
                else
                {
                    RaiseError(assignmentToken, $"Cannot assign to {expr.SourceStart.Lexeme}.");
                }
            }

            return expr;
        }

        ExpressionNode Expression()
        {
            // Assignment goes here eventually.
            return Assignment();
        }

        #region LValue

        public ILValue LValue()
        {
            // Tuple
            if (Match(TokenType.LeftParen))
            {
                var lParen = Previous;
                var elements = new List<ExpressionNode>();
                do
                {
                    // LValues must be pure identifiers
                    var name = Consume(TokenType.Identifier, "Expected identifier.");
                    elements.Add(new Variable()
                    {
                        Name = new QualifiedName(name),
                        SourceStart = name
                    });
                } while (Match(TokenType.Comma));

                if (elements.Count < 2)
                {
                    RaiseError(lParen, "Tuple type requires at lease two elements.");
                }

                Consume(TokenType.RightParen, "Expected ')' after tuple.");

                return new TupleExpression()
                {
                    Elements = elements
                };
            }
            else
            {
                // LValues must be pure identifiers, not qualified names
                var name = Consume(TokenType.Identifier, "Expected name after 'let'");

                return new Variable()
                {
                    Name = new QualifiedName(name),
                    SourceStart = name
                };
            }
        }

        #endregion

        #region Declarations

        Declaration Declaration()
        {
            var isExported = Match(TokenType.Export);

            Declaration declaration = null;
            if (Match(TokenType.Fn))
            {
                declaration = FunctionDeclaration();
            }
            else if (Match(TokenType.Type))
            {
                declaration = AliasTypeDeclaration();
            }
            else if (Match(TokenType.Struct))
            {
                declaration = StructDeclaration();
            }
            else if (Match(TokenType.Let))
            {
                declaration = ModuleVariableDeclaration();
            }
            else if (Match(TokenType.Import))
            {
                declaration = ImportDeclaration();
            }
            else if (Match(TokenType.Submodule))
            {
                declaration = SubmoduleDeclaration();
            }
            else
            {
                RaiseError(Peek, $"Unexpected token found '{Peek.Lexeme}'.");
            }

            declaration.IsExported = isExported;
            return declaration;
        }

        SubmoduleDeclaration SubmoduleDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected identifier after 'submodule'.");

            if (name.Identifier == "root")
            {
                RaiseError(name, "Submodule can not have name 'root'.");
            }

            Consume(TokenType.Semicolon, "Expected ';' after 'submodule'.");
            return new SubmoduleDeclaration()
            {
                Name = name
            };
        }

        Declaration ImportDeclaration()
        {
            Declaration importDeclaration;
            var import = MatchQualifiedName();
            if (import != null)
            {
                importDeclaration = new ImportDeclaration() { Name = import };
            }
            else
            {
                import = ConsumeMultiImport(out List<Token> names);

                importDeclaration = new MultiImportDeclaration()
                {
                    BaseName = import,
                    Names = names
                };
            }

            Consume(TokenType.Semicolon, "Expected ';' after 'import'.");

            return importDeclaration;
        }

        ModuleVariableDeclaration ModuleVariableDeclaration()
        {
            var letStatement = LetStatement();
            return new ModuleVariableDeclaration()
            {
                Let = letStatement
            };
        }

        TypeAliasDeclaration AliasTypeDeclaration()
        {
            var typeStart = Previous;
            var alias = Consume(TokenType.Identifier, "Expected identifier after 'type'.");

            Consume(TokenType.Colon, "Expected ':' after type alias.");

            var aliased = TypeName();

            Consume(TokenType.Semicolon, "Expected ';' after type name.");

            return new TypeAliasDeclaration()
            {
                AliasName = alias,
                AliasedTypeName = aliased,
                SourceStart = typeStart
            };
        }

        StructDeclaration StructDeclaration()
        {
            var structStart = Previous;
            var structName = Consume(TokenType.Identifier, "Expected identifier after 'struct'.");

            Consume(TokenType.LeftBrace, "Expected '{' after 'struct'.");

            var fields = new List<(Token, TypeName)>();
            if (!Check(TokenType.RightBrace))
            {
                do
                {
                    var name = Consume(TokenType.Identifier, "Expected identifier in struct definition.");
                    Consume(TokenType.Colon, "Expected ':' after struct field name.");
                    var typeName = TypeName();
                    fields.Add((name, typeName));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightBrace, "Expected '}' after struct fields.");

            return new StructDeclaration()
            {
                StructName = structName,
                Fields = fields,
                SourceStart = structStart
            };
        }

        FunctionDeclaration FunctionDeclaration()
        {
            var fnStart = Previous;
            var functionName = Consume(TokenType.Identifier, "Expected identifier after 'fn'.");

            Consume(TokenType.LeftParen, "Expected '(' after 'fn'.");

            var functionArguments = new List<FunctionArgumentDeclaration>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var isMutable = Match(TokenType.Mut);

                    var argumentName = Consume(TokenType.Identifier, "Expected argument name.");
                    Consume(TokenType.Colon, "Expected ':' after argument name");
                    var argumentType = TypeName();
                    functionArguments.Add(new FunctionArgumentDeclaration()
                    {
                        Name = argumentName,
                        TypeName = argumentType,
                        IsMutable = isMutable
                    });
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after function parameters.");

            TypeName returnType = StandardTypeNames.Void;
            if (Match(TokenType.Arrow))
            {
                returnType = TypeName();
            }

            Consume(TokenType.LeftBrace, "Expected '{' before function body.");
            var functionBody = ListOfStatements();

            return new FunctionDeclaration()
            {
                FunctionName = functionName,
                Statements = functionBody,
                Arguments = functionArguments,
                ReturnTypeName = returnType,
                SourceStart = fnStart
            };
        }

        TypeName TypeName()
        {
            TypeName baseType;

            var arrayModifier = Match(TokenType.Var);

            if (Match(TokenType.LeftBracket))
            {
                var innerTypeName = TypeName();
                Consume(TokenType.RightBracket, "Expected ']' in type name.");

                baseType = new ArrayTypeName()
                {
                    BaseTypeName = innerTypeName,
                    IsVariable = arrayModifier
                };
            }
            else if (Match(TokenType.LeftParen))
            {
                var parenOpen = Previous;
                var tupleTypes = new List<TypeName>();
                do
                {
                    var innerTypeName = TypeName();
                    tupleTypes.Add(innerTypeName);
                } while (Match(TokenType.Comma));

                if (tupleTypes.Count < 2)
                {
                    RaiseError(parenOpen, "Tuple type must have at least 2 elements.");
                }

                baseType = new TupleTypeName()
                {
                    ElementTypeNames = tupleTypes
                };
            }
            else
            {
                baseType = new BaseTypeName()
                {
                    Name = ConsumeQualifiedName(),
                    VarModifier = arrayModifier
                };
            }

            while (Match(TokenType.VarPtr, TokenType.Ptr))
            {
                var isVariable = Previous.Type == TokenType.VarPtr;
                baseType = new PointerTypeName()
                {
                    IsVariable = isVariable,
                    BaseTypeName = baseType
                };
            }

            return baseType;
        }

        #endregion

        #region Statements

        StatementNode ParseStatement()
        {
            if (Match(TokenType.Let))
            {
                return LetStatement();
            }
            else if (Match(TokenType.DebugPrint))
            {
                return PrintStatement();
            }
            else if (Match(TokenType.LeftBrace))
            {
                return BlockStatement();
            }
            else if (Match(TokenType.If))
            {
                return IfStatement();
            }
            else if (Match(TokenType.While))
            {
                return WhileStatement();
            }
            else if (Match(TokenType.Do))
            {
                return DoWhileStatement();
            }
            else if (Match(TokenType.For))
            {
                return ForStatement();
            }
            else if (Match(TokenType.Break, TokenType.Continue, TokenType.Return))
            {
                return ControlStatement();
            }
            else if (Match(TokenType.Free))
            {
                return FreeStatement();
            }
            else if (Match(TokenType.ForEach))
            {
                return ForEachStatement();
            }

            return ExpressionStatement();
        }

        ForEachStatement ForEachStatement()
        {
            var forEachToken = Previous;

            var optionalParens = Match(TokenType.LeftParen);

            Consume(TokenType.Let, "Expected 'let' after 'foreach'.");

            // TODO: Extract to share with let statement?
            bool isMutable = false;
            if (Match(TokenType.Mut))
            {
                isMutable = true;
            }

            var assignmentTarget = LValue();

            TypeName assertedType = null;
            if (Match(TokenType.Colon))
            {
                assertedType = TypeName();
            }

            Consume(TokenType.In, "Expected 'in' in foreach statement.");

            var iteratedValue = Expression();

            if (optionalParens)
            {
                Consume(TokenType.RightParen, "Mismatched parens.");
                Warning("Unnecessary parenthesis.", Previous);
            }

            Consume(TokenType.LeftBrace, "Expected '{' after declaration in foreach loop.");

            var body = BlockStatement();

            return new ForEachStatement()
            {
                AssignmentTarget = assignmentTarget,
                AssertedTypeName = assertedType,
                IteratedValue = iteratedValue,
                Body = body,
                IsMutable = isMutable,
                SourceStart = forEachToken
            };
        }

        ExpressionStatement ExpressionStatement()
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

        LetStatement LetStatement()
        {
            TypeName assertedType = null;
            ExpressionNode initializer = null;
            bool isMutable = false;
            var letToken = Previous;

            if (Match(TokenType.Mut))
            {
                isMutable = true;
            }

            var assignmentTarget = LValue();

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
                RaiseError(assignmentTarget.SourceStart, "Variable delaration must have either an explicit type or an assignment statement.");
            }

            Consume(TokenType.Semicolon, "Expected ';' after 'let' statement");

            return new LetStatement()
            {
                AssignmentTarget = assignmentTarget,
                SourceStart = letToken,
                Initializer = initializer,
                AssertedType = assertedType,
                IsMutable = isMutable
            };
        }

        PrintStatement PrintStatement()
        {
            var expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after print statement");

            return new PrintStatement()
            {
                Expr = expr
            };
        }

        BlockStatement BlockStatement()
        {
            var brace = Previous;
            var statements = ListOfStatements();

            return new BlockStatement()
            {
                Statements = statements,
                SourceStart = brace
            };
        }

        List<StatementNode> ListOfStatements()
        {
            var statements = new List<StatementNode>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }

            Consume(TokenType.RightBrace, "Expected '}' closing block.");

            return statements;
        }

        StatementNode IfStatement()
        {
            var ifStart = Previous;

            var predicate = Expression();

            if (Match(TokenType.LeftBrace))
            {
                var thenStatements = BlockStatement();
                BlockStatement elseStatements = null;
                if (Match(TokenType.Else))
                {
                    Consume(TokenType.LeftBrace, "Expected '{' after 'else'");
                    elseStatements = BlockStatement();
                }

                return new IfStatement()
                {
                    Predicate = predicate,
                    ThenStatements = thenStatements,
                    ElseStatements = elseStatements,
                    SourceStart = ifStart
                };
            }
            else
            {
                Consume(TokenType.Then, "Expected '{' or 'then' after 'if'.");
                var ifExpression = FinishIfExpression(ifStart, predicate);
                Consume(TokenType.Semicolon, "Expected ';' after expression.");
                return new ExpressionStatement()
                {
                    Expr = ifExpression,
                    SourceStart = ifStart
                };
            }
        }

        StatementNode WhileStatement()
        {
            var whileStart = Previous;
            var predicate = Expression();

            Consume(TokenType.LeftBrace, "Expected '{' after 'while'.");

            var body = BlockStatement();

            return new WhileStatement()
            {
                Predicate = predicate,
                Statements = body,
                SourceStart = whileStart
            };
        }

        StatementNode DoWhileStatement()
        {
            var doStart = Previous;
            var body = BlockStatement();
            Consume(TokenType.While, "Expected 'while' after do block.");

            var predicate = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after 'while'");

            return new WhileStatement()
            {
                Predicate = predicate,
                Statements = body,
                IsDoWhile = true,
                SourceStart = doStart
            };
        }

        StatementNode ForStatement()
        {
            var forStart = Previous;

            var optionalParens = Match(TokenType.LeftParen);

            var initializer = ForLoopInitializer();

            var predicate = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after predicate expression in for loop.");

            var update = Expression();

            if (optionalParens)
            {
                Consume(TokenType.RightParen, "Unmatched parens.");
                Warning("Unnecessary parens in 'if' statement.", Previous);
            }

            Consume(TokenType.LeftBrace, "Expected '{' after update expression in for loop.");

            var body = BlockStatement();

            return new ForStatement()
            {
                Initializer = initializer,
                Predicate = predicate,
                Update = update,
                Statements = body,
                SourceStart = forStart
            };
        }

        StatementNode ForLoopInitializer()
        {
            if (Match(TokenType.Let))
            {
                return LetStatement();
            }
            else
            {
                return ExpressionStatement();
            }
        }

        StatementNode FreeStatement()
        {
            var start = Previous;
            var expr = Expression();

            Consume(TokenType.Semicolon, "Expected ';' after 'free'.");

            return new FreeStatement()
            {
                Expr = expr,
                SourceStart = start
            };
        }

        StatementNode ControlStatement()
        {
            var start = Previous;
            if (start.Type == TokenType.Break)
            {

                Consume(TokenType.Semicolon, "Expected ';' after 'break'.");
                return new BreakStatement()
                {
                    SourceStart = start
                };
            }
            else if (start.Type == TokenType.Continue)
            {
                Consume(TokenType.Semicolon, "Expected ';' after 'continue'.");
                return new ContinueStatement()
                {
                    SourceStart = start
                };
            }
            else if (start.Type == TokenType.Return)
            {
                ExpressionNode returnValue = null;
                if (!Check(TokenType.Semicolon))
                {
                    returnValue = Expression();
                }
                Consume(TokenType.Semicolon, "Expected ';' after 'return'.");

                return new ReturnStatement()
                {
                    ReturnValue = returnValue,
                    SourceStart = start
                };
            }
            else
            {
                throw new NotImplementedException();
            }
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
                var dataType = value >= 0 ? IntType.InstanceUnsigned : IntType.InstanceSigned;
                return new Literal()
                {
                    Value = value,
                    DataType = dataType,
                    SourceStart = token
                };
            }
        }

        #endregion
    }
}