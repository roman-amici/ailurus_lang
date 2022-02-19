using System;
using System.Collections.Generic;
using AilurusLang.ByteCodeInterpreter.Compiler;

namespace AilurusLang.ByteCodeInterpreter.Interpreter
{

    public class InterpreterManaged
    {
        Dictionary<int, AilurusValue> StaticData { get; set; }
        List<ByteCodeOp> Ops { get; set; }

        List<AilurusValue> Stack { get; set; } = new List<AilurusValue>();
        Dictionary<int, AilurusValue> Heap { get; set; } = new Dictionary<int, AilurusValue>();

        int IP { get; set; } = 0;

        public InterpreterManaged(Dictionary<int, AilurusValue> staticData, List<ByteCodeOp> ops)
        {
            StaticData = staticData;
            Ops = ops;
        }

        ByteCodeOp ReadOp()
        {
            var value = Ops[IP];
            IP++;
            return value;
        }

        void Push(AilurusValue value)
        {
            Stack.Add(value);
        }

        AilurusValue Pop()
        {
            var value = Stack[^1];
            Stack.RemoveAt(Stack.Count - 1);
            return value;
        }

        public int Run()
        {
            while (true)
            {
                var instruction = ReadOp();
                AilurusValue l, r, val;
                switch (instruction)
                {
                    case ReturnOp ret:
                        return 0;
                    case ConstantOp constOp:
                        var constant = StaticData[constOp.ConstantPointer];
                        Push(constant);
                        Console.WriteLine(constant);
                        break;
                    case Negate negOp:
                        Push(PrimitiveTypeHandlers.NegateOp(negOp, Pop()));
                        break;
                    case Add addOp:
                        r = Pop();
                        l = Pop();
                        Push(PrimitiveTypeHandlers.AddOp(addOp, l, r));
                        break;
                    case Multiply mulOp:
                        r = Pop();
                        l = Pop();
                        Push(PrimitiveTypeHandlers.MulOp(mulOp, l, r));
                        break;
                    case Divide divOp:
                        r = Pop();
                        l = Pop();
                        Push(PrimitiveTypeHandlers.DivOp(divOp, l, r));
                        break;
                    case Subtract subOp:
                        r = Pop();
                        l = Pop();
                        Push(PrimitiveTypeHandlers.SubOp(subOp, l, r));
                        break;
                }
            }
        }


    }

}