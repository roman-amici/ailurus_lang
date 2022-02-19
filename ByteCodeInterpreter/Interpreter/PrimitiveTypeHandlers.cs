
using System;
using AilurusLang.ByteCodeInterpreter.Compiler;

namespace AilurusLang.ByteCodeInterpreter.Interpreter
{
    public static class PrimitiveTypeHandlers
    {
        public static AilurusValue NumericCast(dynamic value, NumericType type)
        {
            return type switch
            {
                NumericType.U64 => (ulong)value,
                NumericType.U32 => (uint)value,
                NumericType.U16 => (ushort)value,
                NumericType.U8 => (byte)value,
                NumericType.I64 => (long)value,
                NumericType.I32 => (int)value,
                NumericType.I16 => (short)value,
                NumericType.I8 => (byte)value,
                NumericType.F64 => (double)value,
                NumericType.F32 => (float)value,
                _ => throw new ArgumentException("Unknown numeric type."),
            };
        }

        public static AilurusValue NegateOp(Negate negOp, AilurusValue value)
        {
            var neg = -value.GetDynamic();
            return NumericCast(neg, negOp.Type);
        }

        public static AilurusValue AddOp(Add op, AilurusValue l, AilurusValue r)
        {
            var lDyn = l.GetDynamic();
            var rDyn = r.GetDynamic();
            var ret = lDyn + rDyn;
            return NumericCast(ret, op.ResultType);
        }

        public static AilurusValue MulOp(Multiply op, AilurusValue l, AilurusValue r)
        {
            var lDyn = l.GetDynamic();
            var rDyn = r.GetDynamic();
            var ret = lDyn * rDyn;
            return NumericCast(ret, op.ResultType);
        }

        public static AilurusValue DivOp(Divide op, AilurusValue l, AilurusValue r)
        {
            var lDyn = l.GetDynamic();
            var rDyn = r.GetDynamic();
            var ret = lDyn / rDyn;
            return NumericCast(ret, op.ResultType);
        }

        public static AilurusValue SubOp(Subtract op, AilurusValue l, AilurusValue r)
        {
            var lDyn = l.GetDynamic();
            var rDyn = r.GetDynamic();
            var ret = lDyn - rDyn;
            return NumericCast(ret, op.ResultType);
        }

    }
}