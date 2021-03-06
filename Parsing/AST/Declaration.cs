using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Scanning;
using System.Linq;

namespace AilurusLang.Parsing.AST
{
    public abstract class Declaration : ASTNode
    {
        public Declaration()
        {
            NodeType = NodeType.Declaration;
        }
        public virtual AilurusDataType DataType { get; set; }
        public bool IsExported { get; set; }
    }

    public class TypeDeclaration : Declaration
    {
        public enum ResolutionState
        {
            NotStarted,
            Added,
            Resolving,
            Resolved
        }

        public ResolutionState State { get; set; } = ResolutionState.NotStarted;

    }

    public class BaseTypeDeclaration : TypeDeclaration
    {
        public BaseTypeDeclaration()
        {
            State = ResolutionState.Resolved;
        }
        public TypeName TypeName { get; set; }
    } //Used for base types

    public class TypeAliasDeclaration : TypeDeclaration
    {
        public Token AliasName { get; set; }
        public TypeName AliasedTypeName { get; set; }
    }

    public class StructDeclaration : TypeDeclaration
    {
        public Token StructName { get; set; }
        public List<(Token, TypeName)> Fields { get; set; }
    }

    public class VariantDeclaration : TypeDeclaration
    {
        public Token VariantName { get; set; }
        public List<VariantMemberDeclaration> Members { get; set; }
    }

    public class VariantMemberDeclaration : Declaration
    {
        public Token MemberName { get; set; }
        public TypeName TypeName { get; set; }
        public ExpressionNode Index { get; set; }
    }

    public class ModuleVariableDeclaration : Declaration
    {
        public LetStatement Let { get; set; }
    }

    public class FunctionDeclaration : Declaration
    {
        public Token FunctionName { get; set; }
        public List<StatementNode> Statements { get; set; }

        // Nominal types
        public List<FunctionArgumentDeclaration> Arguments { get; set; }
        public TypeName ReturnTypeName { get; set; }

        // Resolved Type
        public override AilurusDataType DataType
        {
            get => FunctionType;
            set => FunctionType = (FunctionType)DataType;
        }
        public FunctionType FunctionType { get; set; }
        public FunctionResolution Resolution { get; set; }
        public List<VariableResolution> ArgumentResolutions { get; set; }
    }

    public class SubmoduleDeclaration : Declaration
    {
        public Token Name { get; set; }

        public string FilePath(string parentPath)
        {
            // remove the last file name. By assumption there was always a last file name
            var path = string.Join('/', parentPath.Split("/").SkipLast(1));
            return $"{path}/{Name.Identifier}";
        }
    }

    public class ImportDeclaration : Declaration
    {
        public ImportDeclaration() { }

        public ImportDeclaration(QualifiedName baseName, Token importedValue)
        {
            Name = new QualifiedName(baseName, importedValue);
        }

        public QualifiedName Name { get; set; }

        public bool IsResolved { get; set; }
    }

    public class MultiImportDeclaration : Declaration
    {
        public QualifiedName BaseName { get; set; }
        public List<Token> Names { get; set; }
    }
}