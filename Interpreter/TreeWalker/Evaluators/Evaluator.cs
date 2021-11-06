using AilurusLang.Interpreter.Runtime;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.TreeWalker.Evaluators
{
    public abstract class Evaluator
    {
        public abstract AilurusValue EvalLiteral(Literal literal);

        public abstract AilurusValue EvalPlus(AilurusValue left, AilurusValue right, Binary b);
        public abstract AilurusValue EvalMinus(AilurusValue left, AilurusValue right, Binary b);
        public abstract AilurusValue EvalBitwiseAnd(AilurusValue left, AilurusValue right, Binary b);
        public abstract AilurusValue EvalBitwiseOr(AilurusValue left, AilurusValue right, Binary b);
        public abstract AilurusValue EvalBitwiseXOr(AilurusValue left, AilurusValue right, Binary b);
        public abstract AilurusValue EvalEquality(AilurusValue left, AilurusValue right, Binary b);

        public abstract AilurusValue EvalAnd(AilurusValue left, AilurusValue right, BinaryShortCircut b);
        public abstract AilurusValue EvalOr(AilurusValue left, AilurusValue right, BinaryShortCircut b);

        public abstract AilurusValue EvalUnaryMinus(AilurusValue inner, Unary u);
        public abstract AilurusValue EvalUnaryBang(AilurusValue inner, Unary u);


    }
}