using System.Collections.Generic;

namespace AilurusLang.Parsing.AST
{
    public class Module : ASTNode
    {
        public List<SubmoduleDeclaration> SubmoduleDeclarations { get; set; } = new List<SubmoduleDeclaration>();
        public List<ImportDeclaration> ImportDeclarations { get; set; } = new List<ImportDeclaration>();
        public List<TypeDeclaration> TypeDeclarations { get; set; } = new List<TypeDeclaration>();
        public List<FunctionDeclaration> FunctionDeclarations { get; set; } = new List<FunctionDeclaration>();
        public List<ModuleVariableDeclaration> VariableDeclarations { get; set; } = new List<ModuleVariableDeclaration>();

        // Computed Properties
        public List<string> Path { get; set; }
        public string ModuleName => Path[^1];
        public List<Module> Submodules { get; set; }
    }


}