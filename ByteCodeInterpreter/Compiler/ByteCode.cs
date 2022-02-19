namespace AilurusLang.ByteCodeInterpreter.Compiler
{
    public enum NumericType
    {
        U64 = 0,
        U32,
        U16,
        U8,
        I64,
        I32,
        I16,
        I8,
        F64,
        F32
    }

    public abstract class ByteCodeOp
    {

    }

    public class ReturnOp : ByteCodeOp
    {

    }

    public class ConstantOp : ByteCodeOp
    {
        public int ConstantPointer { get; set; }
        public short Size { get; set; }
    }

    public class Negate : ByteCodeOp
    {
        public NumericType Type { get; set; }
    }

    public class Add : ByteCodeOp
    {
        public NumericType LeftType { get; set; }
        public NumericType RightType { get; set; }
        public NumericType ResultType { get; set; }
    }

    public class Multiply : ByteCodeOp
    {
        public NumericType LeftType { get; set; }
        public NumericType RightType { get; set; }
        public NumericType ResultType { get; set; }
    }

    public class Divide : ByteCodeOp
    {
        public NumericType LeftType { get; set; }
        public NumericType RightType { get; set; }
        public NumericType ResultType { get; set; }
    }

    public class Subtract : ByteCodeOp
    {
        public NumericType LeftType { get; set; }
        public NumericType RightType { get; set; }
        public NumericType ResultType { get; set; }
    }
}

