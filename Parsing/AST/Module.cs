using System.Collections.Generic;

namespace AilurusLang.Parsing.AST
{
    public class Module : ASTNode
    {
        public List<TypeDeclaration> TypeDeclarations { get; set; } = new List<TypeDeclaration>();
        public List<FunctionDeclaration> FunctionDeclarations { get; set; } = new List<FunctionDeclaration>();
        public List<ModuleVariableDeclaration> VariableDeclarations { get; set; } = new List<ModuleVariableDeclaration>();
    }
}