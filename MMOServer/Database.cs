using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MMOServer
{
    public class Database
    {
        static string strConn = "datasource=localhost;port=3306;username=" + Global.dbUser + ";password=" + Global.dbPassword + ";database=" + Global.dbSchema;


        /*

                public static void setSetting(string name, string value)
        {
            string strSQL = "update setting set setting_value = @1 where setting_name = @2";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", value);
            cmd.Parameters.AddWithValue("@2", name);
            cmd.ExecuteNonQuery();
            conn.Close();
        }


        public static string getSetting(string name)
        {
            string strSQL = "select setting_value from setting where setting_name = @1";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", name);
            string result = cmd.ExecuteScalar().ToString();
            conn.Close();
            return result;
        }
         
         * */


        public static void RegisterWallet(string address)
        {
            string strSQL = "select count(1) from player where player_wallet = @1";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", address);
            int result = Convert.ToInt32(cmd.ExecuteScalar().ToString());
            conn.Close();
            if (result == 0)
            {
                strSQL = "insert into player (player_wallet, player_redeem_tokens) values (@1, 0)";
                conn = new MySqlConnection(strConn);
                conn.Open();
                cmd = new MySqlCommand(strSQL, conn);
                cmd.Parameters.AddWithValue("@1", address);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }



        public static int GetPlayerID(string address)
        {
            string strSQL = "select player_id from player where player_wallet = @1";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", address);
            int result = Convert.ToInt32(cmd.ExecuteScalar().ToString());
            conn.Close();
            return result;
        }

        public static string GetLand(int playerID)
        {

            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            string strSQL = "select land_id, land_type, land_name from land where land_player_id = " + playerID;
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Dictionary<string, string> entry = new Dictionary<string, string>();
                entry.Add("land_id", reader.GetString(0));
                entry.Add("land_type", reader.GetString(1));
                entry.Add("land_name", reader.GetString(2));
                result.Add(entry);
            }
            conn.Close();

            return JsonConvert.SerializeObject(result);
        }


        public static string RegisterPayment(int playerID, string amt, string item)
        {
            SHA256 sha = SHA256.Create();

            uint seed1 = Global.RandomNum((uint)(playerID + item.Length));
            uint seed2 = Global.RandomNum((uint)(playerID + amt.Length));

            byte[] bSeed1 = BitConverter.GetBytes(seed1);
            byte[] bSeed2 = BitConverter.GetBytes(seed2);
            byte[] seed = new byte[8];
            Buffer.BlockCopy(bSeed1, 0, seed, 0, 4);
            Buffer.BlockCopy(bSeed2, 0, seed, 4, 4);

            byte[] hash = sha.ComputeHash(seed);
            string secretHash = ByteArrayToString(hash);


            string strSQL = "insert into payment (payment_from_player_id, payment_amt, payment_timestamp, payment_redeemed, payment_secret, payment_item) values (@1, @2, @3, 0, @4, @5)";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", playerID);
            cmd.Parameters.AddWithValue("@2", amt);
            cmd.Parameters.AddWithValue("@3", DateTimeOffset.Now.ToUnixTimeSeconds());
            cmd.Parameters.AddWithValue("@4", secretHash);
            cmd.Parameters.AddWithValue("@5", item);
            int result = cmd.ExecuteNonQuery();
            conn.Close();

            return secretHash;
        }


        public static string RedeemPayment(string paymentSecret, string txid)
        {
            string result = "error";

            string strSQL = "select payment_item from payment where payment_secret = @1";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", paymentSecret);
            try
            {
                result = cmd.ExecuteScalar().ToString();
            }
            catch (Exception e) { }
            conn.Close();


            if (result != "error")
            {
                strSQL = "update payment set payment_redeemed = @1, payment_txid = @2 where payment_secret = @3";
                conn = new MySqlConnection(strConn);
                conn.Open();
                cmd = new MySqlCommand(strSQL, conn);
                cmd.Parameters.AddWithValue("@1", DateTimeOffset.Now.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("@2", txid);
                cmd.Parameters.AddWithValue("@3", paymentSecret);
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            return result;
        }


        public static void AddLandSlot ( int playerID, int slot)
        {
            string strSQL = "insert into land (land_player_id, land_type, land_name, land_slot) values (@1, @2, @3, @4)";
            MySqlConnection conn = new MySqlConnection(strConn);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(strSQL, conn);
            cmd.Parameters.AddWithValue("@1", playerID);
            cmd.Parameters.AddWithValue("@2", 0);
            cmd.Parameters.AddWithValue("@3", "land " + slot);
            cmd.Parameters.AddWithValue("@4", slot);
            int result = cmd.ExecuteNonQuery();
            conn.Close();
        }


        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
