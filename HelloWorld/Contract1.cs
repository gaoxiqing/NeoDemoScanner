using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace HelloWorld
{
    public class Contract1 : SmartContract
    {
        public static string Main(string opration)
        {
            return "Hello word";
        }
    }
}
