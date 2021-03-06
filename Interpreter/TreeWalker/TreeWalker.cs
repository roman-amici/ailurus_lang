using System;
using System.Collections.Generic;
using System.Linq;
using AilurusLang.DataType;
using AilurusLang.Interpreter.Runtime;
using AilurusLang.Interpreter.TreeWalker.Evaluators;
using AilurusLang.Parsing.AST;
using AilurusLang.Scanning;

namespace AilurusLang.Interpreter.TreeWalker
{
    public class TreeWalker
    {
        public Evaluator Evaluator { get; set; }

        public TreeWalkerEnvironment ModuleEnvironment { get; set; } = new TreeWalkerEnvironment();

        public List<List<TreeWalkerEnvironment>> CallStack { get; set; } = new List<List<TreeWalkerEnvironment>>();

        public TreeWalker(Evaluator evaluator)
        {
            Evaluator = evaluator;
        }

        #region Environment Management

        void PushBlockEnvironment()
        {
            var blockEnvs = CallStack[^1];
            blockEnvs.Add(new TreeWalkerEnvironment());
        }

        void PopBlockEnvironment()
        {
            var blockEnvs = CallStack[^1];
            var env = blockEnvs[^1];
            blockEnvs.RemoveAt(blockEnvs.Count - 1);
            env.MarkInvalid();
        }

        void EnterFunctionEnvironment()
        {
            CallStack.Add(new List<TreeWalkerEnvironment>());
            PushBlockEnvironment();
        }

        void ExitFunctionEnvironment()
        {
            PopBlockEnvironment();
            CallStack.RemoveAt(CallStack.Count - 1);
        }

        TreeWalkerEnvironment GetEnvironmentForVariableResolution(VariableResolution resolution)
        {
            if (resolution.ScopeDepth == null)
            {
                return ModuleEnvironment;
            }
            else
            {
                return CallStack[^1][(int)resolution.ScopeDepth];
            }
        }

        #endregion

        #region Module

        public int EvalRootModule(Module rootModule)
        {
            EvalModule(rootModule);

            FunctionDeclaration mainFunction = null;
            foreach (var function in rootModule.FunctionDeclarations)
            {
                if (function.FunctionName.Identifier == "main")
                {
                    mainFunction = function;
                    break;
                }
            }

            if (mainFunction == null)
            {
                throw new RuntimeError("Unable to execute module, no 'main' function was found.", rootModule.SourceStart);
            }
            else
            {
                var mainCall = new Call()
                {
                    Callee = new Variable()
                    {
                        Resolution = mainFunction.Resolution
                    },
                    ArgumentList = new List<ExpressionNode>(),
                };
                var returnCode = EvalCallExpression(mainCall);

                // Void
                if (returnCode == null)
                {
                    return 0;
                }
                else
                {
                    return returnCode.GetAs<int>();
                }
            }
        }

        void EvalModule(Module module)
        {
            foreach (var variable in module.VariableDeclarations)
            {
                EvalLetStatement(variable.Let);
            }

            foreach (var function in module.FunctionDeclarations)
            {
                EvalFunctionDeclaration(function);
            }

            foreach (var submodule in module.Submodules)
            {
                EvalModule(submodule);
            }

        }

        void EvalFunctionDeclaration(FunctionDeclaration function)
        {
            var functionPointer = new FunctionPointer()
            {
                FunctionDeclaration = function
            };
            ModuleEnvironment.SetValue(function.Resolution, functionPointer);
        }

        #endregion

        #region Statements

