using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeoDemoScanner
{
    class Program
    {
        static Scanner scanner;
        static WebClient wc = new WebClient();
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()+ "\\..\\..\\..\\")
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);


            IConfigurationRoot configuration = builder.Build();


            scanner = new Scanner(configuration);
            scanner.Begin();
            Console.WriteLine("BeginScan!");
            var strurl = configuration.GetSection("utxorpcurl").Value;
            var rpcurl = configuration.GetSection("rpcurl").Value;
            while (true)
            {
                Console.Write("cmd>");
                string cmdline = Console.ReadLine();
                var cmdArgs = cmdline.Split(" ");
                cmdArgs = cmdArgs.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (cmdArgs.Length > 0)
                {
                    var cmd = cmdArgs[0].Replace(" ", "");
                    if (cmd == "") continue;
                    switch (cmd)
                    {
                        case "exit":
                            scanner.Exit();
                            while (scanner.FullExit == false)
                            {
                                System.Threading.Thread.Sleep(1);
                            }
                            return;
                        case "help":
                        case "?":
                            ShowCmdHelp();
                            break;
                        case "sendfrom":
                            if (cmdArgs.Length >= 5)
                            {
                                
                                //if (strurl.Last() != '/') strurl += "/";
                                var assetId = cmdArgs[1];
                                var fromadd = cmdArgs[2];
                                var toadd = cmdArgs[3];
                                var value = cmdArgs[4];
                                var change_address = string.Empty;
                                if(cmdArgs.Length >= 6)
                                {
                                    change_address = cmdArgs[5];
                                }
                                SendtoAddress(strurl, rpcurl,assetId, fromadd, toadd, value, change_address);                                
                            }
                            break;
                        case "createcontract":
                            if (cmdArgs.Length >= 4)
                            {
                                var assetId = cmdArgs[1];
                                var wif = cmdArgs[2];
                                var avmPath = cmdArgs[3];
                                var change_address = string.Empty;
                                if (cmdArgs.Length >= 5)
                                {
                                    change_address = cmdArgs[4];
                                }
                                try
                                {
                                    CreateContract(avmPath, strurl, rpcurl, assetId, wif, change_address);
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            break;
                        case "getstorage":
                            if(cmdArgs.Length >= 3)
                            {
                                var scriptaddress = cmdArgs[1];
                                var key = cmdArgs[2];
                                getstorage(rpcurl, key, scriptaddress);
                            }
                            break;
                        case "invoke":
                            if (cmdArgs.Length >= 2)
                            {
                                var scripthash = cmdArgs[1];
                                invoke(rpcurl, scripthash);
                            }
                            break;
                        case "invokescript":
                            if (cmdArgs.Length >= 2)
                            {
                                var avmPath = cmdArgs[1];
                                invokescript(avmPath,rpcurl);
                            }
                            break;
                        case "nep52":
                            if (cmdArgs.Length >= 3)
                            {
                                var avmPath = cmdArgs[1];
                                var fromadd = cmdArgs[2];
                                nep52(rpcurl,avmPath, fromadd);
                            }
                            break;
                        case "nep53":
                            if (cmdArgs.Length >= 5)
                            {
                                var avmPath = cmdArgs[1];
                                var assetId = cmdArgs[2];
                                var wif = cmdArgs[3];
                                var toadd = cmdArgs[4];
                                try
                                {
                                    nep53(strurl, rpcurl, avmPath, wif, toadd, assetId);
                                }
                                catch(Exception ex)
                                {

                                }
                            }
                            break;
                        case "del":
                            if (cmdArgs.Length >= 3)
                            {
                                var avmPath = cmdArgs[1];
                                var assetId = cmdArgs[2];
                                var wif = cmdArgs[3];
                                var toadd = cmdArgs[4];
                                DelContract(avmPath, strurl,rpcurl,  assetId, wif, toadd);
                            }
                            break;
                        //case "state":
                        //    ShowState();
                        //    break;
                        //case "save":
                        //    scanner.Save();
                        //break;
                        default:
                            Console.WriteLine("unknown cmd,type help to get more.");
                            break;
                    }
                }

            }
        }

        static void ShowState()
        {
            Console.WriteLine("sync height=" + scanner.processedBlock + "  remote height=" + scanner.remoteBlockHeight);
        }
        static void ShowCmdHelp()
        {
            Console.WriteLine("neo_scanner 0.01");
            Console.WriteLine("help -> print helpinfo");
            Console.WriteLine("exit -> exit program");
            //Console.WriteLine("state -> show state");
            //Console.WriteLine("save -> save a state now.");

        }
        static async void getstorage(string rpcurl,string key, string scriptaddress)
        {
            //string scriptaddress = "0x2e88caf10afe621e90142357236834e010b16df2";
            //string key = "9b87a694f0a282b2b5979e4138944b6805350c6fa3380132b21a2f12f9c2f4b6";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(key);
            var hexKey = ThinNeo.Helper.Bytes2HexString(buffer);
            var rev = ThinNeo.Helper.HexString2Bytes(hexKey).Reverse().ToArray();
            var revkey = ThinNeo.Helper.Bytes2HexString(rev);

            var url = Helper.MakeRpcUrl(rpcurl, "getstorage", new MyJson.JsonNode_ValueString(scriptaddress), new MyJson.JsonNode_ValueString(hexKey));

            string result = await Helper.HttpGet(url);
            Console.WriteLine("得到的结果是：" + result);
        }
        static async void nep52(string rpcurl, string avmPath,string address)
        {
            //从文件中读取合约脚本
            byte[] script = System.IO.File.ReadAllBytes(avmPath); //这里填你的合约所在地址
            //string nnc = "0x460b89c3c8d31528b9e90d9baff9de31f6ddb6c6".Replace("0x", "");
            string scripthash = null;
            using (var sb = new ThinNeo.ScriptBuilder())
            {

                sb.EmitParamJson(new MyJson.JsonNode_Array());//参数倒序入
                sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)name"));//参数倒序入
                //ThinNeo.Hash160 shash = new ThinNeo.Hash160(nnc);
                var shash = ThinNeo.Helper.GetScriptHashFromScript(script);
                sb.EmitAppCall(shash);//nep5脚本                

                sb.EmitParamJson(new MyJson.JsonNode_Array());
                sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)symbol"));
                sb.EmitAppCall(shash);

                sb.EmitParamJson(new MyJson.JsonNode_Array());
                sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)decimals"));
                sb.EmitAppCall(shash);

                //sb.EmitParamJson(new MyJson.JsonNode_Array());
                //sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)totalSupply"));
                //sb.EmitAppCall(shash);

                //var array = new MyJson.JsonNode_Array();
                //array.AddArrayValue("(addr)" + address);//from
                //sb.EmitParamJson(array);//参数倒序入
                //sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)balanceOf"));//参数倒序入
                //sb.EmitAppCall(shash);

                var data = sb.ToArray();
                scripthash = ThinNeo.Helper.Bytes2HexString(data);
            }

            //var url = Helper.MakeRpcUrl(api, "invokescript", new MyJson.JsonNode_ValueString(script));
            //string result = await Helper.HttpGet(url);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(rpcurl, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scripthash));
            var result = await Helper.HttpPost(url, postdata);


            Console.WriteLine("得到的结果是：" + result);
            var continer = MyJson.Parse(result).AsDict();
            if (continer.ContainsKey("result"))
            {
                var json = continer["result"].AsDict();
                printByteArray(json);
            }
        }
        static async void nep53(string strurl, string rpcurl, string avmPath,string wif,string toaddr,string asset)
        { 
            //从文件中读取合约脚本
            byte[] script = System.IO.File.ReadAllBytes(avmPath); //这里填你的合约所在地址
            var nnc = ThinNeo.Helper.GetScriptHashFromScript(script);

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            //获取地址的资产列表
            Dictionary<string, List<Utxo>> dir = await Helper.GetBalanceByAddress(strurl, address);


            string targeraddr = address;  //Transfer it to yourself. 
            ThinNeo.Transaction tran = Helper.makeTran(dir[asset], targeraddr, new ThinNeo.Hash256(asset), decimal.Zero);
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder();

            var scriptaddress = new ThinNeo.Hash160(nnc);
            //Parameter inversion 
            MyJson.JsonNode_Array JAParams = new MyJson.JsonNode_Array();
            JAParams.Add(new MyJson.JsonNode_ValueString("(address)" + address));
            JAParams.Add(new MyJson.JsonNode_ValueString("(address)" + toaddr));
            JAParams.Add(new MyJson.JsonNode_ValueString("(integer)" + 1));
            sb.EmitParamJson(JAParams);//Parameter list 
            sb.EmitPushString("transfer");//Method
            sb.EmitAppCall(scriptaddress);  //Asset contract 

            ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
            extdata.script = sb.ToArray();
            extdata.gas = 1;
            tran.extdata = extdata;

            byte[] msg = tran.GetMessage();
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);


            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(rpcurl, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            var response = await Helper.HttpPost(url, postdata);

            MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(response);
            if (resJO.ContainsKey("result"))
            {
                Console.WriteLine(resJO["result"].ToString());
            }

        }
        static async void invoke(string rpcurl, string scripthash)
        {
            var url = Helper.MakeInvokeRpcUrl(rpcurl, "invoke", new MyJson.JsonNode_ValueString(scripthash));
            string result = await Helper.HttpGet(url);

            Console.WriteLine("得到的结果是：" + result);
            var json = MyJson.Parse(result).AsDict()["result"].AsDict();
            printByteArray(json);
        }
        
        static async void invokescript(string avmPath, string rpcurl)
        {
            
            //从文件中读取合约脚本
            byte[] script = System.IO.File.ReadAllBytes(avmPath); //这里填你的合约所在地址
            
            byte[] parameter__list = new byte[0];
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                //倒叙插入数据
                //sb.EmitParamJson(new MyJson.JsonNode_Array());
                sb.EmitParamJson(new MyJson.JsonNode_Array());
                sb.EmitParamJson(new MyJson.JsonNode_ValueString("(str)totalSupply"));
                var nnc = ThinNeo.Helper.GetScriptHashFromScript(script);
                var scriptaddress = new ThinNeo.Hash160(nnc);
                sb.EmitAppCall(scriptaddress);
                //sb.EmitPushBytes(script);
                //ThinNeo.Hash160 shash = new ThinNeo.Hash160(scripthash.Replace("0x", ""));
                //sb.EmitAppCall(script);//nep5脚本

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
                //用ivokescript试运行并得到消耗

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(rpcurl, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
                var result = await Helper.HttpPost(url, postdata);

                Console.WriteLine("得到的结果是：" + result);
                var json = MyJson.Parse(result).AsDict()["result"].AsDict();
                printByteArray(json);
            }
            
        }
        static void  printByteArray(MyJson.JsonNode_Object json)
        {
            if (json.ContainsKey("stack") && json["stack"].AsList().Count > 0)
            {
                int i = 0;
                foreach (var item in json["stack"].AsList())
                {
                    i++;
                    if (item.AsDict()["value"] != null)
                    {
                        if (item.AsDict()["value"].type == MyJson.jsontype.Value_String)
                        {
                            var rev = ThinNeo.Helper.HexString2Bytes(item.AsDict()["value"].ToString());
                            Console.WriteLine($"stack {i}：" + System.Text.Encoding.UTF8.GetString(rev));

                        }
                        else if(item.AsDict()["value"].type == MyJson.jsontype.Value_Number)
                        {
                            var rev = ThinNeo.Helper.HexString2Bytes(item.AsDict()["value"].ToString());

                            Console.WriteLine($"stack {i}：" + BitConverter.ToInt64(rev));
                        }
                    }
                }
            }
        }
        static async void CreateContract(string avmPath,string strurl, string rpcurl, string assetId, string wif, string change_address)
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Dictionary<string, List<Utxo>> dir = await Helper.GetBalanceByAddress(strurl, address);

            //从文件中读取合约脚本
            byte[] script = System.IO.File.ReadAllBytes(avmPath); //这里填你的合约所在地址
            //Console.WriteLine("合约脚本:"+ThinNeo.Helper.Bytes2HexString(script));
            Console.WriteLine("合约脚本hash："+ ThinNeo.Helper.GetScriptHashFromScript(script));
            byte[] parameter__list = ThinNeo.Helper.HexString2Bytes("0710");  //这里填合约入参  例：0610代表（string，[]）
            byte[] return_type = ThinNeo.Helper.HexString2Bytes("05");  //这里填合约的出参
            int need_storage = 1;
            int need_nep4 = 0;
            int need_canCharge = 4;
            string name = "qwe12312";
            string version = "1.0";
            string auther = "NEL";
            string email = "0";
            string description = "0";
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                //倒叙插入数据
                sb.EmitPushString(description);
                sb.EmitPushString(email);
                sb.EmitPushString(auther);
                sb.EmitPushString(version);
                sb.EmitPushString(name);
                //sb.EmitPushNumber(need_storage | need_nep4 | need_canCharge);
                //sb.EmitPushBytes(return_type);
                //sb.EmitPushBytes(parameter__list);
                sb.EmitPushBytes(script);
                sb.EmitSysCall("Neo.Contract.Create");

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
                //用ivokescript试运行并得到消耗

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(rpcurl, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
                var result = await Helper.HttpPost(url, postdata);
                //string result = http.Post(api, "invokescript", new MyJson.JsonNode_Array() { new MyJson.JsonNode_ValueString(scriptPublish) },Encoding.UTF8);
                var resultObj = MyJson.Parse(result) as MyJson.JsonNode_Object;                
                var consume = resultObj["result"].AsDict()["gas_consumed"].ToString();
                decimal gas_consumed = decimal.Parse(consume);
                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //Console.WriteLine(ThinNeo.Helper.Bytes2HexString(extdata.script));
                extdata.gas = (gas_consumed >= 10) ?Math.Ceiling(gas_consumed - 10): gas_consumed;

                //拼装交易体
                ThinNeo.Transaction tran = makeTran(dir, null, new ThinNeo.Hash256(assetId), extdata.gas, change_address);
                tran.version = 1;
                tran.extdata = extdata;
                tran.type = ThinNeo.TransactionType.InvocationTransaction;
                byte[] msg = tran.GetMessage();
                byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                tran.AddWitness(signdata, pubkey, address);
                string txid = tran.GetHash().ToString();
                byte[] data = tran.GetRawData();
                string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                //Console.WriteLine("scripthash:"+scripthash);

                url = Helper.MakeRpcUrlPost(rpcurl, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
                result = await Helper.HttpPost(url, postdata);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }

        }
        static async void DelContract(string avmPath, string strurl, string rpcurl, string assetId, string wif, string change_address)
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Dictionary<string, List<Utxo>> dir = await Helper.GetBalanceByAddress(strurl, address);

            //从文件中读取合约脚本
            byte[] script = System.IO.File.ReadAllBytes(avmPath); //这里填你的合约所在地址
            //Console.WriteLine("合约脚本:"+ThinNeo.Helper.Bytes2HexString(script));
            Console.WriteLine("合约脚本hash：" + ThinNeo.Helper.GetScriptHashFromScript(script));
           
            
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                //倒叙插入数据
                sb.EmitPushBytes(script);
                sb.EmitSysCall("Neo.Contract.Destroy");

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
                //用ivokescript试运行并得到消耗

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(rpcurl, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
                var result = await Helper.HttpPost(url, postdata);
                //string result = http.Post(api, "invokescript", new MyJson.JsonNode_Array() { new MyJson.JsonNode_ValueString(scriptPublish) },Encoding.UTF8);
                var resultObj = MyJson.Parse(result) as MyJson.JsonNode_Object;
                var consume = resultObj["result"].AsDict()["gas_consumed"].ToString();
                decimal gas_consumed = decimal.Parse(consume);
                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //Console.WriteLine(ThinNeo.Helper.Bytes2HexString(extdata.script));
                extdata.gas = (gas_consumed >= 10) ? Math.Ceiling(gas_consumed - 10) : gas_consumed;

                //拼装交易体
                ThinNeo.Transaction tran = makeTran(dir, null, new ThinNeo.Hash256(assetId), extdata.gas, change_address);
                tran.version = 1;
                tran.extdata = extdata;
                tran.type = ThinNeo.TransactionType.InvocationTransaction;
                byte[] msg = tran.GetMessage();
                byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                tran.AddWitness(signdata, pubkey, address);
                string txid = tran.GetHash().ToString();
                byte[] data = tran.GetRawData();
                string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                //Console.WriteLine("scripthash:"+scripthash);

                url = Helper.MakeRpcUrlPost(rpcurl, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
                result = await Helper.HttpPost(url, postdata);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }

        }
        static async void SendtoAddress(string strurl, string rpcurl,string assetId, string wif1, string toadd, string value, string change_address)
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif1);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string fromadd = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);

            Dictionary<string, List<Utxo>> dir = null;
            try
            {
                dir = await Helper.GetBalanceByAddress(strurl, fromadd);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            //拼装交易体
            string[] targetAddrs = new string[1] { toadd };
            decimal sendCount = decimal.Parse(value);
            try
            {
                ThinNeo.Transaction tran = makeTran(dir, targetAddrs, new ThinNeo.Hash256(assetId), sendCount, change_address);
                tran.version = 0;
                tran.type = ThinNeo.TransactionType.ContractTransaction;
                byte[] msg = tran.GetMessage();
                string msgstr = ThinNeo.Helper.Bytes2HexString(msg);
                byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                tran.AddWitness(signdata, pubkey, fromadd);
                string txid = tran.GetHash().ToString();
                byte[] data = tran.GetRawData();
                string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(rpcurl, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            
                var result = await Helper.HttpPost(url, postdata);
                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.Write("cmd>");
        }
        //拼交易体
        static ThinNeo.Transaction makeTran(Dictionary<string, List<Utxo>> dir_utxos, string[] targetaddrs, ThinNeo.Hash256 assetid, decimal sendcount,string change_address)
        {
            if (!dir_utxos.ContainsKey(assetid.ToString()))
                throw new Exception("no enough money.");

            List<Utxo> utxos = dir_utxos[assetid.ToString()];

            var tran = new ThinNeo.Transaction();
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            tran.version = 0;//0 or 1
            tran.extdata = null;

            tran.attributes = new ThinNeo.Attribute[0];
            utxos.Sort((a, b) =>
            {
                if (a.value > b.value)
                    return 1;
                else if (a.value < b.value)
                    return -1;
                else
                    return 0;
            });

            decimal count = decimal.Zero;
            string scraddr = "";
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            for (var i = 0; i < utxos.Count; i++)
            {
                ThinNeo.TransactionInput input = new ThinNeo.TransactionInput();
                input.hash = utxos[i].txid;
                input.index = (ushort)utxos[i].n;
                list_inputs.Add(input);
                count += utxos[i].value;
                scraddr = utxos[i].addr;
                if (count >= (sendcount))
                {
                    break;
                }
            }

            tran.inputs = list_inputs.ToArray();

            if (count >= sendcount)//输入大于等于输出（余额大于等于转账金额）
            {
                List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
                //输出（转账金额）
                if (sendcount > decimal.Zero && targetaddrs != null && targetaddrs.Length > 0)
                {
                    foreach (string targetaddr in targetaddrs)
                    {
                        ThinNeo.TransactionOutput output = new ThinNeo.TransactionOutput();
                        output.assetId = assetid;
                        output.value = sendcount;
                        output.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetaddr);
                        list_outputs.Add(output);
                    }
                }

                //找零
                var change = count - sendcount;
                if (change > decimal.Zero)
                {
                    ThinNeo.TransactionOutput outputchange = new ThinNeo.TransactionOutput();
                    outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(!string.IsNullOrEmpty(change_address) ? change_address : scraddr);
                    outputchange.value = change;
                    outputchange.assetId = assetid;
                    list_outputs.Add(outputchange);
                }
                tran.outputs = list_outputs.ToArray();
            }
            else
            {
                throw new Exception("no enough money.");
            }
            return tran;
        }
    }
}
