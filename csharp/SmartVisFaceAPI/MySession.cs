#define LOCAL
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Net;
using System.IO;
using System.Text;
using SmartVisFaceAPI;


namespace SmartVisFaceAPI
{
    public class MySession
    {
        // You need to put your credentials and keys in here!
#if (LOCAL)
        private static string auth_host = "http://192.168.86.38:10000";
        private static string client_id = "e5c8b719397fed1ad69630c37a74fb28ad1f3b9a017a54a6ced4417c6ea4f858";
        private static string client_secret = "cd292c21fcbbdcacfb18c363e00545b2cbe5e710d2053518b0b5632fd95ed06c";
        private static string email = "demo@digitalbarriers.com";
        private static string password = "demo123!";
#else
        private static string auth_host = "https://auth-eudemo.videoai.net";
        private static string client_id = "";
        private static string client_secret = "";
        private static string email = "";
        private static string password = "";
#endif

        // Some constants that never change
        private static string oauth_body_hash = "2jmj7l5rSw0yVb%2FvlWAYkK%2FYBwk%3D";
        private static string oauth_token = "a_single_token";
        private static string oauth_signature_method = "HMAC-SHA1";
        private static string oauth_version = "1.0";
        

        public string DeviceData { get; set; }
        public string Token { get; set; }
        public string ApiUrl { get; set; }

        public MySession()
        {
            DeviceData = "device_id=\"myDeviceId\", device_name=\"myDevice\", lat=\"38.1499\", lng=\"144.3617\"";
        }


        public bool Authenticate()
        {
            string endpoint = "/auth/api_login";
            var method = "POST";
            Token = "a_single_token";

            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            postData.Add("email", Uri.EscapeDataString(email));
            postData.Add("password", Uri.EscapeDataString(password));
            var postDataAsBytes = PostDataAsByteArray(postData);
            var authorization = Authorization(method, endpoint, postData);

            WebRequest request = WebRequest.Create(auth_host + endpoint);
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Device", DeviceData);
            request.Headers.Add("Authorization", authorization);
            request.ContentLength = postDataAsBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(postDataAsBytes, 0, postDataAsBytes.Length);
            }

            dynamic data;
            try
            {
                using (System.IO.Stream s = request.GetResponse().GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                    {
                        var jss = new JavaScriptSerializer();
                        var json = sr.ReadToEnd();
                        data = jss.Deserialize<dynamic>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            Token = data["oauth_token"]["token"];
            ApiUrl = data["user"]["api_url"];
            return true;
        }
        
        private static string GetNonce()
        {
            var rand = new Random();
            var nonce = rand.Next(1000000000);
            return nonce.ToString();
        }

        private static string GetTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        private static string Sign(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha1 = new HMACSHA1(keyByte))
            {
                byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        public static byte[] PostDataAsByteArray(SortedDictionary<string, string> postData)
        {
            var pd = "";
            foreach (var kvp in postData)
            {
                if (pd.Length == 0)
                    pd += kvp.Key + "=" + kvp.Value;
                else
                    pd += "&" + kvp.Key + "=" + kvp.Value;
            }

            return Encoding.ASCII.GetBytes(pd);
        }

        
        public string Authorization(
            string method,
            string endPoint,
            SortedDictionary<string, string> postData)
        {
            var oauth_nonce = GetNonce();
            var oauth_timestamp = GetTimeStamp();

            postData.Add("device_data", Uri.EscapeDataString(DeviceData));
            postData.Add("oauth_body_hash", oauth_body_hash);
            postData.Add("oauth_consumer_key", client_id);
            postData.Add("oauth_nonce", oauth_nonce);
            postData.Add("oauth_signature_method", oauth_signature_method);
            postData.Add("oauth_timestamp", oauth_timestamp);
            postData.Add("oauth_token", Token);
            postData.Add("oauth_version", oauth_version);

            var parameters = "";
            foreach (KeyValuePair<string, string> param in postData)
            {
                if (parameters.Length == 0)
                    parameters += param.Key + "=" + param.Value;
                else
                    parameters += "&" + param.Key + "=" + param.Value;
            }

            var base_string = method + "&" + Uri.EscapeDataString(endPoint) + "&" + Uri.EscapeDataString(parameters);

            var signed = Sign(base_string, client_secret + "&");
            signed = Uri.EscapeDataString(signed);

            var authorization =
                "OAuth realm=\"\"" +
                ", oauth_body_hash=\"" + oauth_body_hash + "\"" +
                ", oauth_nonce=\"" + oauth_nonce + "\"" +
                ", oauth_timestamp=\"" + oauth_timestamp + "\"" +
                ", oauth_consumer_key=\"" + client_id + "\"" +
                ", oauth_signature_method=\"" + oauth_signature_method + "\"" +
                ", oauth_version=\"" + oauth_version + "\"" +
                ", oauth_token=\"" + Token + "\"" +
                ", oauth_signature=\"" + signed + "\"";

            return authorization;
        }
        
    }
}