        public void EvalStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                EvalStatement(statement);
            }
        }

        public void EvalStatement(StatementNode stmt)
        {
            switch (stmt)
            {
                case ExpressionStatement expressionStatement:
                    EvalExpression(expressionStatement.Expr);
                    break;
                case LetStatement letStatement:
                    EvalLetStatement(letStatement);
                    break;
                case PrintStatement printStatement:
                    EvalPrintStatement(printStatement);
                    break;
                case BlockStatement blockStatement:
                    EvalBlockStatement(blockStatement);
                    break;
                case IfStatement ifStatement:
                    EvalIfStatement(ifStatement);
                    break;
                case WhileStatement whileStatement:
                    EvalWhileStatement(whileStatement);
                    break;
                case ForStatement forStatement:
                    EvalForStatement(forStatement);
                    break;
                case BreakStatement breakStatement:
                    EvalBreakStatement(breakStatement);
                    break;
                case ContinueStatement continueStatement:
                    EvalContinueStatement(continueStatement);
                    break;
                case ReturnStatement returnStatement:
                    EvalReturnStatement(returnStatement);
                    break;
                case ForEachStatement forEachStatement:
                    EvalForEachStatement(forEachStatement);
                    break;
                case FreeStatement freeStatement:
                    EvalFreeStatement(freeStatement);
                    break;
                case MatchStatement matchStatement:
                    EvalMatchStatement(matchStatement);
                    break;
                default:
                    throw new NotImplementedException();

            }
        }

        void EvalMatchLiterals(MatchStatement match, AilurusValue toMatch)
        {
            foreach (var (matchable, statement) in match.Patterns)
            {
                var patternValue = EvalExpression((Literal)matchable);
                if (Evaluator.EvalEquality(patternValue, toMatch, null).GetAs<bool>())
                {
                    EvalStatement(statement);
                    return;
                }
            }

            if (match.DefaultPattern != null)
            {
                EvalStatement(match.DefaultPattern);
            }
        }

        void EvalMatchVariants(MatchStatement match, VariantInstance toMatch)
        {
            var matchMemberName = toMatch.VariantMemberType.MemberName;
            foreach (var (matchable, statement) in match.Patterns)
            {
                var matchableVariant = matchable as VariantDestructure;
                if (matchMemberName == matchableVariant.VariantName.Identifier)
                {
                    PushBlockEnvironment();

                    if (matchableVariant.LValue != null)
                    {
                        AssignLValue(matchableVariant.LValue, toMatch.Value.Value.ByValue());
                    }
                    EvalStatement(statement);
                    PopBlockEnvironment();
                    return;
                }
            }

            if (match.DefaultPattern != null)
            {
                EvalStatement(match.DefaultPattern);
            }
        }

        void EvalMatchStatement(MatchStatement matchStatement)
        {
            var toMatch = EvalExpression(matchStatement.ToMatch);

            if (matchStatement.PatternsAreLiterals)
            {
                EvalMatchLiterals(matchStatement, toMatch);
            }
            else
            {
                EvalMatchVariants(matchStatement, toMatch.GetAs<VariantInstance>());
            }
        }

        void EvalFreeStatement(FreeStatement freeStatement)
        {
            var pointer = EvalExpression(freeStatement.Expr).GetAs<Pointer>();

            if (pointer is NullPointer)
            {
                throw new RuntimeError("Attempted to free null pointer.", freeStatement.SourceStart);
            }

            if (!pointer.Memory.IsOnHeap)
            {
                throw new RuntimeError("Attempted to free memory not on the heap", freeStatement.Expr.SourceStart);
            }

            if (!pointer.Memory.IsValid)
            {
                throw new RuntimeError("Attempted to free memory that has already been freed.", freeStatement.Expr.SourceStart);
            }
        }

        void EvalForEachStatement(ForEachStatement forEach)
        {
            var iteratedValue = EvalExpression(forEach.IteratedValue).GetAs<IArrayInstanceLike>();

            PushBlockEnvironment();

            for (var i = 0; i < iteratedValue.Count; i++)
            {
                if (!iteratedValue.AccessIsValid(i))
                {
                    throw new RuntimeError("Attempt to access array like with invalid memory.", forEach.IteratedValue.SourceStart);
                }

                if (forEach.AssignmentTarget is TupleExpression tupleVariables)
                {
                    if (forEach.IterateOverReference)
                    {
                        throw new NotImplementedException();
                    }

                    var tupleValue = iteratedValue[i].ByValue().GetAs<TupleInstance>();
                    for (var j = 0; j < tupleVariables.Elements.Count; j++)
                    {
                        var variable = tupleVariables.Elements[j] as Variable;
                        var value = tupleValue[j];
                        var env = GetEnvironmentForVariable(variable.Resolution);
                        env.SetValue(variable.Resolution, value);
                    }
                }
                else if (forEach.AssignmentTarget is Variable variable)
                {
                    ;
                    var env = GetEnvironmentForVariable(variable.Resolution);
                    if (forEach.IterateOverReference)
                    {
                        var ptr = new Pointer() { Memory = iteratedValue.GetElementAddress(i) };
                        env.SetValue(variable.Resolution, ptr);
                    }
                    else
                    {
                        env.SetValue(variable.Resolution, iteratedValue[i].ByValue());
                    }
                }

                EvalBlockStatement(forEach.Body);
            }

            PopBlockEnvironment();
        }

        void EvalReturnStatement(ReturnStatement returnStatement)
        {
            var returnValue = EvalExpression(returnStatement.ReturnValue);
            throw new ControlFlowException(ControlFlowType.Return)
            {
                ReturnValue = returnValue.ByValue()
            };
        }

        void EvalContinueStatement(ContinueStatement _)
        {
            throw new ControlFlowException(ControlFlowType.Continue);
        }

        void EvalBreakStatement(BreakStatement _)
        {
            throw new ControlFlowException(ControlFlowType.Break);
        }

        void EvalForStatement(ForStatement forStatement)
        {
            EvalStatement(forStatement.Initializer);
            while (EvalExpression(forStatement.Predicate).GetAs<bool>())
            {
                try
                {
                    EvalStatement(forStatement.Statements);
                }
                catch (ControlFlowException e)
                {
                    if (e.ControlFlowType == ControlFlowType.Break)
                    {
                        break;
                    }
                    else if (e.ControlFlowType == ControlFlowType.Continue)
                    {
                        continue;
                    }
                    else
                    {
                        throw e;
                    }
                }
                EvalExpression(forStatement.Update);
            }
        }

        void EvalWhileStatement(WhileStatement whileStatement)
        {
            if (whileStatement.IsDoWhile)
            {
                while (EvalExpression(whileStatement.Predicate).GetAs<bool>())
                {
                    try
                    {
                        EvalStatement(whileStatement.Statements);
                    }
                    catch (ControlFlowException e)
                    {
                        if (e.ControlFlowType == ControlFlowType.Break)
                        {
                            break;
                        }
                        else if (e.ControlFlowType == ControlFlowType.Continue)
                        {
                            continue;
                        }
                        else
                        {
                            throw e;
                        }
                    }
                }
            }
            else
            {
                do
                {
                    try
                    {
                        EvalStatement(whileStatement.Statements);
                    }
                    catch (ControlFlowException e)
                    {
                        if (e.ControlFlowType == ControlFlowType.Break)
                        {
                            break;
                        }
                        else if (e.ControlFlowType == ControlFlowType.Continue)
                        {
                            continue;
                        }
                        else
                        {
                            throw e;
                        }
                    }
                } while (EvalExpression(whileStatement.Predicate).GetAs<bool>());
            }
        }

        void EvalIfStatement(IfStatement ifStatement)
        {
            // Else branch with no elseif is always 'true'
            bool pred = true;
            if (ifStatement.Predicate != null)
            {
                pred = EvalExpression(ifStatement.Predicate).GetAs<bool>();
            }

            if (pred)
            {
                EvalBlockStatement(ifStatement.ThenStatements);
            }
            else if (ifStatement.ElseStatements != null)
            {
                EvalIfStatement(ifStatement.ElseStatements);
            }
        }

        void EvalPrintStatement(PrintStatement print)
        {
            var value = EvalExpression(print.Expr);
            Console.WriteLine($"{value}");
        }

        void AssignLValue(ILValue assignmentTarget, AilurusValue initializer)
        {
            if (assignmentTarget is TupleExpression tupleVariables)
            {
                var tupleInitializer = initializer.GetAs<TupleInstance>();
                for (var i = 0; i < tupleVariables.Elements.Count; i++)
                {
                    var variable = tupleVariables.Elements[i] as Variable;
                    var value = tupleInitializer[i];
                    var resolution = variable.Resolution as VariableResolution;
                    var env = GetEnvironmentForVariableResolution(resolution);
                    env.SetValue(resolution, value);
                }
            }
            else if (assignmentTarget is Variable variable)
            {
                var resolution = variable.Resolution as VariableResolution;
                var env = GetEnvironmentForVariableResolution(resolution);
                env.SetValue(resolution, initializer);
            }
        }

        void EvalLetStatement(LetStatement stmt)
        {
            AilurusValue initializer = null;
            if (stmt.Initializer != null)
            {
                initializer = EvalExpression(stmt.Initializer);
            }

            AssignLValue(stmt.AssignmentTarget, initializer);

        }

        void EvalBlockStatement(BlockStatement block)
        {
            PushBlockEnvironment();
            foreach (var statement in block.Statements)
            {
                EvalStatement(statement);
            }
            PopBlockEnvironment();
        }

        #endregion

        #region Expressions

        public AilurusValue EvalExpression(ExpressionNode expr)
        {
            return expr.ExprType switch
            {
                ExpressionType.Literal => Evaluator.EvalLiteral((Literal)expr),
                ExpressionType.NumberLiteral => Evaluator.EvalNumberLiteral((NumberLiteral)expr),
                ExpressionType.Unary => EvalUnary((Unary)expr),
                ExpressionType.Binary => EvalBinary((Binary)expr),
                ExpressionType.BinaryShortCircut => EvalBinaryShortCircut((BinaryShortCircut)expr),
                ExpressionType.IfExpression => EvalIfExpression((IfExpression)expr),
                ExpressionType.Variable => EvalVariableExpression((Variable)expr),
                ExpressionType.Assign => EvalAssignExpression((Assign)expr),
                ExpressionType.Call => EvalCallExpression((Call)expr),
                ExpressionType.Get => EvalGetExpression((Get)expr),
                ExpressionType.Set => EvalSetExpression((SetExpression)expr),
                ExpressionType.StructInitialization => EvalStructInitialization((StructInitialization)expr),
                ExpressionType.AddrOfExpression => EvalAddrOfExpression((AddrOfExpression)expr),
                ExpressionType.ArrayLiteral => EvalArrayLiteral((ArrayLiteral)expr),
                ExpressionType.ArrayIndex => EvalArrayIndex((ArrayIndex)expr),
                ExpressionType.ArraySetExpression => EvalArraySet((ArraySetExpression)expr),
                ExpressionType.New => EvalNewAlloc((NewAlloc)expr),
                ExpressionType.VarCast => EvalVarCast((VarCast)expr),
                ExpressionType.Tuple => EvalTupleLiteral((TupleExpression)expr),
                ExpressionType.TupleDestructure => EvalTupleDestructure((TupleDestructure)expr),
                ExpressionType.VariantConstructor => EvalVariantConstructor((VariantConstructor)expr),
                ExpressionType.VariantMemberAccess => EvalVariantMemberAccess((VariantMemberAccess)expr),
                ExpressionType.VariantCheck => EvalVariantCheck((VariantCheck)expr),
                ExpressionType.TypeCast => EvalTypeCast((TypeCast)expr),
                _ => throw new NotImplementedException(),
            };
        }

        AilurusValue EvalTypeCast(TypeCast expr)
        {
            // Dynamic handling will deal with this
            return EvalExpression(expr.Left).CastTo(expr.DataType);
        }

        AilurusValue EvalVariantCheck(VariantCheck check)
        {
            var left = EvalExpression(check.Left).GetAs<VariantInstance>();

            var equal = left.Index == check.MemberIndex;

            return new DynamicValue() { Value = equal };
        }

        AilurusValue EvalVariantMemberAccess(VariantMemberAccess access)
        {
            var callSite = EvalExpression(access.CallSite).GetAs<VariantInstance>();

            if (callSite.Index != access.MemberIndex)
            {
                throw new RuntimeError("Access variant member with wrong index.", access.SourceStart);
            }

            if (access.IndexAsData)
            {
                return new DynamicValue() { Value = callSite.Index };
            }
            else
            {
                return callSite.Value.Value.ByValue();
            }
        }

        AilurusValue EvalVariantConstructor(VariantConstructor expr)
        {
            var memberType = expr.MemberType;
            AilurusValue value = null;
            if (memberType.InnerType is TupleType)
            {
                var members = new List<AilurusValue>();
                foreach (var expression in expr.Arguments)
                {
                    members.Add(EvalExpression(expression).ByValue());
                }

                value = new TupleInstance(members, false);
            }
            else if (!(memberType.InnerType is EmptyVariantMemberType))
            {
                value = EvalExpression(expr.Arguments[0]).ByValue();
            }

            return new VariantInstance()
            {
                VariantType = expr.DataType as VariantType,
                VariantMemberType = expr.MemberType,
                Value = new MemoryLocation() { Value = value },
                Index = memberType.MemberIndex
            };
        }

        AilurusValue EvalTupleDestructure(TupleDestructure destructure)
        {
            var value = EvalExpression(destructure.Value).GetAs<TupleInstance>();

            var targetElements = destructure.AssignmentTarget.Elements;
            for (int i = 0; i < targetElements.Count; i++)
            {
                var targetElement = targetElements[i];
                var valueElement = value[i].ByValue();

                if (targetElement is Variable variable)
                {
                    var env = GetEnvironmentForVariable(variable.Resolution);
                    env.SetValue(variable.Resolution, valueElement);
                }
                else if (targetElement is Get get)
                {
                    var callSite = EvalExpression(get.CallSite).GetAs<StructInstance>();
                    callSite[get.FieldName.Identifier] = valueElement;
                }
                else if (targetElement is ArrayIndex arrayIndex)
                {
                    var array = EvalExpression(arrayIndex.CallSite).GetAs<IArrayInstanceLike>();
                    var index = EvalExpression(arrayIndex.IndexExpression).GetAs<int>();

                    CheckArrayIndex(index, array, arrayIndex.SourceStart);

                    array[index] = valueElement;
                }
            }

            return value;
        }

        AilurusValue EvalTupleLiteral(TupleExpression expr)
        {
            var elements = expr.Elements.Select(e => EvalExpression(e));

            return new TupleInstance(elements, false);
        }

        AilurusValue EvalVarCast(VarCast expr)
        {
            // Var cast is purely for the type system.
            return EvalExpression(expr.Expr);
        }

        void CheckArrayIndex(int index, IArrayInstanceLike array, Token sourceStart)
        {
            if (index >= array.Count)
            {
                throw new RuntimeError("Array index out of bounds", sourceStart);
            }

            if (!array.AccessIsValid(index))
            {
                throw new RuntimeError("Attempt to access array which is not initialized.", sourceStart);
            }
        }

        AilurusValue EvalArraySet(ArraySetExpression expr)
        {
            var value = EvalExpression(expr.Value);
            var array = EvalExpression(expr.ArrayIndex.CallSite).GetAs<IArrayInstanceLike>();
            var index = EvalExpression(expr.ArrayIndex.IndexExpression).GetAs<int>();

            CheckArrayIndex(index, array, expr.SourceStart);

            array[index] = value;

            return value;
        }

        AilurusValue EvalArrayIndex(ArrayIndex indexExpr)
        {
            var array = EvalExpression(indexExpr.CallSite).GetAs<IArrayInstanceLike>();
            var index = EvalExpression(indexExpr.IndexExpression).GetAs<int>();

            if (index >= array.Count)
            {
                throw new RuntimeError("Array index out of bounds", indexExpr.SourceStart);
            }

            if (!array.AccessIsValid(index))
            {
                throw new RuntimeError("Attempt to access array which is not initialized.", indexExpr.CallSite.SourceStart);
            }

            return array[index];
        }

        ArrayInstance EvalArrayLiteral(ArrayLiteral array)
        {
            var values = new List<AilurusValue>();

            if (array.Elements != null)
            {
                foreach (var element in array.Elements)
                {
                    values.Add(EvalExpression(element));
                }
            }

            if (array.FillExpression != null)
            {
                var fillValue = EvalExpression(array.FillExpression);
                var length = EvalExpression(array.FillLength).GetAs<int>();

                for (var i = 0; i < length; i++)
                {
                    values.Add(fillValue);
                }
            }

            return new ArrayInstance(values, false);
        }

        MemoryLocation LoadEffectiveAddress(ExpressionNode expr)
        {
            if (expr is Variable v)
            {
                var environment = GetEnvironmentForVariable(v.Resolution);
                return environment.GetAddress(v.Resolution);
            }
            if (expr is Get g)
            {
                var callSite = EvalExpression(g.CallSite).GetAs<StructInstance>();
                return callSite.GetMemberAddress(g.FieldName.Identifier);
            }
            if (expr is ArrayIndex a)
            {
                var index = EvalExpression(a.IndexExpression).GetAs<int>();
                var callSite = EvalExpression(a.CallSite).GetAs<IArrayInstanceLike>();
                return callSite.GetElementAddress(index);
            }

            // Unreachable?
            return null;
        }

        Pointer EvalAddrOfExpression(AddrOfExpression expr)
        {
            var memory = LoadEffectiveAddress(expr.OperateOn);
            return new Pointer()
            {
                Memory = memory
            };
        }

        AilurusValue EvalStructInitialization(StructInitialization expr)
        {
            var values = new Dictionary<string, MemoryLocation>();
            foreach (var (fieldName, initializer) in expr.Initializers)
            {
                var value = EvalExpression(initializer);
                values.Add(fieldName.Identifier, new MemoryLocation() { Value = value });
            }
            return new StructInstance()
            {
                StructType = (StructType)expr.DataType,
                Members = values
            };
        }

        AilurusValue EvalGetExpression(Get expr)
        {
            var callSite = EvalExpression(expr.CallSite);

            while (callSite is Pointer ptr)
            {
                if (!ptr.IsValid)
                {
                    throw new RuntimeError("Attempt to deref pointer to invalid memory.", expr.SourceStart);
                }
                callSite = ptr.Deref();
            }

            var value = callSite.GetAs<StructInstance>();
            return value[expr.FieldName.Identifier];
        }

        AilurusValue EvalCallExpression(Call call)
        {
            var callee = EvalExpression(call.Callee).GetAs<FunctionPointer>();

            var arguments = new List<AilurusValue>();
            foreach (var argExpr in call.ArgumentList)
            {
                var value = EvalExpression(argExpr);
                arguments.Add(value);
            }

            EnterFunctionEnvironment();
            for (var i = 0; i < arguments.Count; i++)
            {
                var value = arguments[i].ByValue();
                var resolution = callee.FunctionDeclaration.ArgumentResolutions[i];
                CallStack[^1][^1].SetValue(resolution, value);
            }

            AilurusValue returnValue = null; // Void. Typechecker will protect us.
            try
            {
                EvalStatements(callee.FunctionDeclaration.Statements);
            }
            catch (ControlFlowException control)
            {
                if (control.ControlFlowType == ControlFlowType.Return)
                {
                    returnValue = control.ReturnValue;
                }
                else
                {
                    throw control;
                }
            }
            ExitFunctionEnvironment();

            return returnValue;
        }

        AilurusValue EvalUnary(Unary unary)
        {
            var value = EvalExpression(unary.Expr);
            return unary.Operator.Type switch
            {
                TokenType.Bang => Evaluator.EvalUnaryBang(value, unary),
                TokenType.Minus => Evaluator.EvalUnaryMinus(value, unary),
                TokenType.At => EvalUnaryDereference(value, unary),
                TokenType.LenOf => EvalLenOf(value),
                _ => throw new NotImplementedException(),
            };
        }

        AilurusValue EvalNewAlloc(NewAlloc newAlloc)
        {
            var value = EvalExpression(newAlloc.Expr);

            if (value is ArrayInstance a && newAlloc.CopyInPlace)
            {
                return new ArrayInstance(a, true);
            }
            else if (value is StringInstance s)
            {
                return new StringInstance(s, true);
            }
            else
            {
                return new Pointer() { Memory = new MemoryLocation() { Value = value.ByValue() } };
            }
        }

        AilurusValue EvalLenOf(AilurusValue value)
        {
            var instance = value.GetAs<IArrayInstanceLike>();
            var count = instance.Count;

            if (instance.Count > 0 && instance.AccessIsValid(0))
            {
                throw new RuntimeError("Attempted to get len of invalid ArrayLike.");
            }
            return new DynamicValue() { Value = count };
        }

        AilurusValue EvalUnaryDereference(AilurusValue value, Unary unary)
        {
            var pointer = value.GetAs<Pointer>();
            if (!pointer.IsValid)
            {
                throw new RuntimeError("Attempted to dereference invalid pointer.", unary.SourceStart);
            }
            return pointer.Deref();
        }

        AilurusValue EvalBinary(Binary binary)
        {
            var left = EvalExpression(binary.Left);
            var right = EvalExpression(binary.Right);
            return binary.Operator.Type switch
            {
                TokenType.Plus => Evaluator.EvalPlus(left, right, binary),
                TokenType.Minus => Evaluator.EvalMinus(left, right, binary),
                TokenType.Star => Evaluator.EvalTimes(left, right, binary),
                TokenType.Slash => Evaluator.EvalDivision(left, right, binary),
                TokenType.Amp => Evaluator.EvalBitwiseAnd(left, right, binary),
                TokenType.Bar => Evaluator.EvalBitwiseOr(left, right, binary),
                TokenType.Carrot => Evaluator.EvalBitwiseXOr(left, right, binary),
                TokenType.EqualEqual => Evaluator.EvalEquality(left, right, binary),
                TokenType.Greater => Evaluator.EvalGreater(left, right, binary),
                TokenType.GreaterEqual => Evaluator.EvalGreaterEqual(left, right, binary),
                TokenType.Less => Evaluator.EvalLess(left, right, binary),
                TokenType.LessEqual => Evaluator.EvalLessEqual(left, right, binary),
                _ => throw new NotImplementedException(),
            };
        }

        AilurusValue EvalBinaryShortCircut(BinaryShortCircut binary)
        {
            AilurusValue left;
            AilurusValue right;
            switch (binary.Operator.Type)
            {
                case TokenType.AmpAmp:
                    left = EvalExpression(binary.Left);
                    if (left.AssertType(BooleanType.Instance))
                    {
                        right = EvalExpression(binary.Right);
                        if (right.AssertType(BooleanType.Instance))
                        {
                            Evaluator.EvalAnd(left, right, binary);
                        }
                        throw RuntimeError.BinaryOperatorError(left, right, binary.Operator);
                    }
                    throw RuntimeError.BinaryOperatorErrorFirstOp(left, binary.Operator);
                case TokenType.BarBar:
                    left = EvalExpression(binary.Left);
                    if (left.AssertType(BooleanType.Instance))
                    {
                        right = EvalExpression(binary.Right);
                        if (right.AssertType(BooleanType.Instance))
                        {
                            return Evaluator.EvalOr(left, right, binary);
                        }
                        throw RuntimeError.BinaryOperatorError(left, right, binary.Operator);
                    }
                    throw RuntimeError.BinaryOperatorErrorFirstOp(left, binary.Operator);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusValue EvalIfExpression(IfExpression ifExpr)
        {
            var predicate = EvalExpression(ifExpr.Predicate);
            if (predicate.AssertType(BooleanType.Instance))
            {
                if (predicate.GetAs<bool>())
                {
                    return EvalExpression(ifExpr.TrueExpr);
                }
                else
                {
                    return EvalExpression(ifExpr.FalseExpr);
                }
            }
            else
            {
                throw new RuntimeError("Predicate in 'If' expression must be of type 'bool'", ifExpr.SourceStart);
            }
        }

        TreeWalkerEnvironment GetEnvironmentForVariable(Resolution resolution)
        {
            if (resolution is VariableResolution v)
            {
                return GetEnvironmentForVariableResolution(v);
            }
            else if (resolution is FunctionResolution)
            {
                return ModuleEnvironment;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        AilurusValue EvalVariableExpression(Variable varExpr)
        {
            var environment = GetEnvironmentForVariable(varExpr.Resolution);
            return environment.GetValue(varExpr.Resolution);
        }

        AilurusValue EvalAssignExpression(Assign assign)
        {
            var value = EvalExpression(assign.Assignment);
            var environment = GetEnvironmentForVariable(assign.Resolution);

            if (assign.PointerAssign)
            {
                environment.GetValue(assign.Resolution).GetAs<Pointer>().Assign(value);
            }
            else
            {
                environment.SetValue(assign.Resolution, value);
            }

            return value;
        }

        AilurusValue EvalSetExpression(SetExpression expr)
        {
            var callSite = EvalExpression(expr.CallSite);
            while (callSite is Pointer ptr)
            {
                if (!ptr.IsValid)
                {
                    throw new RuntimeError("Attempt to dereference pointer to invalid memory.", expr.FieldName);
                }
                callSite = ptr.Deref();
            }

            var value = EvalExpression(expr.Value);
            var structInstance = callSite.GetAs<StructInstance>();

            if (expr.PointerAssign)
            {
                structInstance[expr.FieldName.Identifier].GetAs<Pointer>().Assign(value);
            }
            else
            {
                structInstance[expr.FieldName.Identifier] = value;
            }

            return value;
        }

        #endregion
    }
}