using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public ModuleScope RootModuleScope { get; set; }

        //TODO: Proper branch analysis and dead-code detection
        public bool WasInLoopBody { get; set; }
        public bool IsInLoopBody { get; set; }

        public bool IsInFunctionDefinition { get; set; }
        public AilurusDataType CurrentFunctionReturnType { get; set; }

        public bool HadError { get; set; }

        // Configuration Options
        // TODO: move to own file
        public bool PreventImplicitSignificanceLoss { get; set; } = true;
        public bool AllowPointerTypeErasure { get; set; }
        public bool AllowExplicitConstCast { get; set; }

        public bool PlaceholdersResolved { get; set; }

        void ResetScope()
        {
            Scopes = new List<BlockScope>();
            IsInLoopBody = false;
            WasInLoopBody = false;
            IsInFunctionDefinition = false;
            CurrentFunctionReturnType = null;
        }

        void Error(string message, Token token)
        {
            HadError = true;
            var error = new TypeError(message, token);
            Console.WriteLine(error);
        }

        void Warning(string message, Token token)
        {
            Console.WriteLine($"Warning:{token.SourceFile}:{token.Line}:{token.Column} - {message}");
        }

        #region EntryPoints

        public void ResolveRootModule(Module module)
        {
            RootModuleScope = ModuleScope;

            ResolveTypeDeclarationsFirstPass(module);
            ResolveTypeImportDeclarations(module);
            ResolveTypeDeclarationsSecondPass(module);
            PlaceholdersResolved = true;

            ResolveVariableDeclarations(module); // TODO: add a second pass for constexpr
            ResolveFunctionDeclarationsFirstPass(module);
            ResolveFunctionVariableImportDeclarations(module);
            ResolveFunctionDeclarationsSecondPass(module);
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
        ModuleScope EnterModuleScope(Module submodule)
        {
            var oldModuleScope = ModuleScope;
            ResetScope();
            if (!oldModuleScope.SubmoduleScopes.ContainsKey(submodule.ModuleName))
            {
                oldModuleScope.SubmoduleScopes[submodule.ModuleName] = new ModuleScope();
            }
            ModuleScope = oldModuleScope.SubmoduleScopes[submodule.ModuleName];

            return oldModuleScope;
        }

        void ExitModuleScope(ModuleScope oldModuleScope)
        {
            ResetScope();
            ModuleScope = oldModuleScope;
        }

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

        public Resolution FindVariableDefinition(QualifiedName name)
        {
            // TODO: Handle module resolution
            var concreteName = name.ConcreteName.Identifier;

            foreach (var scope in Scopes)
            {
                if (scope.VariableResolutions.ContainsKey(concreteName))
                {
                    return scope.VariableResolutions[concreteName];
                }
            }

            var moduleScope = LookupModuleScope(name);
            if (moduleScope == null)
            {
                return null;
            }

            Resolution nameResolution = null;
            if (moduleScope.VariableResolutions.ContainsKey(concreteName))
            {
                nameResolution = moduleScope.VariableResolutions[concreteName];
            }

            if (moduleScope.FunctionResolutions.ContainsKey(concreteName))
            {
                nameResolution = moduleScope.FunctionResolutions[concreteName];
            }

            if (moduleScope != ModuleScope
                && nameResolution != null
                && !nameResolution.IsExported) // Foregin variable
            {
                Error($"Unable to reference {name} since it was not exported.", name.SourceStart);
            }

            return nameResolution;
        }

        ModuleScope TraverseSubmodules(IEnumerable<Token> path, ModuleScope moduleScope)
        {
            foreach (var submoduleName in path) // Don't use the concrete name
            {
                if (moduleScope.SubmoduleScopes.ContainsKey(submoduleName.Identifier))
                {
                    moduleScope = moduleScope.SubmoduleScopes[submoduleName.Identifier];
                }
                else
                {
                    return null;
                }
            }

            return moduleScope;
        }

        ModuleScope LookupModuleScope(QualifiedName name)
        {
            ModuleScope moduleScope = ModuleScope;
            var path = name.Name.SkipLast(1);
            if (name.Name[0].Identifier == "root")
            {
                moduleScope = RootModuleScope;
                path = path.Skip(1);
            }

            return TraverseSubmodules(path, moduleScope);
        }

        AilurusDataType LookupTypeByName(QualifiedName name)
        {
            var declaration = LookupTypeDeclarationByName(name);
            if (declaration != null)
            {
                return declaration.DataType;
            }
            else
            {
                return null;
            }
        }

        TypeDeclaration LookupTypeDeclarationByName(QualifiedName name)
        {
            var baseName = name.ConcreteName.Identifier;
            // This works because all the types in the standard scope are reserved words so we can't alias them
            if (StandardScope.TypeDeclarations.ContainsKey(baseName))
            {
                return StandardScope.TypeDeclarations[baseName];
            }

            var qualifiedModuleSope = LookupModuleScope(name); // Scope not found

            if (qualifiedModuleSope is null)
            {
                return null;
            }

            TypeDeclaration declaration = null;
            if (qualifiedModuleSope.TypeDeclarations.ContainsKey(baseName))
            {
                declaration = qualifiedModuleSope.TypeDeclarations[baseName];
                if (declaration.State == TypeDeclaration.ResolutionState.Resolving)
                {
                    Error($"Circular type declaration detected while resolving type {baseName}", declaration.SourceStart);
                    declaration = StandardScope.ErrorDeclaration;
                }
                else if (declaration.State != TypeDeclaration.ResolutionState.Resolved)
                {
                    ResolveTypeDeclarationSecondPass(declaration);
                }
            }

            if (qualifiedModuleSope != ModuleScope
                && declaration != null
                && !declaration.IsExported) // Foregin variable
            {
                Error($"Unable to reference {name} since it was not exported.", name.SourceStart);
            }

            return declaration;
        }

        VariableResolution AddVariableToCurrentScope(
            Token name,
            AilurusDataType type,
            bool isMutable,
            bool initialized,
            bool isExported = false)
        {
            var resolution = new VariableResolution()
            {
                Name = name.Lexeme,
                DataType = type,
                IsMutable = isMutable,
                IsInitialized = initialized,
                IsExported = isExported
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

        AilurusDataType ResolveTypeName(QualifiedName name)
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
                        IsVariable = p.IsVariable,
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
                        BaseType = baseType,
                        IsVariable = a.IsVariable
                    };
                }
            }
            else if (typeName is TupleTypeName t)
            {
                var elementTypes =
                    t.ElementTypeNames
                    .Select(t => ResolveTypeName(t)).ToList();

                if (elementTypes.Find(e => e is ErrorType) != null)
                {
                    return ErrorType.Instance;
                }
                else
                {
                    return new TupleType()
                    {
                        MemberTypes = elementTypes
                    };
                }
            }
            else if (typeName is BaseTypeName b)
            {
                var type = LookupTypeByName(b.Name);
                if (type == null)
                {
                    return ErrorType.Instance;
                }

                if (type is PlaceholderType placeholder)
                {
                    if (placeholder.ResolvedType != null)
                    {
                        type = placeholder.ResolvedType;
                    }
                    else if (PlaceholdersResolved && placeholder == null)
                    {
                        Error($"Reference to unresolved type '{placeholder.TypeName}'.", placeholder.TypeName.Name.SourceStart);
                    }
                }

                if (b.VarModifier)
                {
                    if (type is StringType)
                    {
                        return new StringType()
                        {
                            IsVariable = true
                        };
                    }
                    else
                    {
                        Warning("'var' applied to type which cannot be variable. Modifier will be ignored.", b.Name.ConcreteName);
                    }
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
            if (t1 is AliasType alias1)
            {
                return TypesAreEqual(alias1.BaseType, t2);
            }
            else if (t2 is AliasType alias2)
            {
                return TypesAreEqual(t1, alias2.BaseType);
            }
            else if (t1 is PlaceholderType p1)
            {
                if (p1.ResolvedType == null && PlaceholdersResolved)
                {
                    Error($"Reference to unresolved type '{p1.TypeName}'.", p1.TypeName.Name.SourceStart);
                    return false; // Error Type
                }
                return TypesAreEqual(p1.ResolvedType, t2);
            }
            else if (t2 is PlaceholderType p2)
            {
                if (p2.ResolvedType == null && PlaceholdersResolved)
                {
                    Error($"Reference to unresolved type '{p2.TypeName}'.", p2.TypeName.Name.SourceStart);
                    return false; // Error Type
                }
                return TypesAreEqual(t1, p2.ResolvedType);
            }
            if (t1 is FunctionType f1 && t2 is FunctionType f2)
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
            else if (t1 is IArrayLikeType a1 && t2 is IArrayLikeType a2)
            {
                return (a1.IsVariable == a2.IsVariable)
                    && TypesAreEqual(a1.ElementType, a2.ElementType);
            }
            else if (t1 is TupleType tuple1 && t2 is TupleType tuple2)
            {
                if (tuple1.MemberTypes.Count != tuple2.MemberTypes.Count)
                {
                    return false;
                }

                for (var i = 0; i < tuple1.MemberTypes.Count; i++)
                {
                    if (!TypesAreEqual(tuple1.MemberTypes[i], tuple2.MemberTypes[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (t1 is PointerType p1 && t2 is PointerType p2)
            {
                return p1.IsVariable == p2.IsVariable
                    && TypesAreEqual(p1.BaseType, p2.BaseType);
            }
            else if (t1 is StructType s1 && t2 is StructType s2)
            {
                // Structs are nominally typed
                return s1 == s2;
            }
            else if (t1 is IntegralType i1 && t2 is IntegralType i2)
            {
                return i1.NumBytes == i2.NumBytes
                    && i1.Signed == i2.Signed;
            }
            else
            {
                // Just incase we forgot to use the instances
                return t1.GetType() == t2.GetType();
            }

        }

        bool CanCoerceNumericType(NumericType lhsType, NumericType rhsType)
        {
            // TODO: actual coercion
            return true;
        }

        bool CanAssignToInner(AilurusDataType lhsType, AilurusDataType rhsType, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (lhsType is ErrorType || rhsType is ErrorType)
            {
                return true;
            }
            else if (lhsType is AliasType lhsAlias)
            {
                return CanAssignToInner(lhsAlias.BaseType, rhsType, out errorMessage);
            }
            else if (rhsType is AliasType rhsAlias)
            {
                return CanAssignToInner(lhsType, rhsAlias.BaseType, out errorMessage);
            }
            else if (lhsType is PlaceholderType lhsPlaceHolder)
            {
                if (lhsPlaceHolder.ResolvedType == null)
                {
                    Error($"Reference to unresolved type {lhsPlaceHolder.TypeName}.", lhsPlaceHolder.TypeName.Name.SourceStart);
                    return true; // ErrorType
                }
                return CanAssignToInner(lhsPlaceHolder.ResolvedType, rhsType, out errorMessage);
            }
            else if (rhsType is PlaceholderType rhsPlaceHolder)
            {
                if (rhsPlaceHolder.ResolvedType == null)
                {
                    Error($"Reference to unresolved type {rhsPlaceHolder.TypeName}.", rhsPlaceHolder.TypeName.Name.SourceStart);
                    return true; // ErrorType
                }
                return CanAssignToInner(lhsType, rhsPlaceHolder.ResolvedType, out errorMessage);
            }
            else if (lhsType is PointerType && rhsType is NullType) // Any pointer type can be set to null
            {
                return true;
            }
            else if (lhsType is PointerType lhsPtr && rhsType is PointerType rhsPtr)
            {
                if (CanAssignToInner(lhsPtr.BaseType, rhsPtr.BaseType, out errorMessage))
                {
                    if (lhsPtr.IsVariable && !rhsPtr.IsVariable)
                    {
                        errorMessage = "Cannot assign a constant pointer to variable pointer without a cast.";
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (lhsType is IArrayLikeType lhsArray && rhsType is IArrayLikeType rhsArray)
            {
                // Type equality since we don't allow type coercion here.
                if (TypesAreEqual(lhsArray.ElementType, rhsArray.ElementType))
                {
                    if (lhsArray.IsVariable && !rhsArray.IsVariable)
                    {
                        errorMessage = "Cannot assign a constant array to a variable array without cast.";
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (lhsType is TupleType lhsTuple && rhsType is TupleType rhsTuple)
            {
                if (lhsTuple.MemberTypes.Count != rhsTuple.MemberTypes.Count)
                {
                    return false;
                }

                for (var i = 0; i < lhsTuple.MemberTypes.Count; i++)
                {
                    if (!CanAssignToInner(lhsTuple.MemberTypes[i], rhsTuple.MemberTypes[i], out errorMessage))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (lhsType is VariantType lhsVariant && rhsType is VariantMemberType rhsMember)
            {
                return lhsVariant.Members.ContainsKey(rhsMember.MemberName)
                    && TypesAreEqual(lhsVariant.Members[rhsMember.MemberName], rhsMember);
            }
            else if (lhsType is NumericType lhsNumeric && rhsType is NumericType rhsNumeric)
            {
                return CanImplicitNumericCast(lhsNumeric, rhsNumeric);
            }
            else
            {
                return TypesAreEqual(lhsType, rhsType);
            }
        }

        bool CanCompareEquality(AilurusDataType t1, AilurusDataType t2)
        {
            if (t1 is NumericType && t2 is NumericType)
            {
                return true;
            }
            else
            {
                return TypesAreEqual(t1, t2);
            }
        }

        bool CanAssignTo(AilurusDataType lhsType, AilurusDataType rhsType, bool pointerAssign, out string errorMessage)
        {
            // Do we dereference before we assign?
            if (pointerAssign)
            {
                lhsType = DereferenceType(lhsType, out bool lhsIsVariable);
                if (!lhsIsVariable)
                {
                    errorMessage = "Cannot mutate constant pointer.";
                    return false;
                }
            }

            if (!lhsType.Concrete)
            {
                errorMessage = $"Cannot instantiate a value of type '{lhsType.DataTypeName}' since it is not a concrete type.";
                return false;
            }

            errorMessage = $"Cannot assign value of type {rhsType.DataTypeName} to value of type {lhsType.DataTypeName}.";
            var canAssign = CanAssignToInner(lhsType, rhsType, out string newErrorMessage);

            if (!string.IsNullOrEmpty(newErrorMessage))
            {
                errorMessage = newErrorMessage;
            }

            return canAssign;
        }

        AilurusDataType GetNumericOperatorCoercion(AilurusDataType t1, AilurusDataType t2)
        {
            if (!(t1 is NumericType n1) || !(t2 is NumericType n2))
            {
                return ErrorType.Instance;
            }

            if (n1 is Float64Type ||
                n2 is Float64Type)
            {
                return Float64Type.Instance;
            }

            if (n1 is Float32Type ||
                n2 is Float32Type)
            {
                return Float32Type.Instance;
            }

            var i1 = n1 as IntegralType;
            var i2 = n2 as IntegralType;

            // Type is 'signed' if either type is signed
            var unsigned = true;
            unsigned = unsigned && !i1.Signed;
            unsigned = unsigned && !i2.Signed;

            if (i1.NumBytes == 8 || i2.NumBytes == 8)
            {
                return unsigned ? (IntegralType)Unsigned64Type.Instance : Signed64Type.Instance;
            }
            else if (i1.NumBytes == 4 || i2.NumBytes == 4)
            {
                return unsigned ? (IntegralType)Unsigned32Type.Instance : Signed32Type.Instance;
            }
            else if (i1.NumBytes == 2 || i2.NumBytes == 2)
            {
                return unsigned ? (IntegralType)Unsigned16Type.Instance : Signed16Type.Instance;
            }
            else if (i1.NumBytes == 1 || i2.NumBytes == 1)
            {
                return unsigned ? (IntegralType)Unsigned8Type.Instance : Signed8Type.Instance;
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

        bool IsMatchableLiteral(AilurusDataType type)
        {
            return (type is NumericType) ||
                (type is BooleanType) ||
                (type is StringType) ||
                (type is CharType);
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
                    return !i.Signed;
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

        AilurusDataType ToSigned(IntegralType t)
        {
            if (t.Signed)
            {
                return t;
            }

            AilurusDataType type = ErrorType.Instance;

            if (t is Unsigned8Type)
            {
                type = Signed16Type.Instance;
            }
            else if (t is Unsigned16Type)
            {
                type = Signed32Type.Instance;

            }
            else if (t is Unsigned32Type)
            {
                type = Signed64Type.Instance;
            }
            else if (t is Unsigned64Type)
            {
                if (!PreventImplicitSignificanceLoss)
                {
                    type = Signed64Type.Instance;
                }
            }
            else if (t is UnsignedSizeType uz)
            {
                var sizeType = uz.NextLargestType();
                if (sizeType != null)
                {
                    type = sizeType;
                }
            }

            return type;
        }

        AilurusDataType NumberTypeFromLiteral(NumberLiteral literal)
        {
            bool hasDecimal = literal.Number.Contains(".");

            NumericType explicitType = literal.DataTypeString switch
            {
                "u" => UnsignedSizeType.Instance,
                "u8" => Unsigned8Type.Instance,
                "u16" => Unsigned16Type.Instance,
                "u32" => Unsigned32Type.Instance,
                "u64" => Unsigned64Type.Instance,
                "i" => SignedSizeType.Instance,
                "i8" => Signed8Type.Instance,
                "i16" => Signed16Type.Instance,
                "i32" => Signed32Type.Instance,
                "i64" => Signed64Type.Instance,
                "f" => Float64Type.Instance,
                "f32" => Float32Type.Instance,
                "f64" => Float64Type.Instance,
                _ => hasDecimal ? (NumericType)Float64Type.Instance : SignedSizeType.Instance
            };

            if (hasDecimal && explicitType is IntegralType)
            {
                Error("Unable to create number literal without loss of significance.", literal.SourceStart);
                return ErrorType.Instance;
            }

            return explicitType;
        }

        object ConvertIntegral(
            string number,
            int numericBase,
            uint size,
            bool signed)
        {
            object value = null;
            try
            {
                value = (size, signed) switch
                {
                    (1, true) => Convert.ToSByte(number, numericBase),
                    (1, false) => Convert.ToByte(number, numericBase),
                    (2, true) => Convert.ToInt16(number, numericBase),
                    (2, false) => Convert.ToUInt16(number, numericBase),
                    (4, true) => Convert.ToInt32(number, numericBase),
                    (4, false) => Convert.ToUInt32(number, numericBase),
                    (8, true) => Convert.ToInt64(number, numericBase),
                    (8, false) => Convert.ToUInt64(number, numericBase),
                    _ => null
                };
            }
            catch (OverflowException) { }

            return value;
        }

        object ParseNumberLiteral(
            string number,
            int numericBase,
            AilurusDataType dataType)
        {
            if (dataType is IntegralType i)
            {
                return ConvertIntegral(number, numericBase, i.NumBytes, i is SignedIntegralType);
            }
            else if (dataType is FloatingPointType f)
            {
                if (f is Float32Type f32)
                {
                    return float.TryParse(number, out float f32v) ? (object)f32v : null;
                }
                else if (f is Float64Type f64)
                {
                    return double.TryParse(number, out double f64v) ? (object)f64v : null;
                }
            }
            return null;
        }

        bool CanImplicitNumericCast(NumericType fromN, NumericType toN)
        {
            if (!PreventImplicitSignificanceLoss)
            {
                return true;
            }

            if (fromN is FloatingPointType && toN is FloatingPointType)
            {
                return fromN.NumBytes <= toN.NumBytes;
            }
            else if (fromN is IntegralType fromI && toN is IntegralType toI)
            {
                // Can't convert a signed integer to an unsigned integer without cast 
                if (fromI.Signed && !toI.Signed)
                {
                    return false;
                }
                else
                {
                    // Convert it if one can fit inside the other.
                    return toI.IntegralBits <= fromI.IntegralBits;
                }
            }
            else if (fromN is SignedIntegralType fromI2 && toN is FloatingPointType toF)
            {
                return fromI2.IntegralBits <= toF.IntegralBits;
            }

            return false;
        }

        bool CanImplicitCast(AilurusDataType fromType, AilurusDataType toType)
        {
            if (fromType is PointerType fromP &&
                toType is PointerType toP &&
                AllowPointerTypeErasure)
            {
                if (!(fromP.BaseType is AnyType))
                {
                    return false;
                }

                return fromP.IsVariable == toP.IsVariable;

            }
            else if (fromType is NumericType fromN && toType is NumericType toN)
            {
                return CanImplicitNumericCast(fromN, toN);
            }

            return false;
        }

        bool CanExplicitCast(AilurusDataType fromType, AilurusDataType toType)
        {
            if (CanImplicitCast(fromType, toType))
            {
                return true;
            }

            if (fromType is PointerType fromP && toType is PointerType toP)
            {
                var varMatch = AllowExplicitConstCast ? true : fromP.IsVariable == toP.IsVariable;
                var typesMatch = AllowPointerTypeErasure ? true : TypesAreEqual(fromP, toP);
                return varMatch && typesMatch;
            }
            else if (fromType is NumericType && toType is NumericType)
            {
                // Can explicit cast any numeric transformation
                return true;
            }

            return false;
        }

        #endregion

        #region Resolve Expression

        #region Compound Expression Identifiers
        bool IsInitializerExpression(ExpressionNode node)
        {
            if (node is VarCast c)
            {
                return IsInitializerExpression(c.Expr);
            }
            else if (
                node is NewAlloc ||
                node is ArrayLiteral ||
                node is Literal)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool IsStringLiteral(ExpressionNode expr)
        {
            if (expr is Literal l && l.DataType is StringType)
            {
                return true;
            }
            else if (expr is VarCast v)
            {
                return IsStringLiteral(v.Expr);
            }
            else
            {
                return false;
            }
        }

        int? IntegralLiteral(ExpressionNode expr)
        {
            if (expr is Literal l && l.DataType is IntegralType)
            {
                return (int)l.Value;
            }
            else if (expr is VarCast v)
            {
                return IntegralLiteral(v.Expr);
            }
            else
            {
                return null;
            }
        }

        bool IsAssignmentTarget(ExpressionNode expr)
        {
            // TODO: support nested destructuring
            return expr is Variable || expr is ArrayIndex || expr is Get;
        }
        #endregion

        AilurusDataType ResolveExpression(ExpressionNode expr)
        {
            switch (expr.ExprType)
            {
                case ExpressionType.Literal:
                    return ResolveLiteral((Literal)expr);
                case ExpressionType.NumberLiteral:
                    return ResolveNumberLiteral((NumberLiteral)expr);
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
                    return ResolveArrayLiteral((ArrayLiteral)expr, false);
                case ExpressionType.ArrayIndex:
                    return ResolveArrayIndex((ArrayIndex)expr);
                case ExpressionType.ArraySetExpression:
                    return ResolveArraySet((ArraySetExpression)expr);
                case ExpressionType.New:
                    return ResolveNew((NewAlloc)expr);
                case ExpressionType.VarCast:
                    return ResolveVarCast((VarCast)expr);
                case ExpressionType.Tuple:
                    return ResolveTupleLiteral((TupleExpression)expr);
                case ExpressionType.TupleDestructure:
                    return ResolveTupleDestructure((TupleDestructure)expr);
                case ExpressionType.VariantConstructor:
                    return ResolveVariantConstructor((VariantConstructor)expr);
                case ExpressionType.VariantMemberAccess:
                    return ResolveVariantMemberAccess((VariantMemberAccess)expr);
                case ExpressionType.VariantCheck:
                    return ResolveVariantCheck((VariantCheck)expr);
                case ExpressionType.TypeCast:
                    return ResolveTypeCast((TypeCast)expr);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusDataType ResolveTypeCast(TypeCast cast)
        {
            var castToType = ResolveTypeName(cast.TypeName);
            var exprType = ResolveExpression(cast.Left);

            if (!CanExplicitCast(exprType, castToType))
            {
                Error($"Cannot convert type {exprType.DataTypeName} to {castToType.DataTypeName}", cast.SourceStart);
                return ErrorType.Instance;
            }

            cast.DataType = castToType;
            return cast.DataType;
        }

        AilurusDataType ResolveVariantCheck(VariantCheck expr)
        {
            var leftDataType = ResolveExpression(expr.Left);
            expr.DataType = BooleanType.Instance;

            if (!(leftDataType is VariantType variantType))
            {
                Error($"Expected variant type but instead found type '{leftDataType.DataTypeName}'.", expr.Left.SourceStart);
                return expr.DataType;
            }

            var memberName = expr.MemberName;
            if (!variantType.Members.ContainsKey(memberName.Identifier))
            {
                Error($"Varaint of type '{variantType.DataTypeName}' does not contain a member named '{memberName.Identifier}'.", expr.MemberName);
                return expr.DataType;
            }

            expr.MemberIndex = variantType.Members[memberName.Identifier].MemberIndex;

            return expr.DataType;
        }

        AilurusDataType ResolveVariantMemberAccess(VariantMemberAccess expr)
        {
            var callSiteType = ResolveExpression(expr.CallSite);
            expr.DataType = ErrorType.Instance;

            if (!(callSiteType is VariantType variantType))
            {
                Error($"Expected varaint type but instead found type '{callSiteType.DataTypeName}'.", expr.CallSite.SourceStart);
                return expr.DataType;
            }

            var memberName = expr.MemberName;
            if (!variantType.Members.ContainsKey(memberName.Identifier))
            {
                Error($"Varaint of type '{variantType.DataTypeName}' does not contain a member named '{memberName.Identifier}'.", expr.MemberName);
                return expr.DataType;
            }

            var memberType = variantType.Members[memberName.Identifier];
            expr.MemberIndex = memberType.MemberIndex;

            if (memberType.InnerType is EmptyVariantMemberType)
            {
                expr.IndexAsData = true;
                expr.DataType = SignedSizeType.Instance;
            }
            else
            {
                expr.DataType = memberType.InnerType;
            }

            return expr.DataType;
        }

        AilurusDataType ResolveVariantConstructor(VariantConstructor expr)
        {
            var constructorType = LookupTypeByName(expr.VariantName);
            expr.DataType = ErrorType.Instance;

            if (constructorType == null)
            {
                Error($"Unable to find variant type '{expr.VariantName}'.", expr.VariantName.SourceStart);
                return expr.DataType;
            }

            if (!(constructorType is VariantType variantType))
            {
                Error($"Type '{expr.VariantName}' is not a variant type.", expr.VariantName.SourceStart);
                return expr.DataType;
            }

            // This affects what type we infer
            expr.DataType = variantType;

            var memberName = expr.MemberName;
            if (!variantType.Members.ContainsKey(memberName.Identifier))
            {
                Error($"Variant '{variantType.VariantName} does not contain member '{memberName.Identifier}'.", memberName);
                return expr.DataType;
            }

            var memberType = variantType.Members[memberName.Identifier];
            expr.MemberType = memberType;

            var argCount = expr.Arguments?.Count ?? 0;

            var expectsArguments = !(memberType.InnerType is EmptyVariantMemberType);
            if (expectsArguments && argCount == 0)
            {
                Error($"Expected arguments for '{memberName.Identifier}' but found none.", memberName);
                return expr.DataType;
            }

            if (!expectsArguments && argCount > 0)
            {
                Error($"Variant member '{memberName.Identifier}' takes no arguments.", memberName);
                return expr.DataType;
            }

            if (expectsArguments)
            {
                if (memberType.InnerType is TupleType tuple)
                {
                    if (argCount != tuple.MemberTypes.Count)
                    {
                        Error($"Incorrect number of argument for variant constructor '{memberName.Identifier}'.", memberName);
                        return expr.DataType;
                    }

                    for (var i = 0; i < expr.Arguments.Count; i++)
                    {
                        var argumentType = ResolveExpression(expr.Arguments[i]);
                        var tupleMemberType = tuple.MemberTypes[i];
                        if (!CanAssignTo(tupleMemberType, argumentType, false, out string errorMessage))
                        {
                            Error(errorMessage, expr.Arguments[i].SourceStart);
                        }
                    }
                }
                else
                {
                    if (argCount > 1)
                    {
                        Error("$Incorrect number of arguments given for variant constructor.", memberName);
                        return expr.DataType;
                    }

                    var argumentType = ResolveExpression(expr.Arguments[0]);
                    if (!CanAssignTo(memberType.InnerType, argumentType, false, out string errorMessage))
                    {
                        Error(errorMessage, expr.Arguments[0].SourceStart);
                    }
                }
            }

            return expr.DataType;
        }

        AilurusDataType ResolveTupleAssignmentTarget(TupleExpression tuple)
        {
            foreach (var element in tuple.Elements)
            {
                if (!IsAssignmentTarget(element))
                {
                    Error($"Cannot assign to '{element.ExprType}'.", element.SourceStart);
                }
            }

            return ResolveTupleLiteral(tuple);
        }

        AilurusDataType ResolveTupleDestructure(TupleDestructure destructure)
        {
            var valueType = ResolveExpression(destructure.Value);
            var tupleType = ResolveTupleAssignmentTarget(destructure.AssignmentTarget);

            destructure.DataType = tupleType;

            if (valueType is TupleType valueTupleType)
            {
                if (!CanAssignTo(tupleType, valueTupleType, false, out string errorMessage))
                {
                    Error(errorMessage, destructure.SourceStart);
                }
            }
            else
            {
                Error($"Cannot destructure type '{valueType.DataTypeName}'.", destructure.Value.SourceStart);
            }

            return destructure.DataType;
        }

        AilurusDataType ResolveTupleLiteral(TupleExpression tuple)
        {
            var types = tuple.Elements.Select(e => ResolveExpression(e)).ToList();

            var tupleType = new TupleType()
            {
                MemberTypes = types
            };
            tuple.DataType = tupleType;

            return tuple.DataType;
        }

        AilurusDataType ResolveVarCast(VarCast expr)
        {
            var dataType = ResolveExpression(expr.Expr);
            if (dataType is ArrayType a && expr.Expr is ArrayLiteral)
            {
                dataType = new ArrayType()
                {
                    BaseType = a.BaseType,
                    IsVariable = true
                };
                expr.Expr.DataType = dataType;
            }
            // String constant
            else if (dataType is StringType && expr.Expr is Literal)
            {
                dataType = new StringType()
                {
                    IsVariable = true
                };
                expr.Expr.DataType = dataType;
            }
            else if (dataType is PointerType p && expr.Expr is NewAlloc)
            {
                dataType = new PointerType()
                {
                    BaseType = p.BaseType,
                    IsVariable = true,
                };
            }
            else
            {
                Error("Operator 'var' can only apply to Array Literals, String Literal, and 'new' expressions.", expr.SourceStart);
            }

            expr.DataType = dataType;
            return dataType;
        }

        AilurusDataType ResolveArraySet(ArraySetExpression expr)
        {
            var valueType = ResolveExpression(expr.Value);
            var arrayValueType = ResolveExpression(expr.ArrayIndex);

            if (expr.ArrayIndex.CallSite.DataType is IArrayLikeType a)
            {
                if (!a.IsVariable)
                {
                    Error("Cannot assign to array since its is not declared as variable.", expr.ArrayIndex.CallSite.SourceStart);
                }
            }

            if (!CanAssignTo(arrayValueType, valueType, expr.PointerAssign, out string errorMessage))
            {
                Error(errorMessage, expr.SourceStart);
            }

            expr.DataType = arrayValueType;
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

        AilurusDataType ResolveArrayLiteral(ArrayLiteral array, bool isInNewExpression)
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
                if (!isInNewExpression && !IsStaticExpression(array.FillLength))
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
            var structName = expr.StructName.ConcreteName;
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
                    Error($"Not all fields were initialized for struct '{structName}'.", expr.StructName.SourceStart);
                }
            }
            else
            {
                Error($"Expected struct type but found type {type.DataTypeName}.", expr.StructName.SourceStart);
            }

            return expr.DataType;
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
            expr.DataType = fieldType;
            if (structType != null)
            {
                if (!CanAssignTo(fieldType, valueType, expr.PointerAssign, out string errorMessage))
                {
                    Error(errorMessage, expr.SourceStart);
                }
            }

            return expr.DataType;
        }

        AilurusDataType ResolveGet(Get expr)
        {
            expr.DataType = ResolveFieldReference(expr, out StructType _);
            return expr.DataType;
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

            call.DataType = returnType;
            return call.DataType;
        }

        AilurusDataType ResolveAssign(Assign expr)
        {
            var assignment = ResolveExpression(expr.Assignment);
            var declaration = FindVariableDefinition(expr.Name);
            if (declaration == null)
            {
                Error($"No variable was found with name {expr.Name}", expr.Name.SourceStart);
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
                Error($"Cannot assign to {expr.Name}", expr.Name.SourceStart);
            }

            expr.DataType = assignment;
            return expr.DataType;
        }

        AilurusDataType ResolveVariable(Variable expr)
        {
            var resolution = FindVariableDefinition(expr.Name);

            expr.DataType = ErrorType.Instance;
            if (resolution == null)
            {
                Error($"No variable was found with name {expr.Name}", expr.SourceStart);
                return expr.DataType;
            }

            expr.Resolution = resolution;
            if (resolution is VariableResolution v)
            {
                expr.DataType = v.DataType;
            }
            else if (resolution is FunctionResolution f)
            {
                expr.DataType = f.Declaration.DataType;
            }

            return expr.DataType;
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
                    char _ => CharType.Instance,
                    _ => throw new NotImplementedException(),
                };
            }

            return literal.DataType;
        }

        AilurusDataType ResolveNumberLiteral(NumberLiteral literal)
        {
            // Already resolved
            if (literal.DataType != null)
            {
                return literal.DataType;
            }

            literal.DataType = NumberTypeFromLiteral(literal);

            if (!(literal.DataType is ErrorType))
            {
                literal.Value = ParseNumberLiteral(literal.Number, literal.Base, literal.DataType);

                // If the parse failed its because it doesn't fit since we already know its parseable
                if (literal.Value == null)
                {
                    Error($"Unable to fit {literal.Number} in literal of type {literal.DataType.DataTypeName}.", literal.SourceStart);
                }
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
                            unary.DataType = ToSigned(i);
                            if (unary.DataType is ErrorType)
                            {
                                Error($"Unable to negate type {i.DataTypeName} without significance loss.", unary.Operator);
                            }
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
                    unary.DataType = UnsignedSizeType.Instance;
                    if (!(inner is IArrayLikeType))
                    {
                        Error($"Expected type 'array' or 'string' for operator 'lenOf' but found type '{inner.DataTypeName}'.", unary.Expr.SourceStart);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return unary.DataType;
        }

        AilurusDataType ResolveNew(NewAlloc newExpression)
        {
            AilurusDataType exprType;
            if (newExpression.Expr is ArrayLiteral l)
            {
                exprType = ResolveArrayLiteral(l, true);
                newExpression.CopyInPlace = true;
            }
            else if (newExpression.Expr is VarCast varCast
            && varCast.Expr is ArrayLiteral ll)
            {
                exprType = ResolveArrayLiteral(ll, true);
                if (exprType is ArrayType a)
                {
                    a.IsVariable = true;
                }
                newExpression.CopyInPlace = true;
            }
            else
            {
                exprType = ResolveExpression(newExpression.Expr);
                if (IsStringLiteral(newExpression.Expr))
                {
                    newExpression.CopyInPlace = true;
                }
            }

            if (exprType is ArrayType || exprType is StringType)
            {
                newExpression.DataType = exprType;
            }
            else
            {
                newExpression.DataType = new PointerType()
                {
                    BaseType = exprType,
                };
            }

            return newExpression.DataType;
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

        void ResolveStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                ResolveStatement(statement);
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
                case StatementType.Free:
                    ResolveFreeStatement((FreeStatement)statement);
                    break;
                case StatementType.ForEach:
                    ResolveForEachStatement((ForEachStatement)statement);
                    break;
                case StatementType.MatchStatement:
                    ResolveMatchStatement((MatchStatement)statement);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusDataType GetAssertedType(TypeName assertedTypeName)
        {
            AilurusDataType assertedType = null;
            if (assertedTypeName != null)
            {
                assertedType = ResolveTypeName(assertedTypeName);
                if (assertedType is ErrorType)
                {
                    Error($"Asserted type '{assertedTypeName.Name}' could not be found.", assertedTypeName.Name.SourceStart);
                }
            }

            return assertedType;
        }

        void CheckModuleVariableInitialization(ExpressionNode initializer, Token sourceStart)
        {
            if (initializer != null)
            {
                if (!IsStaticExpression(initializer))
                {
                    Error($"Module variables must have a static initialization.", initializer.SourceStart);
                }
            }
            else
            {
                Error($"Module variables must be initialized.", sourceStart);
            }
        }

        void InheritVariability(AilurusDataType assertedType, AilurusDataType initializerType, ExpressionNode initializer)
        {
            // Inherit variability from asserted type rather than initializer type.
            if (assertedType is IArrayLikeType a &&
                a.IsVariable)
            {
                if (initializerType is IArrayLikeType aa)
                {
                    if (IsInitializerExpression(initializer))
                    {
                        aa.IsVariable = a.IsVariable;
                    }
                }
            }
        }

        bool DeclareVariable(
            Variable variable,
            AilurusDataType variableType,
            bool isMutable,
            bool initialized,
            bool isExported = false)
        {
            // Variable declarations should not qualify their names
            Debug.Assert(variable.Name.Name.Count == 1);

            var variableName = variable.Name.ConcreteName;
            if (!IsValidVariableName(variableName.Identifier))
            {
                Error($"Variable cannot have name '{variableName}' since it is reserved.", variableName);
                return false;
            }

            if (!CanDeclareName(variableName.Identifier))
            {
                Error($"Variable with name '{variableName.Identifier}' already exists in this scope.", variable.Name.ConcreteName);
                return false;
            }

            var declaration = AddVariableToCurrentScope(
                variableName,
                variableType,
                isMutable,
                initialized,
                isExported);

            variable.Resolution = declaration;
            return true;
        }

        bool DeclareLValue(
            ILValue assignmentTarget,
            AilurusDataType assertedType,
            bool isMutable,
            bool initialized,
            bool isExported)
        {
            if (assignmentTarget is TupleExpression tuple)
            {
                var assertedTupleType = assertedType as TupleType;

                if (tuple.Elements.Count != assertedTupleType.MemberTypes.Count)
                {
                    Error($"Asserted type {assertedTupleType.DataTypeName} does not match shape of target expression.", tuple.SourceStart);
                    return false;
                }

                var success = true;
                for (var i = 0; i < tuple.Elements.Count; i++)
                {

                    var variable = tuple.Elements[i] as Variable;
                    var variableType = assertedTupleType.MemberTypes[i];
                    success = success && DeclareVariable(
                        variable,
                        variableType,
                        isMutable,
                        initialized,
                        isExported);
                }

                return success;
            }
            else if (assignmentTarget is Variable variable)
            {
                return DeclareVariable(
                    variable,
                    assertedType,
                    isMutable,
                    initialized,
                    isExported);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void ResolveLet(LetStatement let, bool moduleVariable = false, bool isExported = false)
        {
            var assertedType = GetAssertedType(let.AssertedType);
            AilurusDataType initializerType = null;

            var initialized = let.Initializer != null;

            if (moduleVariable)
            {
                CheckModuleVariableInitialization(let.Initializer, let.SourceStart);
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
            else if (initializerType == null)
            {
                initializerType = assertedType;
            }

            if (assertedType == null)
            {
                Error($"Could not infer type for assignment.", let.SourceStart);
                return;
            }

            if (!assertedType.Concrete)
            {
                Error($"Unable to instantiate value of type '{assertedType.DataTypeName}' since it is not a concrete type.", let.SourceStart);
                return;
            }

            if (!CanAssignTo(assertedType, initializerType, false, out string errorMessage))
            {
                Error(errorMessage, let.SourceStart);
                return;
            }

            DeclareLValue(let.AssignmentTarget, assertedType, let.IsMutable, initialized, isExported);
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
            // The "else" branch of an if statement is also an if statement. If
            // its not an 'else if' branch, the predicate is null
            if (ifStatement.Predicate != null)
            {
                var predicate = ResolveExpression(ifStatement.Predicate);

                if (!(predicate is BooleanType))
                {
                    Error($"Expected if predicate to be of type {BooleanType.Instance.DataTypeName} but instead found type {predicate.DataTypeName}", ifStatement.SourceStart);
                }
            }

            ResolveBlock(ifStatement.ThenStatements);
            if (ifStatement.ElseStatements != null)
            {
                ResolveIfStatement(ifStatement.ElseStatements);
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

            if (!assertedType.Concrete)
            {
                Error($"Unable to instantiate value of type '{assertedType.DataTypeName}' since it is not a concrete type.", forEach.SourceStart);
            }

            bool canAssign = CanAssignTo(assertedType, elementType, false, out string errorMessage);
            if (!canAssign) // Can't assign the array element type to the decleared type
            {
                // See if we can assign a pointer to the array element instead
                if (assertedType is PointerType lhs)
                {
                    var rhs = new PointerType()
                    {
                        BaseType = elementType,
                    };
                    canAssign = CanAssignTo(lhs, rhs, false, out string _);
                    forEach.IterateOverReference = canAssign;
                }
            }

            if (!canAssign)
            {
                // Use the first error message since its probably closer to what they wanted
                Error(errorMessage, forEach.AssignmentTarget.SourceStart);
            }

            DeclareLValue(
                forEach.AssignmentTarget,
                assertedType,
                forEach.IsMutable,
                true, // Is initialized
                false); // Is not exported

            ResolveStatement(forEach.Body);

            EndScope();
        }

        void ResolveLiteralMatch(MatchStatement statement, AilurusDataType type)
        {
            statement.PatternsAreLiterals = true;
            foreach (var (match, pattern) in statement.Patterns)
            {
                if (match is Literal l)
                {
                    var matchType = ResolveExpression(l);
                    if (!CanCompareEquality(matchType, type))
                    {
                        Error($"Cannot match value of type '{type.DataTypeName}' with pattern of type '{matchType.DataTypeName}'.", match.SourceStart);
                    }
                }
                else
                {
                    Error($"Pattern must be literal of type '{type.DataTypeName}'.", match.SourceStart);
                }

                ResolveStatement(pattern);
            }

            if (statement.DefaultPattern != null)
            {
                ResolveStatement(statement.DefaultPattern);
            }
        }

        void ResolveVariantMatch(MatchStatement statement, VariantType type)
        {

        }

        void ResolveTupleMatch(MatchStatement statement, TupleType type)
        {

        }

        void ResolveMatchStatement(MatchStatement statement)
        {
            var exprType = ResolveExpression(statement.ToMatch);

            if (IsMatchableLiteral(exprType))
            {
                ResolveLiteralMatch(statement, exprType);
            }
            else if (exprType is VariantType v)
            {
                ResolveVariantMatch(statement, v);
            }
            else if (exprType is TupleType t)
            {
                ResolveTupleMatch(statement, t);
            }
        }

        #endregion

        #region Resolve Declarations

        void ResolveTypeDeclarationsFirstPass(Module module)
        {
            foreach (var typeDeclaration in module.TypeDeclarations)
            {
                // In the first pass we gather type names 
                ResolveTypeDeclarationFirstPass(typeDeclaration);
            }
            foreach (var submodule in module.Submodules)
            {
                // Could even be parallelized?
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveTypeDeclarationsFirstPass(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveTypeDeclarationsSecondPass(Module module)
        {
            foreach (var typeDeclaration in module.TypeDeclarations)
            {
                ResolveTypeDeclarationSecondPass(typeDeclaration);
            }
            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveTypeDeclarationsSecondPass(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveVariableDeclarations(Module module)
        {
            foreach (var variableDeclaration in module.VariableDeclarations)
            {
                ResolveModuleVariableDeclarations(variableDeclaration);
            }
            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveVariableDeclarations(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveFunctionDeclarationsFirstPass(Module module)
        {
            foreach (var functionDeclaration in module.FunctionDeclarations)
            {
                ResolveFunctionDeclarationFirstPass(functionDeclaration);
            }
            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveFunctionDeclarationsFirstPass(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveFunctionDeclarationsSecondPass(Module module)
        {
            foreach (var functionDeclaration in module.FunctionDeclarations)
            {
                ResolveFunctionDeclarationSecondPass(functionDeclaration);
            }
            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveFunctionDeclarationsSecondPass(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveTypeImportDeclarations(Module module)
        {
            foreach (var declaration in module.ImportDeclarations)
            {
                ResolveTypeImportDeclaration(declaration);
            }
            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveTypeImportDeclarations(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

        void ResolveFunctionVariableImportDeclarations(Module module)
        {
            foreach (var declaration in module.ImportDeclarations)
            {
                ResolveFunctionVariableImportDeclaration(declaration);
            }

            foreach (var submodule in module.Submodules)
            {
                var oldModuleScope = EnterModuleScope(submodule);
                ResolveFunctionVariableImportDeclarations(submodule);
                ExitModuleScope(oldModuleScope);
            }
        }

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

        bool ResolveVariantDeclarationFirstPass(VariantDeclaration v)
        {
            var variantName = v.VariantName.Identifier;
            if (!CanDeclareName(variantName))
            {
                Error($"Typename {variantName} is already declared in this module.", v.VariantName);
                return false;
            }

            var currentIndex = 0;
            var indices = new HashSet<int>();
            var memberTypes = new Dictionary<string, VariantMemberType>();

            foreach (var member in v.Members)
            {
                // Defaults to unsigned int by default
                AilurusDataType dataType = EmptyVariantMemberType.Instance;

                var memberName = member.MemberName.Identifier;
                if (memberTypes.ContainsKey(memberName))
                {
                    Error($"Variant already contains member with name '{memberName}'.", member.MemberName);
                    return false;
                }

                if (member.TypeName != null)
                {
                    dataType = new PlaceholderType() { TypeName = member.TypeName };
                }

                if (member.Index != null)
                {
                    var indexValue = IntegralLiteral(member.Index);
                    if (indexValue is null)
                    {
                        Error($"Variant index value must be an integer.", member.Index.SourceStart);
                        return false;
                    }
                    else
                    {
                        currentIndex = (int)indexValue;
                    }
                }

                if (indices.Contains(currentIndex))
                {
                    Error($"Variant with index {currentIndex} already exists for {variantName}.", member.SourceStart);
                    return false;
                }

                var memberType = new VariantMemberType()
                {
                    MemberName = memberName,
                    InnerType = dataType,
                    MemberIndex = currentIndex
                };

                memberTypes.Add(memberName, memberType);
                indices.Add(currentIndex);
                currentIndex++;
            }

            var variantType = new VariantType()
            {
                VariantName = variantName,
                Members = memberTypes
            };
            v.DataType = variantType;

            ModuleScope.TypeDeclarations.Add(variantName, v);

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
            else if (declaration is VariantDeclaration v)
            {
                added = ResolveVariantDeclarationFirstPass(v);
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
            else if (declaration is VariantDeclaration v)
            {
                ResolveVariantDeclarationSecondPass(v);
            }
            else if (declaration is TypeAliasDeclaration a)
            {
                ResolveAliasDeclarationSecondPass(a);
            }
            else
            {
                throw new NotImplementedException();
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
                Error($"Unable to declare type alias {alias.AliasName.Identifier}. Could not resolve type name {alias.AliasedTypeName.Name}", alias.AliasName);
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
                if (structType.Definitions[fieldName] is PlaceholderType placeholder)
                {
                    var resolvedType = placeholder.ResolvedType;
                    structType.Definitions[fieldName] = resolvedType;
                    if (resolvedType is ErrorType)
                    {
                        hadError = true;
                    }
                    if (!resolvedType.Concrete)
                    {
                        // TODO: Get the right span for error reporting.
                        Error($"Unable to instantiate value of type '{resolvedType.DataTypeName}' since it is not a concrete type.", s.SourceStart);
                    }
                }
            }

            if (hadError)
            {
                Error($"Unable to define struct {s.StructName.Identifier}. Unable to resolve one or more fields.", s.StructName);
            }

            s.State = TypeDeclaration.ResolutionState.Resolved;
        }

        void ResolveVariantDeclarationSecondPass(VariantDeclaration v)
        {
            v.State = TypeDeclaration.ResolutionState.Resolving;
            var variantType = v.DataType as VariantType;

            // Resolve the placeholder types
            foreach (var member in variantType.Members.Values)
            {
                if (member.InnerType is PlaceholderType p)
                {
                    member.InnerType = ResolvePlaceholder(p);
                    if (member.InnerType is ErrorType)
                    {
                        Error($"Unable to resolve type '{p.TypeName}'.", p.TypeName.Name.SourceStart);
                    }

                }
                var resolvedType = member.InnerType;
                if (!resolvedType.Concrete)
                {
                    // TODO: Get the right span for error reporting.
                    Error($"Unable to instantiate value of type '{resolvedType.DataTypeName}' since it is not a concrete type.", v.SourceStart);
                }
            }

            v.State = TypeDeclaration.ResolutionState.Resolved;
        }

        void ResolveModuleVariableDeclarations(ModuleVariableDeclaration declaration)
        {
            ResolveLet(declaration.Let, true, declaration.IsExported);
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
                    Error($"Type {type.DataTypeName} is not a valid type for a function argument", argument.TypeName.Name.SourceStart);
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

        bool ValidateReturnIfStatement(IfStatement statement)
        {
            if (statement == null)
            {
                return false;
            }
            if (statement.Predicate == null)
            {
                // Must have 'naked' else branch for all to return
                return true;
            }
            else
            {
                return ValidateReturn(statement.ThenStatements.Statements)
                    && ValidateReturnIfStatement(statement.ElseStatements);
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

            if (lastStatement is IfStatement i)
            {
                return ValidateReturnIfStatement(i);
            }
            else
            {
                return false;
            }
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

        void ResolveTypeImportDeclaration(ImportDeclaration import)
        {
            var declaration = LookupTypeDeclarationByName(import.Name);

            // Not a definition or else it doesn't exist
            if (declaration == null)
            {
                return;
            }

            if (!declaration.IsExported)
            {
                Error($"Unable to import {import.Name} since it was not exported.", import.Name.SourceStart);
                return;
            }

            // Intentionally after the declaration lookup so we don't print the message twice.
            var typeName = import.Name.ConcreteName.Identifier;
            if (!CanDeclareType(typeName))
            {
                Error($"Type name {typeName} is already declared in this module.", import.Name.ConcreteName);
                return;
            }

            ModuleScope.TypeDeclarations.Add(typeName, declaration);
            import.IsResolved = true;
        }

        void ResolveFunctionVariableImportDeclaration(ImportDeclaration import)
        {
            if (import.IsResolved)
            {
                return; // TypeDeclaration that's already resolved
            }

            var identifier = import.Name.ConcreteName.Identifier;
            if (!CanDeclareName(identifier))
            {
                Error($"Identifier with name '{identifier}' already exists in this module.", import.Name.ConcreteName);
                return;
            }

            var resolution = FindVariableDefinition(import.Name);

            if (resolution == null)
            {
                Error($"Value {import.Name} was not found in submodule.", import.Name.SourceStart);
                return;
            }

            if (!resolution.IsExported)
            {
                Error($"Unable to import {import.Name} since it was not exported.", import.Name.SourceStart);
                return;
            }

            if (resolution is FunctionResolution functionResolution)
            {
                ModuleScope.FunctionResolutions.Add(identifier, functionResolution);
            }
            else if (resolution is VariableResolution variableResolution)
            {
                ModuleScope.VariableResolutions.Add(identifier, variableResolution);
            }
            else
            {
                throw new NotImplementedException();
            }

            import.IsResolved = true;
        }

        #endregion
    }
}