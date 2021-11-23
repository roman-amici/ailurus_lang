using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;
using AilurusLang.Scanning;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class Resolver
    {
        public static readonly ReservedWords ReservedWords = new ReservedWords();

        public static StandardScope StandardScope { get; set; } = new StandardScope();

        public List<BlockScope> Scopes { get; set; } = new List<BlockScope>();
        public ModuleScope ModuleScope { get; set; } = new ModuleScope();


        public bool WasInLoopBody { get; set; }
        public bool IsInLoopBody { get; set; }
        public bool IsInFunctionDefinition { get; set; }

        public bool HadError { get; set; }

        public void Reset()
        {
            HadError = false;
            Scopes = new List<BlockScope>();
            ModuleScope = new ModuleScope();
            IsInLoopBody = false;
            WasInLoopBody = false;
            IsInFunctionDefinition = false;
        }

        void Error(string message, Token token)
        {
            HadError = true;
            var error = new TypeError(message, token);
            Console.WriteLine(error);
        }

        public void ResolveStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                ResolveStatement(statement);
            }
        }

        #region Control Flow Analysis
        void EnterLoopBody()
        {
            WasInLoopBody = IsInLoopBody;
            IsInLoopBody = true;
        }

        void ExitLoopBody()
        {
            IsInLoopBody = WasInLoopBody;
        }

        void EnterFunctionDefinition()
        {
            IsInFunctionDefinition = true;
        }

        void ExitFunctionDefinition()
        {
            IsInFunctionDefinition = false;
        }

        #endregion

        #region Scope Management
        void BeginScope()
        {
            Scopes.Add(new BlockScope());
        }

        void EndScope()
        {
            Scopes.RemoveAt(Scopes.Count - 1);
        }

        bool IsValidVariableName(string name)
        {
            return !ReservedWords.Contains(name);
        }

        bool CanDeclareName(string name)
        {
            if (Scopes.Count == 0)
            {
                if (ModuleScope.VariableResolutions.ContainsKey(name) ||
                    ModuleScope.FunctionResolutions.ContainsKey(name))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                var scope = Scopes[^1];
                if (scope.VariableResolutions.ContainsKey(name))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public Resolution FindVariableDefinition(string name)
        {
            foreach (var scope in Scopes)
            {
                if (scope.VariableResolutions.ContainsKey(name))
                {
                    return scope.VariableResolutions[name];
                }
            }

            if (ModuleScope.VariableResolutions.ContainsKey(name))
            {
                return ModuleScope.VariableResolutions[name];
            }

            if (ModuleScope.FunctionResolutions.ContainsKey(name))
            {
                return ModuleScope.FunctionResolutions[name];
            }

            return null;
        }

        AilurusDataType LookupTypeByName(string name)
        {
            if (ModuleScope.TypeDeclarations.ContainsKey(name))
            {
                return ModuleScope.TypeDeclarations[name].DataType;
            }
            else
            {
                if (StandardScope.TypeDeclarations.ContainsKey(name))
                {
                    return StandardScope.TypeDeclarations[name].DataType;
                }
            }

            return null;
        }

        VariableResolution AddVariableToCurrentScope(
            Token name,
            AilurusDataType type,
            bool isMutable,
            bool initialized)
        {
            var declaration = new VariableResolution()
            {
                Name = name.Lexeme,
                DataType = type,
                IsMutable = isMutable,
                IsInitialized = initialized
            };

            if (Scopes.Count > 0)
            {
                declaration.ScopeDepth = Scopes.Count - 1;
                Scopes[^1].VariableResolutions.Add(declaration.Name, declaration);
            }
            else
            {
                ModuleScope.VariableResolutions.Add(declaration.Name, declaration);
            }

            return declaration;
        }

        #endregion

        #region Type Resolution Helpers

        AilurusDataType ResolveTypeName(TypeName typeName)
        {
            var type = LookupTypeByName(typeName.Name.Lexeme);
            if (type == null)
            {
                return ErrorType.Instance;
            }

            if (typeName.IsPtr)
            {
                return new PointerType()
                {
                    BaseType = type
                };
            }
            else
            {
                return type;
            }
        }

        (int, AilurusDataType) GetBaseType(AilurusDataType t)
        {
            var ptrCount = 0;
            AilurusDataType baseType = t;
            while (baseType is PointerType || baseType is AliasType)
            {
                if (baseType is PointerType p)
                {
                    ptrCount++;
                    baseType = p.BaseType;
                }
                else if (baseType is AliasType a)
                {
                    baseType = a.BaseType;
                }
            }

            return (ptrCount, baseType);
        }

        bool TypesAreEqual(AilurusDataType t1, AilurusDataType t2)
        {
            var (t1PtrCount, t1BaseType) = GetBaseType(t1);
            var (t2PtrCount, t2BaseType) = GetBaseType(t2);

            if (t1PtrCount != t2PtrCount)
            {
                return false;
            }

            if (t1BaseType is FunctionType f1 && t2BaseType is FunctionType f2)
            {
                if (!TypesAreEqual(f1.ReturnType, f1.ReturnType))
                {
                    return false;
                }

                if (f1.ArgumentTypes.Count != f2.ArgumentTypes.Count)
                {
                    return false;
                }

                for (var i = 0; i < f1.ArgumentTypes.Count; i++)
                {
                    if (!TypesAreEqual(f1.ArgumentTypes[i], f2.ArgumentTypes[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return t1BaseType.GetType() == t2BaseType.GetType();
        }

        bool CanAssignTo(AilurusDataType assertedType, AilurusDataType initializerType)
        {
            if (assertedType is ErrorType || initializerType is ErrorType)
            {
                return true;
            }

            // Check for facile equality first as an optimization
            if (assertedType == initializerType)
            {
                return true;
            }

            var (t1PtrCount, t1BaseType) = GetBaseType(assertedType);
            var (t2PtrCount, t2BaseType) = GetBaseType(initializerType);

            if (t1PtrCount == 0 && t2PtrCount == 0)
            {
                if (AilurusDataType.IsNumeric(t1BaseType) &&
                    AilurusDataType.IsNumeric(t2BaseType))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return TypesAreEqual(assertedType, initializerType);
            }

        }

        AilurusDataType GetNumericOperatorCoercion(AilurusDataType t1, AilurusDataType t2)
        {
            if (!(t1 is NumericType) || !(t2 is NumericType))
            {
                return ErrorType.Instance;
            }

            if (t1 is DoubleType ||
                t2 is DoubleType)
            {
                return DoubleType.Instance;
            }

            if (t1 is FloatType ||
                t2 is FloatType)
            {
                return FloatType.Instance;
            }

            // Default to 'signed' if either type is signed
            var unsigned = true;
            if (t1 is IntegralType i1)
            {
                unsigned = unsigned && i1.Unsigned;
            }
            if (t2 is IntegralType i2)
            {
                unsigned = unsigned && i2.Unsigned;
            }

            if (t1 is IntType ||
                t2 is IntType)
            {
                return unsigned ? IntType.InstanceUnsigned : IntType.InstanceSigned;
            }

            if (t1 is ShortType ||
                t2 is ShortType)
            {
                return unsigned ? ShortType.InstanceUnsigned : ShortType.InstanceSigned;
            }

            if (t1 is ByteType ||
                t2 is ByteType)
            {
                return unsigned ? ByteType.InstanceUnsigned : ByteType.InstanceSigned;
            }

            // Unreachable
            return ErrorType.Instance;
        }

        #endregion

        #region Resolve Expression
        AilurusDataType ResolveExpression(ExpressionNode expr)
        {
            switch (expr.ExprType)
            {
                case ExpressionType.Literal:
                    return ResolveLiteral((Literal)expr);
                case ExpressionType.Binary:
                case ExpressionType.BinaryShortCircut:
                    return ResolveBinary((BinaryLike)expr);
                case ExpressionType.Unary:
                    return ResolveUnary((Unary)expr);
                case ExpressionType.Grouping:
                    var grouping = (Grouping)expr;
                    return ResolveExpression(grouping.Inner);
                case ExpressionType.IfExpression:
                    return ResolveIfExpression((IfExpression)expr);
                case ExpressionType.Variable:
                    return ResolveVariable((Variable)expr);
                case ExpressionType.Assign:
                    return ResolveAssign((Assign)expr);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusDataType ResolveAssign(Assign expr)
        {
            var assignment = ResolveExpression(expr.Assignment);
            var declaration = FindVariableDefinition(expr.Name.Lexeme);
            if (declaration == null)
            {
                Error($"No variable was found with name {expr.Name.Lexeme}", expr.Name);
            }
            else if (declaration is VariableResolution v)
            {
                expr.Resolution = v;
                expr.Resolution.IsInitialized = true;
            }
            else
            {
                Error($"Cannot assign to {expr.Name.Lexeme}", expr.Name);
            }

            return assignment;
        }

        AilurusDataType ResolveVariable(Variable expr)
        {
            var resolution = FindVariableDefinition(expr.Name.Lexeme);

            if (resolution == null)
            {
                Error($"No variable was found with name {expr.Name.Lexeme}", expr.SourceStart);
                return ErrorType.Instance;
            }

            expr.Resolution = resolution;
            if (resolution is VariableResolution v)
            {
                if (!v.IsInitialized)
                {
                    Error($"Variable '{expr.Name.Lexeme}' referenced before assignment.", expr.Name);
                }
                return v.DataType;
            }
            else if (resolution is FunctionResolution f)
            {
                return f.Declaration.DataType;
            }

            // Unreachable
            return ErrorType.Instance;
        }

        AilurusDataType ResolveLiteral(Literal literal)
        {

            // Will already be resolve in the scanning phase once we get there...
            if (literal.DataType == null)
            {
                literal.DataType = literal.Value switch
                {
                    string _ => StringType.Instance,
                    bool _ => BooleanType.Instance,
                    int _ => IntType.InstanceSigned,
                    double _ => DoubleType.Instance,
                    _ => throw new NotImplementedException(),
                };
            }

            return literal.DataType;
        }

        void BinaryError(BinaryLike b, AilurusDataType t1, AilurusDataType t2)
        {
            var errorMessage = $"Found values of type {t1.DataTypeName} and {t2.DataTypeName}"
            + $" which are incompatible with operator {b.Operator.Lexeme}";
            Error(errorMessage, b.Operator);
        }

        AilurusDataType ResolveBinary(BinaryLike binary)
        {
            var t1 = ResolveExpression(binary.Left);
            var t2 = ResolveExpression(binary.Right);

            switch (binary.Operator.Type)
            {
                case TokenType.AmpAmp:
                case TokenType.BarBar:
                    if (t1 is BooleanType && t2 is BooleanType)
                    {
                        binary.DataType = t1;
                    }
                    else
                    {
                        binary.DataType = ErrorType.Instance;
                        BinaryError(binary, t1, t2);
                    }
                    break;
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Star:
                case TokenType.Slash:
                case TokenType.Mod:
                    binary.DataType = GetNumericOperatorCoercion(t1, t2);
                    if (binary.DataType is ErrorType)
                    {
                        BinaryError(binary, t1, t2);
                    }
                    break;
                case TokenType.EqualEqual:
                case TokenType.BangEqual:
                    var numericType = GetNumericOperatorCoercion(t1, t2);
                    if (numericType is ErrorType)
                    {
                        if (!TypesAreEqual(t1, t2))
                        {
                            BinaryError(binary, t1, t2);
                            binary.DataType = ErrorType.Instance;
                        }
                        else
                        {
                            binary.DataType = BooleanType.Instance;
                        }
                    }
                    else
                    {
                        binary.DataType = BooleanType.Instance;
                    }
                    break;
                case TokenType.Amp:
                case TokenType.Bar:
                case TokenType.Carrot:
                    if (t1 is IntegralType && t2 is IntegralType)
                    {
                        binary.DataType = GetNumericOperatorCoercion(t1, t2);
                    }
                    else
                    {
                        binary.DataType = ErrorType.Instance;
                        BinaryError(binary, t1, t2);
                    }
                    break;
                case TokenType.Less:
                case TokenType.LessEqual:
                case TokenType.Greater:
                case TokenType.GreaterEqual:
                    if (t1 is NumericType && t2 is NumericType)
                    {
                        binary.DataType = BooleanType.Instance;
                    }
                    else
                    {
                        binary.DataType = ErrorType.Instance;
                        BinaryError(binary, t1, t2);
                    }
                    break;
                default:
                    throw new NotImplementedException();

            }

            return binary.DataType;
        }

        void UnaryError(Unary unary, AilurusDataType inner)
        {
            Error($"Found type {inner.DataTypeName} which is incompatible with operator {unary.Operator.Lexeme}", unary.Operator);
        }

        AilurusDataType ResolveUnary(Unary unary)
        {
            var inner = ResolveExpression(unary.Expr);
            switch (unary.Operator.Type)
            {
                case TokenType.Minus:
                    if (inner is NumericType)
                    {
                        if (inner is IntegralType i)
                        {
                            unary.DataType = i.SignedInstance;
                        }
                        else
                        {
                            unary.DataType = inner;
                        }
                    }
                    else
                    {
                        UnaryError(unary, inner);
                    }
                    break;
                case TokenType.Bang:
                    if (inner is BooleanType)
                    {
                        unary.DataType = BooleanType.Instance;
                    }
                    else
                    {
                        UnaryError(unary, inner);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return unary.DataType;
        }

        AilurusDataType ResolveIfExpression(IfExpression ifExpr)
        {
            var pred = ResolveExpression(ifExpr.Predicate);
            var t = ResolveExpression(ifExpr.TrueExpr);
            var f = ResolveExpression(ifExpr.FalseExpr);

            if (!(pred is BooleanType))
            {
                Error($"Expected type bool in if expression but found type {pred.DataTypeName}", ifExpr.Predicate.SourceStart);
            }

            if (TypesAreEqual(t, f))
            {
                ifExpr.DataType = t;
            }
            else
            {
                Error($"Both sides of an if expression must be the same type, but found {t} and {f}", ifExpr.TrueExpr.SourceStart);
                ifExpr.DataType = ErrorType.Instance;
            }

            return ifExpr.DataType;
        }

        #endregion

        #region Resolve Statements
        void ResolveLet(LetStatement let)
        {
            if (!IsValidVariableName(let.Name.Lexeme))
            {
                Error($"Variable cannot have name '{let.Name.Lexeme}' since it is reserved.", let.Name);
                return;
            }

            if (!CanDeclareName(let.Name.Lexeme))
            {
                Error($"Variable with name '{let.Name.Lexeme}' already exists in this scope.", let.SourceStart);
                return;
            }

            AilurusDataType assertedType = null;
            AilurusDataType initializerType = null;
            if (let.AssertedType != null)
            {
                assertedType = ResolveTypeName(let.AssertedType);
                if (assertedType is ErrorType)
                {
                    Error($"Asserted type '{let.AssertedType.Name.Lexeme}' could not be found.", let.AssertedType.Name);
                }
            }

            var initialized = let.Initializer != null;
            if (initialized)
            {
                initializerType = ResolveExpression(let.Initializer);
            }

            if (assertedType == null)
            {
                // Type inference
                assertedType = initializerType;
            }

            if (assertedType == null)
            {
                Error($"Could not infer type for variable '{let.Name.Lexeme}'", let.Name);
                return;
            }

            if (!CanAssignTo(assertedType, initializerType))
            {
                Error($"Can't assing type {initializerType.DataTypeName} to type {assertedType.DataTypeName}", let.SourceStart);
                return;
            }

            var declaration = AddVariableToCurrentScope(
                let.Name,
                assertedType,
                let.IsMutable,
                initialized);

            let.Resolution = declaration;
        }

        void ResolvePrint(PrintStatement print)
        {
            ResolveExpression(print.Expr);
        }

        void ResolveBlock(BlockStatement block)
        {
            BeginScope();
            ResolveStatements(block.Statements);
            EndScope();
        }

        void ResolveIfStatement(IfStatement ifStatement)
        {
            var predicate = ResolveExpression(ifStatement.Predicate);

            if (!(predicate is BooleanType))
            {
                Error($"Expected if predicate to be of type {BooleanType.Instance.DataTypeName} but instead found type {predicate.DataTypeName}", ifStatement.SourceStart);
            }

            ResolveBlock(ifStatement.ThenStatements);
            if (ifStatement.ElseStatements != null)
            {
                ResolveBlock(ifStatement.ElseStatements);
            }
        }

        void ResolveWhileStatement(WhileStatement whileStatement)
        {
            var predicate = ResolveExpression(whileStatement.Predicate);

            if (!(predicate is BooleanType))
            {
                Error("$Expected 'while' predicate to be of type {BooleanType.Instance.DataTypeName} but instead found type {predicate.DataTypeName}", whileStatement.SourceStart);
            }

            EnterLoopBody();
            ResolveBlock(whileStatement.Statements);
            ExitLoopBody();
        }

        void ResolveForStatement(ForStatement forStatement)
        {
            ResolveStatement(forStatement.Initializer);
            var predicate = ResolveExpression(forStatement.Predicate);

            if (!(predicate is BooleanType))
            {
                Error($"Expected 'for' predicate to be of type {BooleanType.Instance.DataTypeName} but instead frond type {predicate.DataTypeName}", forStatement.Predicate.SourceStart);
            }

            ResolveExpression(forStatement.Update);

            EnterLoopBody();
            ResolveBlock(forStatement.Statements);
            ExitLoopBody();
        }

        void ResolveBreakOrContinueStatement(ControlStatement control)
        {
            if (!IsInLoopBody)
            {
                Error($"'{control.SourceStart.Lexeme}' found outside of a loop body", control.SourceStart);
            }
        }

        void ResolveReturnStatement(ReturnStatement returnStatement)
        {
            if (IsInFunctionDefinition)
            {
                Error($"'return' found outside of a function body.", returnStatement.SourceStart);
            }

            //TODO: handle type checking
            var returnType = ResolveExpression(returnStatement.ReturnValue);
        }

        void ResolveStatement(StatementNode statement)
        {
            switch (statement.StmtType)
            {
                case StatementType.Expression:
                    ResolveExpression(((ExpressionStatement)statement).Expr);
                    break;
                case StatementType.Let:
                    ResolveLet((LetStatement)statement);
                    break;
                case StatementType.Print:
                    ResolvePrint((PrintStatement)statement);
                    break;
                case StatementType.Block:
                    ResolveBlock((BlockStatement)statement);
                    break;
                case StatementType.If:
                    ResolveIfStatement((IfStatement)statement);
                    break;
                case StatementType.While:
                    ResolveWhileStatement((WhileStatement)statement);
                    break;
                case StatementType.For:
                    ResolveForStatement((ForStatement)statement);
                    break;
                case StatementType.Break:
                case StatementType.Continue:
                    ResolveBreakOrContinueStatement((ControlStatement)statement);
                    break;
                case StatementType.Return:
                    ResolveReturnStatement((ReturnStatement)statement);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
    }
}