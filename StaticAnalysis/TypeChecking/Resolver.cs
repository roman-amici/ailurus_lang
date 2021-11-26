using System;
using System.Collections.Generic;
using System.Linq;
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

        //TODO: Proper branch analysis and dead-code detection
        public bool WasInLoopBody { get; set; }
        public bool IsInLoopBody { get; set; }

        public bool IsInFunctionDefinition { get; set; }
        public AilurusDataType CurrentFunctionReturnType { get; set; }

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

        #region EntryPoints

        public void ResolveStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                ResolveStatement(statement);
            }
        }

        public void ResolveModule(Module module)
        {
            //TODO: Resolve imports

            foreach (var typeDeclaration in module.TypeDeclarations)
            {
                // In the first pass we gather type names 
                ResolveTypeDeclarationFirstPass(typeDeclaration);
            }
            foreach (var typeDeclaration in module.TypeDeclarations)
            {
                ResolveTypeDeclarationSecondPass(typeDeclaration);
            }

            foreach (var variableDeclaration in module.VariableDeclarations)
            {
                ResolveModuleVariableDeclarations(variableDeclaration);
            }

            foreach (var functionDeclaration in module.FunctionDeclarations)
            {
                ResolveFunctionDeclarationFirstPass(functionDeclaration);
            }
            foreach (var functionDeclaration in module.FunctionDeclarations)
            {
                ResolveFunctionDeclarationSecondPass(functionDeclaration);
            }

        }

        #endregion

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

        void EnterFunctionDefinition(AilurusDataType returnType)
        {
            BeginScope();
            IsInFunctionDefinition = true;
            CurrentFunctionReturnType = returnType;
        }

        void ExitFunctionDefinition()
        {
            EndScope();
            IsInFunctionDefinition = false;
            CurrentFunctionReturnType = null;
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
                var declaration = ModuleScope.TypeDeclarations[name];
                if (declaration.State == TypeDeclaration.ResolutionState.Resolving)
                {
                    Error($"Circular type declaration detected while resolving type {name}", declaration.SourceStart);
                    return ErrorType.Instance;
                }
                else if (declaration.State != TypeDeclaration.ResolutionState.Resolved)
                {
                    ResolveTypeDeclarationSecondPass(declaration);
                }
                return declaration.DataType;
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
            var resolution = new VariableResolution()
            {
                Name = name.Lexeme,
                DataType = type,
                IsMutable = isMutable,
                IsInitialized = initialized
            };

            if (Scopes.Count > 0)
            {
                resolution.ScopeDepth = Scopes.Count - 1;
                Scopes[^1].VariableResolutions.Add(resolution.Name, resolution);
            }
            else
            {
                ModuleScope.VariableResolutions.Add(resolution.Name, resolution);
            }

            return resolution;
        }

        FunctionResolution AddFunctionToModule(string functionName, FunctionDeclaration declaration)
        {
            var resolution = new FunctionResolution()
            {
                Declaration = declaration,
            };
            ModuleScope.FunctionResolutions.Add(functionName, resolution);

            return resolution;
        }

        #endregion

        #region Type Resolution Helpers

        AilurusDataType ResolvePlaceholder(PlaceholderType placeholder)
        {
            if (placeholder.ResolvedType == null)
            {
                placeholder.ResolvedType = ResolveTypeName(placeholder.TypeName);
            }

            return placeholder.ResolvedType;
        }

        AilurusDataType ResolveTypeName(Token name)
        {
            return ResolveTypeName(new TypeName()
            {
                Name = name,
                IsPtr = false
            });
        }

        AilurusDataType ResolveTypeName(TypeName typeName)
        {
            var type = LookupTypeByName(typeName.Name.Lexeme);
            if (type == null)
            {
                return ErrorType.Instance;
            }

            if (type is PlaceholderType placeholder)
            {
                type = placeholder.ResolvedType;
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

        bool IsValidFunctionArgumentType(AilurusDataType type)
        {

            //TODO: Others?
            if (type is VoidType)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        bool IsStaticExpression(ExpressionNode expr)
        {
            // TODO: Constant evaluation and constexpr graphs?
            return expr switch
            {
                BinaryLike b => IsStaticExpression(b.Left) && IsStaticExpression(b.Right),
                Literal _ => true,
                Unary u => IsStaticExpression(u.Expr),
                Grouping g => IsStaticExpression(g.Inner),
                IfExpression i => IsStaticExpression(i.Predicate)
                                    && IsStaticExpression(i.TrueExpr)
                                    && IsStaticExpression(i.FalseExpr),
                // All others are non-static
                _ => false,
            };
        }

        bool CanDeclareType(string type)
        {
            return !ModuleScope.TypeDeclarations.ContainsKey(type);
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
                case ExpressionType.Call:
                    return ResolveCall((Call)expr);
                case ExpressionType.Get:
                    return ResolveGet((Get)expr);
                case ExpressionType.StructInitialization:
                    return ResolveStructInitialization((StructInitialization)expr);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusDataType ResolveStructInitialization(StructInitialization expr)
        {
            var structName = expr.StructName.Identifier;
            var type = ResolveTypeName(expr.StructName);
            expr.DataType = type;

            if (type is StructType structType)
            {
                expr.DataType = structType;

                var fieldsIntialized = new HashSet<string>();
                foreach (var (fieldName, initializer) in expr.Initializers)
                {
                    var fieldIdentifier = fieldName.Identifier;
                    var initializerType = ResolveExpression(initializer);
                    if (structType.Definitions.ContainsKey(fieldIdentifier))
                    {
                        if (fieldsIntialized.Contains(fieldIdentifier))
                        {
                            Error($"Field '{fieldIdentifier}' has already been initialized for struct '{structName}'.", fieldName);
                        }
                        else
                        {
                            fieldsIntialized.Add(fieldIdentifier);
                            var fieldType = structType.Definitions[fieldIdentifier];
                            if (!TypesAreEqual(initializerType, fieldType))
                            {
                                Error($"Field '{fieldIdentifier}' expected type '{fieldType.DataTypeName}' but instead found type '{initializerType.DataTypeName}'", fieldName);
                            }
                        }
                    }
                }

                if (fieldsIntialized.Count != structType.Definitions.Count)
                {
                    Error($"Not all fields were initialized for struct '{structName}'.", expr.StructName);
                }
            }
            else
            {
                Error($"Expected struct type but found type {type.DataTypeName}.", expr.StructName);
            }

            return type;
        }

        AilurusDataType ResolveGet(Get expr)
        {
            var callSiteType = ResolveExpression(expr.CallSite);

            var (_, baseType) = GetBaseType(callSiteType);

            if (baseType is StructType structType)
            {
                var fieldName = expr.FieldName.Identifier;
                if (structType.Definitions.ContainsKey(fieldName))
                {
                    return structType.Definitions[fieldName];
                }
                else
                {
                    Error($"Field '{fieldName}' not found on struct of type '{structType.DataTypeName}'", expr.SourceStart);
                }
            }
            else
            {
                Error($"Properties must be of type struct, but found {baseType.DataTypeName}", expr.SourceStart);
            }

            return ErrorType.Instance;
        }

        AilurusDataType ResolveCall(Call call)
        {
            var callee = ResolveExpression(call.Callee);
            AilurusDataType returnType = ErrorType.Instance;

            List<AilurusDataType> argumentTypes;
            if (callee is FunctionType function)
            {
                argumentTypes = function.ArgumentTypes;
                if (argumentTypes.Count != call.ArgumentList.Count)
                {
                    Error($"Expected ${argumentTypes.Count} arguments for function but found ${call.ArgumentList.Count} arguments.", call.RightParen);
                }
                returnType = function.ReturnType;
            }
            else
            {
                Error($"Unable to call value of type {callee.DataTypeName}", call.Callee.SourceStart);
                argumentTypes = new List<AilurusDataType>();
            }

            for (var i = 0; i < call.ArgumentList.Count; i++)
            {
                var callArgType = ResolveExpression(call.ArgumentList[i]);
                if (i < argumentTypes.Count)
                {
                    var functionArgType = argumentTypes[i];
                    if (!CanAssignTo(functionArgType, callArgType))
                    {
                        Error($"Argument {i} with type {functionArgType.DataTypeName} does not match given expression of type {callArgType.DataTypeName}", call.ArgumentList[i].SourceStart);
                    }
                }
            }

            return returnType;
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
        void ResolveLet(LetStatement let, bool moduleVariable = false)
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

            if (moduleVariable)
            {
                if (initialized)
                {
                    if (!IsStaticExpression(let.Initializer))
                    {
                        Error($"Module variables must have a static initialization.", let.Initializer.SourceStart);
                    }
                }
                else
                {
                    Error($"Module variables must be initialized.", let.SourceStart);
                }
            }

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
            if (!IsInFunctionDefinition)
            {
                Error($"'return' found outside of a function body.", returnStatement.SourceStart);
            }

            //TODO: handle type checking
            var returnType = ResolveExpression(returnStatement.ReturnValue);
            if (!TypesAreEqual(returnType, CurrentFunctionReturnType))
            {
                Error($"Return statement has return type of {returnType.DataTypeName} which is incompatible with function return type of {CurrentFunctionReturnType.DataTypeName}", returnStatement.SourceStart);
            }
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

        #region Resolve Declarations

        bool ResolveAliasDeclarationFirstPass(TypeAliasDeclaration alias)
        {
            var typeName = alias.AliasName.Identifier;
            if (!CanDeclareType(typeName))
            {
                Error($"Type name {typeName} is already declared in this module.", alias.AliasName);
                return false;
            }

            var placeholder = new PlaceholderType()
            {
                TypeName = alias.AliasedTypeName
            };
            alias.DataType = new AliasType()
            {
                Alias = typeName,
                BaseType = placeholder,
            };

            ModuleScope.TypeDeclarations.Add(typeName, alias);

            return true;
        }

        bool ResolveStructDeclarationFirstPass(StructDeclaration s)
        {
            var structName = s.StructName.Identifier;
            if (!CanDeclareType(structName))
            {
                Error($"Type name {structName} is already declared in this module.", s.StructName);
                return false; // TODO: resolve fields anyway
            }
            //Add to a dictionary of placeholders since we don't resolve the types until the second pass
            // This allows, e.g. embedding one struct in another.
            var placeholderDictionary = new Dictionary<string, AilurusDataType>();
            foreach (var (fieldName, typeName) in s.Fields)
            {
                placeholderDictionary.Add(fieldName.Identifier, new PlaceholderType() { TypeName = typeName });
            }

            var structType = new StructType()
            {
                StructName = s.StructName.Identifier,
                Definitions = placeholderDictionary
            };
            s.DataType = structType;
            ModuleScope.TypeDeclarations.Add(structName, s);

            return true;
        }

        void ResolveTypeDeclarationFirstPass(TypeDeclaration declaration)
        {
            bool added;
            if (declaration is StructDeclaration s)
            {
                added = ResolveStructDeclarationFirstPass(s);
            }
            else if (declaration is TypeAliasDeclaration a)
            {
                added = ResolveAliasDeclarationFirstPass(a);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (added)
            {
                declaration.State = TypeDeclaration.ResolutionState.Added;
            }
        }

        void ResolveTypeDeclarationSecondPass(TypeDeclaration declaration)
        {
            if (declaration is StructDeclaration s)
            {
                ResolveStructDeclarationSecondPass(s);

            }
            else if (declaration is TypeAliasDeclaration a)
            {
                ResolveAliasDeclarationSecondPass(a);
            }
        }

        void ResolveAliasDeclarationSecondPass(TypeAliasDeclaration alias)
        {
            alias.State = TypeDeclaration.ResolutionState.Resolving;

            var aliasType = (AliasType)alias.DataType;
            if (aliasType.BaseType is PlaceholderType placeholder)
            {
                ResolvePlaceholder(placeholder);
                // Replace the placeholder with the resolved base type
                aliasType.BaseType = placeholder.ResolvedType;
            }

            if (aliasType.BaseType is ErrorType)
            {
                Error($"Unable to declare type alias {alias.AliasName.Identifier}. Could not resolve type name {alias.AliasedTypeName.Name.Identifier}", alias.AliasName);
            }

            alias.State = TypeDeclaration.ResolutionState.Resolved;
        }

        void ResolveStructDeclarationSecondPass(StructDeclaration s)
        {
            s.State = TypeDeclaration.ResolutionState.Resolving;
            var structType = (StructType)s.DataType;
            foreach (var kvp in structType.Definitions)
            {

                if (kvp.Value is PlaceholderType placeholder)
                {
                    ResolvePlaceholder(placeholder);
                }
            }

            // Replace the placeholder types with their resolved base types 
            // Iterate over list of string to avoid iterator invalidation
            var hadError = false;
            foreach (var fieldName in structType.Definitions.Keys.ToList())
            {
                var placeholder = (PlaceholderType)structType.Definitions[fieldName];

                structType.Definitions[fieldName] = placeholder.ResolvedType;
                if (placeholder.ResolvedType is ErrorType)
                {
                    hadError = true;
                }
            }

            if (hadError)
            {
                Error($"Unable to define struct {s.StructName.Identifier}. Unable to resolve one or more fields.", s.StructName);
            }

            s.State = TypeDeclaration.ResolutionState.Resolved;
        }

        void ResolveModuleVariableDeclarations(ModuleVariableDeclaration declaration)
        {
            ResolveLet(declaration.Let);
        }

        void ResolveFunctionDeclarationFirstPass(FunctionDeclaration declaration)
        {
            // Resolve the function type signature and name
            var functionName = declaration.FunctionName.Lexeme;

            var argumentTypes = new List<AilurusDataType>();
            foreach (var (_, typeName) in declaration.Arguments)
            {
                var type = ResolveTypeName(typeName);
                if (!IsValidFunctionArgumentType(type))
                {
                    Error($"Type {type.DataTypeName} is not a valid type for function arguments.", typeName.Name);
                }
                argumentTypes.Add(type);
            }

            var returnType = ResolveTypeName(declaration.ReturnTypeName);

            declaration.FunctionType = new FunctionType()
            {
                ArgumentTypes = argumentTypes,
                ReturnType = returnType
            };

            if (!CanDeclareName(functionName))
            {
                Error($"Identifier with name '{functionName}' already exists in this module.", declaration.SourceStart);
            }
            else
            {
                // Only add it if the name doesn't conflict
                var resolution = AddFunctionToModule(functionName, declaration);
                declaration.Resolution = resolution;
            }
        }

        //Todo: Sparate pass for control flow analysis
        bool ValidateReturn(List<StatementNode> statements)
        {
            if (statements.Count == 0)
            {
                return false;
            }

            var lastStatement = statements[^1];
            if (lastStatement is ReturnStatement)
            {
                return true;
            }

            var allBranchesReturn = false;
            if (lastStatement is IfStatement i && i.ElseStatements != null)
            {
                var thenReturns = ValidateReturn(i.ThenStatements.Statements);
                var elseReturns = ValidateReturn(i.ElseStatements.Statements);
                allBranchesReturn = thenReturns && elseReturns;
            }
            return allBranchesReturn;
        }

        void ResolveFunctionDeclarationSecondPass(FunctionDeclaration declaration)
        {
            EnterFunctionDefinition(declaration.FunctionType.ReturnType);
            declaration.ArgumentResolutions = new List<VariableResolution>();
            for (var i = 0; i < declaration.Arguments.Count; i++)
            {
                var (argumentName, _) = declaration.Arguments[i];
                var dataType = declaration.FunctionType.ArgumentTypes[i];

                // TODO: handle mutability
                var resolution = AddVariableToCurrentScope(argumentName, dataType, true, true);
                declaration.ArgumentResolutions.Add(resolution);
            }

            foreach (var statement in declaration.Statements)
            {
                ResolveStatement(statement);
            }
            ExitFunctionDefinition();

            if (!(declaration.FunctionType.ReturnType is VoidType))
            {
                if (!ValidateReturn(declaration.Statements))
                {
                    var token = declaration.Statements.Count == 0 ? declaration.SourceStart : declaration.Statements[^1].SourceStart;
                    Error("Not all branches return a value.", token);
                }
            }
        }

        #endregion
    }
}