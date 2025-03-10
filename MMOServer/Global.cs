﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace MMOServer
{
    public class Global
    {
        public static bool Shutdown = false;

        public static Dictionary<string, string> settings = new Dictionary<string, string>();


        public static uint randSeed;
        public static Object randLock = new Object();


        public static string dbUser = "root";
        public static string dbPassword = "walletDYN1042";
        public static string dbSchema = "dynammo";

        public class WebPack
        {
            public string hash;
            public string indexPage;
            public Dictionary<string, byte[]> pages;
        }

        public static Dictionary<string, WebPack> webPacks = new Dictionary<string, WebPack>();

        public class Transaction
        {
            public uint timestamp;
            public decimal amount;
            public string from;
            public string to;
        }
        public class Wallet
        {
            public string address;
            public decimal balance;
            public List<Transaction> history;
            public Dictionary<string,UTXO> utxo;
        }

        public class TX
        {
            public string hash;
            public int vout;
            public decimal amount;
            public string address;
            public bool spent;
        }

        public class UTXO
        {
            public string hash;
            public int vout;
            public decimal amount;
        }

        public static Dictionary<string, TX> txList = new Dictionary<string, TX>();
        public static Dictionary<string, Wallet> walletList = new Dictionary<string, Wallet>();


        public static int lastBlock;
        public static uint lastBlockTimestamp;

        public static bool useDatabase = false;


        public static uint RandomNum(uint x)
        {
            lock (randLock)
            {
                randSeed += x;
                return randSeed;
            }
        }

        public static void UpdateRand(uint x)
        {
            lock (randLock)
            {
                randSeed += x;
            }
        }

        public static void LoadSettings()
        {
            using (StreamReader r = new StreamReader("settings.txt"))
            {
                string json = r.ReadToEnd();
                settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }

        public static string  WebServerURL()
        {
            if (settings.ContainsKey("ListenURL"))
                return settings["ListenURL"];
            else
                return "http://*:8080/";
        }

        public static string FullNodeRPC()
        {
            return settings["FullNodeRPC"];
        }

        public static string FullNodeUser()
        {
            return settings["FullNodeUser"];
        }

        public static string FullNodePass()
        {
            return settings["FullNodePass"];
        }


        public static string WebPackReadString (byte[] data, ref int pointer)
        {
            string result = "";
            int len = WebPackReadInt(data, ref pointer);
            for (int i = 0; i < len; i++)
                result += Convert.ToChar(data[pointer + i]);
            pointer += len;
            return result;
        }

        public static int WebPackReadInt(byte[] data, ref int pointer)
        {
            int result = 0;
            result += data[pointer] << 24;
            result += data[pointer+1] << 16;
            result += data[pointer+2] << 8;
            result += data[pointer+3];

            pointer += 4;

            return result;
        }

        public static byte[] WebPackReadBinary(byte[] data, ref int pointer)
        {
            int len = WebPackReadInt(data, ref pointer);
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
                result[i] = data[pointer + i];
            pointer += len;
            return result;
        }

        public static void LoadWebPack(string assetHash, byte[] compressedPack, string indexPage)
        {

            byte[] pack = Decompress(compressedPack);

            WebPack p = new WebPack();
            p.hash = assetHash;
            p.indexPage = indexPage;
            p.pages = new Dictionary<string, byte[]>();

            int pointer = 0;
            while (pointer < pack.Length)
            {
                
                string pageName = WebPackReadString(pack, ref pointer);
                byte[] data = WebPackReadBinary(pack, ref pointer);
                p.pages.Add(pageName, data);
            }
            webPacks.Add(assetHash, p);
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        public static byte[] GetWebPackPage(string hash, string page)
        {
            byte[] result = new byte[0];

            if (webPacks.ContainsKey(hash))
                if (webPacks[hash].pages.ContainsKey(page))
                    result = webPacks[hash].pages[page];

            return result;
        }


        public static void saveTx(string txID, int n, decimal amount, string address)
        {
            amount *= 100000000m;

            TX tx = new TX();
            tx.hash = txID;
            tx.vout = n;
            tx.amount = amount;
            tx.address = address;
            tx.spent = false;

            string key = txID + n.ToString();

            lock(txList)
                txList.Add(key, tx);


            if (address == "dy1q752fqtt6w02jc5sp8hfn0yafvrt4sx3y0cgm76")
                Console.WriteLine("");

            lock (walletList)
            {
                Wallet w;
                if (walletList.ContainsKey(address))
                {
                    w = walletList[address];
                }
                else
                {
                    w = new Wallet();
                    w.address = address;
                    w.balance = 0;
                    w.history = new List<Transaction>();
                    w.utxo = new Dictionary<string, UTXO>();
                }

                UTXO u = new UTXO();
                u.hash = txID;
                u.vout = n;
                u.amount = amount;
                w.utxo.Add(key, u);
                walletList[address] = w;
            }

        }

        public static void spendTransaction(string txid, int vout)
        {
            string key = txid + vout.ToString();
            lock (txList)
            {
                if (txList.ContainsKey(key))
                {
                    TX tx = txList[key];
                    tx.spent = true;
                    txList[key] = tx;

                    lock (walletList)
                    {
                        if (walletList[tx.address].utxo.ContainsKey(key))
                        {
                            walletList[tx.address].utxo.Remove(key);
                            updateWalletBalance(tx.address, -tx.amount);
                        }
                        else
                            Console.WriteLine("Error: didnt find utxo in wallet " + txid + "  " + vout);
                    }
                }
                else
                    Console.WriteLine("Error: didnt find utxo " + txid + "  " + vout);
            }

        }

        public static void updateWalletBalance(string address, decimal amount)
        {

            if (address == "dy1q752fqtt6w02jc5sp8hfn0yafvrt4sx3y0cgm76")
                Console.Write("");

            lock (walletList)
            {
                if (walletList.ContainsKey(address))
                {
                    Wallet w = walletList[address];
                    w.balance += amount;
                    walletList[address] = w;
                }
                else
                {
                    Wallet w = new Wallet();
                    w.address = address;
                    w.balance = amount;
                    w.history = new List<Transaction>();
                    w.utxo = new Dictionary<string, UTXO>();
                    walletList.Add(address, w);
                }
            }
        }

        public static void addWalletHistory(string from, string to, uint timestamp, decimal amount)
        {
            Wallet w;
            Transaction t;

            if (to == from)     //exclude change  (assumes user doesnt send coins to themselves)
                return;

            lock (walletList)
            {
                if (from != "coinbase")
                {
                    w = walletList[from];
                    t = new Transaction();
                    t.timestamp = timestamp;
                    t.amount = -amount;
                    t.from = from;
                    t.to = to;
                    w.history.Add(t);
                    walletList[from] = w;
                }

                w = walletList[to];
                t = new Transaction();
                t.timestamp = timestamp;
                t.amount = amount;
                t.from = from;
                t.to = to;
                w.history.Add(t);
                walletList[to] = w;
            }
        }


        public static Boolean ValidateSignature()
        {
            string address = "dy1qzvx3yfrucqa2ntsw8e7dyzv6u6dl2c2wjvx5jy";
            string signature = "Jyv5LFiSCHKmY+DRAK324jlw4PguGdwMG6jdMDO6HQVRUDf4bGMJW+wWFtHoFPMU54lkHISn+ZCQ0Fo3f/Ie6sk=";
            string message = "123456";
            string sArgs = address + " " + signature + " " + message;

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"node.exe",
                    Arguments = "verify_address.js " + sArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = @"C:\Users\user\source\repos\MMOServer\MMOServer"
                }
            };
            p.Start();
            while (!p.HasExited)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine(p.StandardError.ReadToEnd());
            Console.WriteLine(p.StandardOutput.ReadToEnd());

            return false;
        }


    }
}
