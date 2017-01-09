using System;

namespace PyNet.Test
{
    public struct SimpleStruct
    {
        public int IntValue;
        public double DoubleValue;
        public string StringValue;

        public SimpleStruct(int intValue, double doubleValue, string stringValue)
        {
            IntValue = intValue;
            DoubleValue = doubleValue;
            StringValue = stringValue;
        }

        public override string ToString() => $"SimpleStruct {{ {IntValue}, {DoubleValue}, {StringValue} }}";
    }
}
