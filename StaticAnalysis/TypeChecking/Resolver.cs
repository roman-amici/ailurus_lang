using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class Resolver
    {
        public static StandardScope StandardScope { get; set; } = new StandardScope();

        public List<BlockScope> Scopes { get; set; } = new List<BlockScope>();
        public ModuleScope ModuleScope { get; set; } = new ModuleScope();

        public void ResolveStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                ResolveStatement(statement);
            }
        }

        TypeDeclaration ResolveExpression(ExpressionNode expr)
        {
            throw new NotImplementedException();
        }

        public void ResolveLet(LetStatement let)
        {
            if (VariableExistsInCurrentScope(let.Name))
            {
                Error($"Variable with name {let.Name.Lexeme} already exists in this scope.");
                return;
            }

            AilurusDataType assertedType = null;
            AilurusDataType initializerType = null;
            if (let.AssertedType != null)
            {
                assertedType = ResolveTypeName(let.AssertedType);
            }

            if (let.Initializer != null)
            {
                initializerType = ResolveExpression(let.Initializer);
                if (initializerType == null)
                {
                    // TODO: Add error type?
                }
            }

            if (assertedType == null)
            {
                // Type inference
                assertedType = initializerType;
            }

            if (!CanAssignTo(assertedType, initializerType))
            {
                Error($"Can't assing type {initializerType} to type {assertedType}");
                return;
            }

            AddVariableToCurrentScope(let.Name, assertedType);

        }

        public void ResolveStatement(StatementNode statement)
        {
            switch (statement.StmtType)
            {
                case StatementType.Expression:
                    ResolveExpression(((ExpressionStatement)statement).Expr);
                    break;
                case StatementType.Let:
                    ResolveLet((LetStatement)statement);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}