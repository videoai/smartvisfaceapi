using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Script.Serialization;

namespace SmartVisFaceAPI
{

    class Program
    {
    
        class MySession
        {
            public string token;
            public string api_url;
        }
    
        private static string auth_host = "http://192.168.86.38:10000";
        private static string client_id = "48b009b1e9e651d806e91cc24a4239cdc28cabaafee51635f8b575f309db2d88";
        private static string client_secret = "2ae6ac4addaac04f688e6c6c17f6bcad501ace832f6da64e2d027f6d6322c8f6";
        private static string email = "demo@digitalbarriers.com";
        private static string password = "Demo123123";
        //private static string auth_host = "https://auth-eudemo.videoai.net";
        //private static string client_id = "43468dddbbf113ed312ba65dbe3dbb329a5cfb3115c6a6b4343b54f5fce551fc";
        //private static string client_secret = "1e4fd81af8d0551f51d20040bdcdd4336ebe612f82d62f7953ac364c882fdb05";
        //private static string email = "kieron.messer@digitalbarriers.com";
        //private static string password = "kieron123!";
        private static string oauth_body_hash = "2jmj7l5rSw0yVb%2FvlWAYkK%2FYBwk%3D";
        private static string oauth_token = "a_single_token";
        private static string oauth_signature_method = "HMAC-SHA1";
        private static string oauth_version = "1.0";
        
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

        private static string CreateToken(string message, string secret)
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

        private static bool ResponseOk(dynamic response)
        {
            string status = response["status"];
            return String.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
        }

        private static dynamic DoRequest(WebRequest request)
        {
            try
            {
                using (System.IO.Stream s = request.GetResponse().GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                    {
                        var jss = new JavaScriptSerializer();
                        var json = sr.ReadToEnd();
                        var json_object = jss.Deserialize<dynamic>(json);
                        return json_object;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new System.Dynamic.ExpandoObject(); 
        }
        
        private static MySession Login()
        {
            string auth_endpoint = "/auth/api_login";
            string post_data =
                "email=" + Uri.EscapeUriString(email) +
                "&password=" + Uri.EscapeUriString(password);
            byte[] data = Encoding.ASCII.GetBytes(post_data);

            var oauth_nonce = GetNonce();
            var oauth_timestamp = GetTimeStamp();
            var method = "POST";

            string parameters =
                "email=" + Uri.EscapeDataString(email) +
                "&oauth_body_hash=" + oauth_body_hash +
                "&oauth_consumer_key=" + client_id +
                "&oauth_nonce=" + oauth_nonce +
                "&oauth_signature_method=" + oauth_signature_method +
                "&oauth_timestamp=" + oauth_timestamp +
                "&oauth_token=" + oauth_token +
                "&oauth_version=" + oauth_version +
                "&password=" + Uri.EscapeDataString(password);

            string base_string = method + "&" + Uri.EscapeDataString(auth_endpoint) + "&" + Uri.EscapeDataString(parameters);

            string signed = CreateToken(base_string, client_secret + "&");
            signed = Uri.EscapeDataString(signed);

            string authorization_header =
                "OAuth realm=\"\"" +
                ", oauth_body_hash=\"" + oauth_body_hash + "\"" +
                ", oauth_nonce=\"" + oauth_nonce + "\"" +
                ", oauth_timestamp=\"" + oauth_timestamp + "\"" +
                ", oauth_consumer_key=\"" + client_id + "\"" +
                ", oauth_signature_method=\"" + oauth_signature_method + "\"" +
                ", oauth_version=\"" + oauth_version + "\"" +
                ", oauth_token=\"" + oauth_token + "\"" +
                ", oauth_signature=\"" + signed + "\"";

            WebRequest request = WebRequest.Create(auth_host + "/auth/api_login");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Authorization", authorization_header);
            request.Headers.Add("Initial-User-Agent", "client_id=" + client_id);
            request.ContentLength = data.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var jss = new JavaScriptSerializer();
            var mySession = new MySession();
            var json = DoRequest(request);
            mySession.token = json["oauth_token"]["token"];
            mySession.api_url = json["user"]["api_url"];
            return mySession;
        }

        private static bool ListSubjects(MySession mySession)
        {
            var device_data = Uri.EscapeDataString("device_id=\"id1234\", device_name=\"mydevice\", lat=\"0\", lng=\"0\"");
            var oauth_nonce = GetNonce();
            var oauth_timestamp = GetTimeStamp();
            var subject_end_point = "/subject";
            var method = "GET";

            var parameters =
                //"device_data=" + device_data +
                "oauth_body_hash=" + oauth_body_hash +
                "&oauth_consumer_key=" + client_id +
                "&oauth_nonce=" + oauth_nonce +
                "&oauth_signature_method=" + oauth_signature_method +
                "&oauth_timestamp=" + oauth_timestamp +
                "&oauth_token=" + mySession.token +
                "&oauth_version=" + oauth_version;
            
            var base_string = method + "&" + Uri.EscapeDataString(subject_end_point) + "&" + Uri.EscapeDataString(parameters);
            var signed = CreateToken(base_string, client_secret + "&");
            signed = Uri.EscapeDataString(signed);
            
            // Lets make a request to list all the subjects in the database
            var authorization_header =
                "OAuth realm=\"\"" +
                ", oauth_body_hash=\"" + oauth_body_hash + "\"" +
                ", oauth_nonce=\"" + oauth_nonce + "\"" +
                ", oauth_timestamp=\"" + oauth_timestamp + "\"" +
                ", oauth_consumer_key=\"" + client_id + "\"" +
                ", oauth_signature_method=\"" + oauth_signature_method + "\"" +
                ", oauth_version=\"" + oauth_version + "\"" +
                ", oauth_token=\"" + mySession.token + "\"" +
                ", oauth_signature=\"" + signed + "\"";
            
            var subject_request = WebRequest.Create(mySession.api_url + subject_end_point);
            subject_request.Method = method;
            //request.ContentType = "application/x-www-form-urlencoded";
            //subject_request.Headers.Add("Device", "device_data=" + device_data);
            subject_request.Headers.Add("Authorization", authorization_header);
            subject_request.Headers.Add("Initial-User-Agent", "client_id=" + client_id);
            var json_object = DoRequest(subject_request);
            return true;
        }
       
        
        static void Main(string[] args)
        {
            // Login...
            MySession mySession = Login();
            Console.WriteLine("API: " + mySession.api_url + " Token: " + mySession.token);

            // List subjects...
            ListSubjects(mySession);
        }
    }
}
