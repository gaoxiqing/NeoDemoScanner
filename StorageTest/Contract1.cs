using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace StorageTest
{
    public class StorageContract1 : SmartContract
    {
        public static string Main(string method, params object[] args)
        {
            //Storage.Put("totalSupply", "Hello World");
            //var val1 = Storage.Get("totalSupply");

           

            if (method == "totalSupply") return totalSupply();
            
            return "";
        }

        //发行总量
        public static string totalSupply()
        {

            //var total = Storage.Get(Storage.CurrentContext, "totalSupply");
            //if (total != null && total.Length > 0)
            //{
            //    return total.AsString();
            //}
            //else
            //{
            //    return "haha";
            //}
            return "haha";
        }

        
    }
}
