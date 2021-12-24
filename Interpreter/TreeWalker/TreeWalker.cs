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

        public void EvalModule(Module module)
        {
            foreach (var variable in module.VariableDeclarations)
            {
                EvalLetStatement(variable.Let);
            }

            FunctionDeclaration mainFunction = null;
            foreach (var function in module.FunctionDeclarations)
            {
                EvalFunctionDeclaration(function);
                if (function.FunctionName.Lexeme == "main")
                {
                    mainFunction = function;
                }
            }

            if (mainFunction == null)
            {
                throw new RuntimeError("Unable to execute module, no 'main' function was found.", module.SourceStart);
            }
            else
            {
                // TODO: Figure out a better way to do this.
                var mainCall = new Call()
                {
                    Callee = new Variable()
                    {
                        Resolution = mainFunction.Resolution
                    },
                    ArgumentList = new List<ExpressionNode>(),
                };
                EvalCallExpression(mainCall);
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
                default:
                    throw new NotImplementedException();

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
                        var env = GetEnvironmentForVariable(variable.Resolution, variable.Name);
                        env.SetValue(variable.Resolution, value);
                    }
                }
                else if (forEach.AssignmentTarget is Variable variable)
                {
                    ;
                    var env = GetEnvironmentForVariable(variable.Resolution, variable.Name);
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
            var pred = EvalExpression(ifStatement.Predicate);
            if (pred.GetAs<bool>())
            {
                EvalBlockStatement(ifStatement.ThenStatements);
            }
            else if (ifStatement.ElseStatements != null)
            {
                EvalBlockStatement(ifStatement.ElseStatements);
            }
        }

        void EvalPrintStatement(PrintStatement print)
        {
            var value = EvalExpression(print.Expr);
            Console.WriteLine($"{value}");
        }

        void EvalLetStatement(LetStatement stmt)
        {
            AilurusValue initializer = null;
            if (stmt.Initializer != null)
            {
                initializer = EvalExpression(stmt.Initializer);
            }

            if (stmt.AssignmentTarget is TupleExpression tupleVariables)
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
            else if (stmt.AssignmentTarget is Variable variable)
            {
                var resolution = variable.Resolution as VariableResolution;
                var env = GetEnvironmentForVariableResolution(resolution);
                env.SetValue(resolution, initializer);
            }
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
                _ => throw new NotImplementedException(),
            };
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

        AilurusValue EvalArraySet(ArraySetExpression expr)
        {
            var value = EvalExpression(expr.Value);
            var array = EvalExpression(expr.ArrayIndex.CallSite).GetAs<IArrayInstanceLike>();
            var index = EvalExpression(expr.ArrayIndex.IndexExpression).GetAs<int>();

            if (index >= array.Count)
            {
                throw new RuntimeError("Array index out of bounds", expr.SourceStart);
            }

            if (!array.AccessIsValid(index))
            {
                throw new RuntimeError("Attempt to access array which is not initialized.", expr.ArrayIndex.CallSite.SourceStart);
            }

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
                var environment = GetEnvironmentForVariable(v.Resolution, v.Name);
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

        TreeWalkerEnvironment GetEnvironmentForVariable(Resolution resolution, Token Name)
        {
            if (resolution is VariableResolution v)
            {
                if (!v.IsInitialized)
                {
                    // TODO: Reject statically
                    throw new RuntimeError("Referenced an uninitialized variable", Name);
                }
                return GetEnvironmentForVariableResolution(v);
            }
            else if (resolution is FunctionResolution f)
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
            var environment = GetEnvironmentForVariable(varExpr.Resolution, varExpr.Name);
            return environment.GetValue(varExpr.Resolution);
        }

        AilurusValue EvalAssignExpression(Assign assign)
        {
            var value = EvalExpression(assign.Assignment);
            var environment = GetEnvironmentForVariable(assign.Resolution, assign.Name);

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