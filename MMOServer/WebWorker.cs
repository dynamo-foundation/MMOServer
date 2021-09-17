using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MMOServer
{

    public class WebWorker
    {

        public HttpListenerContext context;
        static readonly HttpClient client = new HttpClient();

        static HttpWebRequest webRequest;


        public void run()
        {
            try
            {
                HttpListenerRequest request = context.Request;

                StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string text = reader.ReadToEnd();

                string[] path = request.RawUrl.Substring(1).Split("/");
                Dictionary<string, string> args = ParseArgs(request.Url.Query);

                Console.WriteLine(request.RawUrl);


                string result = "Internal error";

                if (path[0].StartsWith("get_land"))
                    result = GetLand(args);

                else if (path[0].StartsWith("get_player_id"))
                    result = GetPlayerID(args).ToString();

                else if (path[0].StartsWith("register_wallet"))
                    result = RegisterWallet(args);

                else if (path[0].StartsWith("register_payment"))
                    result = RegisterPayment(args);

                else if (path[0].StartsWith("redeem_payment"))
                    result = RedeemPayment(args);

                else if (path[0].StartsWith("rename_land"))
                    result = RenameLand(args);

                Console.WriteLine("Result: " + result);

                byte[] binaryData = Encoding.ASCII.GetBytes(result);

                HttpListenerResponse response = context.Response;

                System.IO.Stream output = response.OutputStream;
                output.Write(binaryData, 0, binaryData.Length);
                output.Close();

                uint sum = 0;
                for (int i = 0; i < binaryData.Length; i++)
                    sum += binaryData[i];

                Global.UpdateRand(sum);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in worker thread:" + e.Message);
                Console.WriteLine(e.StackTrace);

            }
        }


        public Dictionary<string,string> ParseArgs (string data)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            string[] arguments = data.Substring(1).Split('&');
            for (int i = 0; i < arguments.Length; i++)
            {
                string[] param = arguments[i].Split("=");
                args.Add(param[0], param[1]);
            }

            return args;
        }

        public string ReadString ( byte[] data, ref int offset)
        {
            string result = "";
            int len = (data[offset] << 24) + (data[offset + 1] << 16) + (data[offset + 2] << 8) + (data[offset + 3]);
            for (int i = 0; i < len; i++)
                result += Convert.ToChar(data[offset + i + 4]);
            offset += len + 4;
            return result;
        }

        public byte[] ReadVector(byte[] data, ref int offset)
        {
            byte[] result;
            int len = (data[offset] << 24) + (data[offset + 1] << 16) + (data[offset + 2] << 8) + (data[offset + 3]);
            result = new byte[len];
            System.Array.Copy(data, offset + 4, result, 0, len);
            offset += len + 4;
            return result;
        }


        public static string GetAsset(string hash)
        {
            string result = "error";

            string command = "get-asset";
            string getcommand = "{ \"id\": 0, \"method\" : \"getnft\", \"params\" : [ \"" + command + "\", \"" + hash + "\" ] }";

            try
            {
                string rpcResult = rpcExec(getcommand);
                dynamic jRPCResult = JObject.Parse(rpcResult);
                result = jRPCResult.result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return result;
        }


        public byte[] HexToByte(string data)
        {
            data = data.ToUpper();
            byte[] result = new byte[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2)
            {
                byte hi = hex(data[i]);
                byte lo = hex(data[i + 1]);
                result[i / 2] = (byte)(hi * 16 + lo);
            }

            return result;
        }


        public static string rpcExec(string command)
        {
            webRequest = (HttpWebRequest)WebRequest.Create(Global.FullNodeRPC());
            webRequest.KeepAlive = false;
            webRequest.Timeout = 300000;

            var data = Encoding.ASCII.GetBytes(command);

            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = data.Length;

            var username = Global.FullNodeUser();
            var password = Global.FullNodePass();
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            webRequest.Headers.Add("Authorization", "Basic " + encoded);


            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }


            var webresponse = (HttpWebResponse)webRequest.GetResponse();

            string submitResponse = new StreamReader(webresponse.GetResponseStream()).ReadToEnd();

            webresponse.Dispose();


            return submitResponse;
        }


        public bool ValidHex (string data)
        {
            data = data.ToLower();

            bool result = true;
            int i = 0;
            while ((i < data.Length) && (result))
            {
                result = (((data[i] >= '0') && (data[i] <= '9')) || ((data[i] >= 'a') && (data[i] <= 'f')));
                if (result)
                    i++;
            }
            return result;
        }

        public byte hex(char data)
        {

            if (data < 'A')
                return (byte)(data - '0');
            else
                return (byte)((data - 'A') + 10);
        }


        public int GetPlayerID (Dictionary<string, string> args)
        {
            string address = args["addr"];

            return Database.GetPlayerID(address);
        }

        public string GetLand (Dictionary<string,string> args)
        {
            int playerID = Convert.ToInt32(args["player_id"]);

            return Database.GetLand(playerID);
        }

        public string RegisterWallet(Dictionary<string, string> args)
        {
            string address = args["addr"];

            Database.RegisterWallet(address);

            return "ok";
        }

        public string RegisterPayment(Dictionary<string, string> args)
        {

            int playerID = Convert.ToInt32(args["player_id"]);
            string amt = args["amt"];
            string item = args["item"];
            Decimal iAmt = Convert.ToDecimal(amt);

            if ((item == "land2") && (iAmt != 10))
                return "error";
            if ((item == "land3") && (iAmt != 10))
                return "error";
            if ((item == "land4") && (iAmt != 30))
                return "error";
            if ((item == "land5") && (iAmt != 50))
                return "error";

            string hash = Database.RegisterPayment(playerID, amt, item);

            return hash;
        }

        public string RedeemPayment(Dictionary<string, string> args)
        {
            //todo - verify # of transaction confirmations
            //todo - verify transaction paid to correct wallet

            string paymentSecret = args["payment_secret"];
            string txid = args["txid"];
            int playerID = Convert.ToInt32(args["player_id"]);

            string result = "error";
            string item = Database.RedeemPayment(paymentSecret, txid);
            if (item == "land1")
            {
                Database.AddLandSlot(playerID, 1);
                result = "ok";
            }


            return result;
        }

        public string RenameLand(Dictionary<string, string> args)
        {

            /*
            string paymentSecret = args["playerID"];
            string txid = args["txid"];
            int playerID = Convert.ToInt32(args["playerID"]);

            string result = "error";
            string item = Database.RedeemPayment(paymentSecret, txid);
            if (item == "land1")
            {
                Database.AddLandSlot(playerID, 1);
                result = "ok";
            }

            */
            return "";
        }


    }
}
