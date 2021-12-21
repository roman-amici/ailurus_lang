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

        public bool IsInNewExpression { get; set; }
        public bool WasInNewExpression { get; set; }

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
            IsInNewExpression = false;
            WasInNewExpression = false;
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

        void EnterNewStatement()
        {
            IsInNewExpression = true;
        }

        void ExitNewStatement()
        {
            IsInNewExpression = false;
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
            return ResolveTypeName(new BaseTypeName()
            {
                Name = name,
            });
        }

        AilurusDataType ResolveTypeName(TypeName typeName)
        {
            if (typeName is PointerTypeName p)
            {
                var baseType = ResolveTypeName(p.BaseTypeName);
                if (baseType is ErrorType)
                {
                    return baseType;
                }
                else
                {
                    return new PointerType()
                    {
                        BaseType = baseType,
                        IsVariable = p.IsVariable
                    };
                }
            }
            else if (typeName is ArrayTypeName a)
            {
                var baseType = ResolveTypeName(a.BaseTypeName);
                if (baseType is ErrorType)
                {
                    return baseType;
                }
                else
                {
                    return new ArrayType()
                    {
                        BaseType = baseType
                    };
                }
            }
            else if (typeName is BaseTypeName b)
            {
                var type = LookupTypeByName(b.Name.Lexeme);
                if (type == null)
                {
                    return ErrorType.Instance;
                }

                if (type is PlaceholderType placeholder)
                {
                    type = placeholder.ResolvedType;
                }

                return type;
            }
            else
            {
                return ErrorType.Instance; //Unreachable
            }
        }

        AilurusDataType UnwrapPtrTypes(AilurusDataType t, out int ptrCount, out bool isVariable)
        {
            isVariable = false;
            ptrCount = 0;
            AilurusDataType baseType = t;
            while (baseType is PointerType || baseType is AliasType)
            {
                if (baseType is PointerType p)
                {
                    isVariable = isVariable || p.IsVariable;
                    ptrCount++;
                    baseType = p.BaseType;
                }
                else if (baseType is AliasType a)
                {
                    baseType = a.BaseType;
                }
            }

            return baseType;
        }

        bool TypesAreEqual(AilurusDataType t1, AilurusDataType t2)
        {
            var t1BaseType = UnwrapPtrTypes(t1, out int t1PtrCount, out bool t1Variable);
            var t2BaseType = UnwrapPtrTypes(t2, out int t2PtrCount, out bool t2Variable);

            if (t1PtrCount != t2PtrCount)
            {
                return false;
            }

            if (t1Variable != t2Variable)
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
            else if (t1BaseType is ArrayType a1 && t2BaseType is ArrayType a2)
            {
                return TypesAreEqual(a1.BaseType, a2.BaseType);
            }

            return t1BaseType.GetType() == t2BaseType.GetType();
        }

        bool CanAssignTo(AilurusDataType lhsType, AilurusDataType rhsType, bool pointerAssign, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (lhsType is ErrorType || rhsType is ErrorType)
            {
                return true;
            }

            var assertedType = lhsType;
            bool lhsIsVariable = false;
            // Do we dereference before we assign?
            if (pointerAssign)
            {
                assertedType = DereferenceType(assertedType, out lhsIsVariable);
                if (!lhsIsVariable)
                {
                    errorMessage = "Cannot mutate constant pointer.";
                    return false;
                }
            }

            // Check for facile equality first as an optimization
            if (assertedType == rhsType)
            {
                return true;
            }

            var lhsBaseType = UnwrapPtrTypes(assertedType, out int lhsPtrCount, out bool _);
            var rhsBaseType = UnwrapPtrTypes(rhsType, out int rhsPtrCount, out bool rhsIsVariable);

            var genericError = $"Cannot assign value of type {rhsType.DataTypeName} to value of type {assertedType.DataTypeName}.";
            if (lhsPtrCount == 0 && rhsPtrCount == 0)
            {
                if (AilurusDataType.IsNumeric(lhsBaseType) &&
                    AilurusDataType.IsNumeric(rhsBaseType))
                {
                    return true;
                }
            }
            else if (lhsPtrCount > 0)
            {
                if (rhsBaseType is NullType)
                {
                    return true;
                }
                if (lhsIsVariable && !rhsIsVariable)
                {
                    errorMessage = $"Cannot assign variable pointer to constant pointer without a cast.";
                    return false;
                }
                return lhsPtrCount == rhsPtrCount && TypesAreEqual(lhsBaseType, rhsBaseType);
            }

            // Fall through and compare the inner types
            var result = TypesAreEqual(assertedType, rhsType);
            if (!result)
            {
                errorMessage = genericError;
            };

            return result;

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

        AilurusDataType DereferenceType(AilurusDataType type, out bool isVariable)
        {
            isVariable = false;
            if (type is AliasType a)
            {
                return DereferenceType(a.BaseType, out isVariable);
            }
            else if (type is PointerType p)
            {
                isVariable = p.IsVariable;
                return p.BaseType;
            }
            else
            {
                return ErrorType.Instance;
            }
        }

        bool IsPointerType(AilurusDataType type)
        {
            if (type is AliasType a)
            {
                return IsPointerType(a.BaseType);
            }
            else
            {
                return type is PointerType;
            }
        }

        bool IsIntegerType(AilurusDataType type)
        {
            if (type is AliasType a)
            {
                return IsIntegerType(a.BaseType);
            }
            else
            {
                return type is IntegralType;
            }
        }

        bool IsIndexType(AilurusDataType type)
        {
            if (type is AliasType a)
            {
                return IsIndexType(a.BaseType);
            }
            else
            {
                if (type is IntegralType i)
                {
                    return i.Unsigned;
                }
                else
                {
                    return false;
                }
            }
        }

        bool IsArrayLikeType(AilurusDataType type)
        {
            if (type is AliasType a)
            {
                return IsArrayLikeType(a.BaseType);
            }
            else
            {
                if (type is ArrayType || type is StringType)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
                case ExpressionType.Set:
                    return ResolveSet((SetExpression)expr);
                case ExpressionType.StructInitialization:
                    return ResolveStructInitialization((StructInitialization)expr);
                case ExpressionType.AddrOfExpression:
                    return ResolveAddrOfExpression((AddrOfExpression)expr);
                case ExpressionType.ArrayLiteral:
                    return ResolveArrayLiteral((ArrayLiteral)expr);
                case ExpressionType.ArrayIndex:
                    return ResolveArrayIndex((ArrayIndex)expr);
                case ExpressionType.ArraySetExpression:
                    return ResolveArraySet((ArraySetExpression)expr);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusDataType ResolveArraySet(ArraySetExpression expr)
        {
            var valueType = ResolveExpression(expr.Value);
            var arrayValueType = ResolveExpression(expr.ArrayIndex);

            if (!CanAssignTo(arrayValueType, valueType, expr.PointerAssign, out string errorMessage))
            {
                Error(errorMessage, expr.SourceStart);
            }

            return arrayValueType;
        }

        AilurusDataType ResolveArrayIndex(ArrayIndex index)
        {
            var callsiteType = ResolveExpression(index.CallSite);

            AilurusDataType expressionType = ErrorType.Instance;
            if (callsiteType is StringType)
            {
                expressionType = CharType.Instance;
            }
            else if (callsiteType is ArrayType a)
            {
                expressionType = a.BaseType;
            }
            else
            {
                Error($"Expected type 'array' or type 'string' but found type '{callsiteType.DataTypeName}'.", index.CallSite.SourceStart);
            }

            var indexType = ResolveExpression(index.IndexExpression);

            if (!IsIndexType(indexType))
            {
                Error($"Expected array index to have integral type but instead found type '{indexType.DataTypeName}'.", index.SourceStart);
            }

            index.DataType = expressionType;
            return expressionType;
        }

        AilurusDataType ResolveArrayLiteral(ArrayLiteral array)
        {
            AilurusDataType baseType = null;
            if (array.Elements != null)
            {
                foreach (var element in array.Elements)
                {
                    var elementType = ResolveExpression(element);

                    baseType ??= elementType;
                    if (!TypesAreEqual(baseType, elementType))
                    {
                        Error("All elements of array literal must be of the same type.", element.SourceStart);
                        baseType = ErrorType.Instance;
                        break;
                    }
                }
            }

            if (array.FillExpression != null)
            {
                var fillType = ResolveExpression(array.FillExpression);
                baseType ??= fillType;
                if (!TypesAreEqual(baseType, fillType))
                {
                    Error("All elements of array literal must be of the same type.", array.FillExpression.SourceStart);
                    baseType = ErrorType.Instance;
                }
            }

            if (array.FillLength != null)
            {
                var fillLengthType = ResolveExpression(array.FillLength);
                if (!IsIndexType(fillLengthType))
                {
                    Error($"Array fill length must be a positive integer but was instead of type {fillLengthType.DataTypeName}", array.FillLength.SourceStart);
                }
                if (!IsInNewExpression && !IsStaticExpression(array.FillLength))
                {
                    Error($"Array literals outside of a 'new' expression must be have a static size", array.FillLength.SourceStart);
                }
            }

            var arrayType = new ArrayType()
            {
                BaseType = baseType
            };
            array.DataType = arrayType;
            return arrayType;
        }

        AilurusDataType ResolveAddrOfExpression(AddrOfExpression expr)
        {
            var exprType = ResolveExpression(expr.OperateOn);
            var ptrType = new PointerType()
            {
                BaseType = exprType,
                IsVariable = expr.VarAddr
            };
            expr.DataType = ptrType;
            return ptrType;
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
                            if (!CanAssignTo(initializerType, fieldType, false, out string errorMessage))
                            {
                                Error(errorMessage, fieldName);
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

        AilurusDataType ResolveFieldReference(IFieldAccessor expr, out StructType innerStruct)
        {
            innerStruct = null;
            var callSiteType = ResolveExpression(expr.CallSite);

            var baseType = UnwrapPtrTypes(callSiteType, out int _, out bool _);

            if (baseType is StructType structType)
            {
                innerStruct = structType;
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

        AilurusDataType ResolveSet(SetExpression expr)
        {
            var valueType = ResolveExpression(expr.Value);
            var fieldType = ResolveFieldReference(expr, out StructType structType);
            if (structType != null)
            {
                if (!CanAssignTo(fieldType, valueType, expr.PointerAssign, out string errorMessage))
                {
                    Error(errorMessage, expr.SourceStart);
                }
            }

            return fieldType;
        }

        AilurusDataType ResolveGet(Get expr)
        {
            return ResolveFieldReference(expr, out StructType _);
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
                    if (!CanAssignTo(functionArgType, callArgType, false, out _))
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
                if (!expr.PointerAssign && !v.IsMutable)
                {
                    Error($"Cannot assign to variable {v.Name} since it was not declared as mutable.", expr.SourceStart);
                }
                expr.Resolution = v;
                expr.Resolution.IsInitialized = true;

                var lhsType = v.DataType;
                if (!CanAssignTo(lhsType, assignment, expr.PointerAssign, out string errorMessage))
                {
                    Error(errorMessage, expr.SourceStart);
                }
            }
            else
            {
                Error($"Cannot assign to {expr.Name.Lexeme}", expr.Name);
            }

            expr.DataType = assignment;
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
                    char _ => CharType.Instance,
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
                case TokenType.At:
                    unary.DataType = DereferenceType(inner, out _);
                    if (unary.DataType is ErrorType)
                    {
                        UnaryError(unary, unary.DataType);
                    }
                    break;
                case TokenType.LenOf:
                    // TODO: replace with usize
                    unary.DataType = IntType.InstanceUnsigned;
                    if (!(inner is IArrayLikeType))
                    {
                        Error($"Expected type 'array' or 'string' for operator 'lenOf' but found type '{inner.DataTypeName}'.", unary.Expr.SourceStart);
                    }
                    break;
                case TokenType.New:
                    unary.DataType = ResolveNew(inner);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return unary.DataType;
        }

        AilurusDataType ResolveNew(AilurusDataType innerType)
        {
            if (innerType is ArrayType)
            {
                return innerType;
            }
            else
            {
                return new PointerType()
                {
                    BaseType = innerType,
                };
            }
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

            // TODO: Allow pointer assignments in let expressions
            if (!CanAssignTo(assertedType, initializerType, false, out string errorMessage))
            {
                Error(errorMessage, let.SourceStart);
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

        void ResolveFreeStatement(FreeStatement freeStatement)
        {
            var expression = ResolveExpression(freeStatement.Expr);
            if (!IsPointerType(expression) && !IsArrayLikeType(expression))
            {
                Error($"Argument to 'free' operator must be a pointer, array or string type, found {expression.DataTypeName}", freeStatement.SourceStart);
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
            if (!CanAssignTo(returnType, CurrentFunctionReturnType, false, out _))
            {
                Error($"Return statement has return type of {returnType.DataTypeName} which is incompatible with function return type of {CurrentFunctionReturnType.DataTypeName}", returnStatement.SourceStart);
            }
        }

        void ResolveForEachStatement(ForEachStatement forEach)
        {
            BeginScope();

            if (!CanDeclareName(forEach.Name.Identifier))
            {
                Error($"Variable with name '{forEach.Name.Lexeme}' already exists in this scope.", forEach.Name);
            }

            var iteratorType = ResolveExpression(forEach.IteratedValue);

            AilurusDataType elementType = ErrorType.Instance;
            if (iteratorType is IArrayLikeType a)
            {
                elementType = a.ElementType;
            }
            else
            {
                Error($"Expected type 'array' or type 'string' as iterator in foreach loop but instead found type '{iteratorType.DataTypeName}'.", forEach.IteratedValue.SourceStart);
            }

            AilurusDataType assertedType = elementType;
            if (forEach.AssertedTypeName != null)
            {
                assertedType = ResolveTypeName(forEach.AssertedTypeName);
            }

            // TODO: Allow for reference to elements
            if (!CanAssignTo(assertedType, elementType, false, out string errorMessage))
            {
                Error(errorMessage, forEach.Name);
            }

            var declaration = AddVariableToCurrentScope(
                forEach.Name,
                assertedType,
                forEach.IsMutable,
                true);

            forEach.Resolution = declaration;

            ResolveStatement(forEach.Body);

            EndScope();
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
                case StatementType.Free:
                    ResolveFreeStatement((FreeStatement)statement);
                    break;
                case StatementType.ForEach:
                    ResolveForEachStatement((ForEachStatement)statement);
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
            var argumentMutable = new List<bool>();
            foreach (var argument in declaration.Arguments)
            {
                var type = ResolveTypeName(argument.TypeName);
                if (!IsValidFunctionArgumentType(type))
                {
                    Error($"Type {type.DataTypeName} is not a valid type for a function argument", argument.TypeName.Name);
                }
                argumentTypes.Add(type);
                argumentMutable.Add(argument.IsMutable);
            }

            var returnType = ResolveTypeName(declaration.ReturnTypeName);

            declaration.FunctionType = new FunctionType()
            {
                ArgumentTypes = argumentTypes,
                ArgumentMutable = argumentMutable,
                ReturnType = returnType,
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
                var argument = declaration.Arguments[i];
                var dataType = declaration.FunctionType.ArgumentTypes[i];

                var resolution = AddVariableToCurrentScope(argument.Name, dataType, argument.IsMutable, true);
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