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

        class MyHeaders
        {
            public string authorization;
            public string device_data;
        }
        
    
        // You need to put your credentials and keys in here!
        private static string auth_host = "https://auth-eudemo.videoai.net";
        private static string client_id = "43468dddbbf113ed312ba65dbe3dbb329a5cfb3115c6a6b4343b54f5fce551fc";
        private static string client_secret = "1e4fd81af8d0551f51d20040bdcdd4336ebe612f82d62f7953ac364c882fdb05";
        private static string email = "kieron.messer@digitalbarriers.com";
        private static string password = "";
        
        // Some constants that never change
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
                        //Console.WriteLine(json);
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
        
        private static byte[] PostDataAsByteArray(SortedDictionary<string, string>  postData)
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
        
        private static MyHeaders BuildHeaders(MySession mySession, 
            string method,
            string endPoint,
            SortedDictionary<string, string> postData)
        {
            var device_data = "device_id=\"myDeviceId\", device_name=\"myDevice\", lat=\"38.1499\", lng=\"144.3617\"";
            var oauth_nonce = GetNonce();
            var oauth_timestamp = GetTimeStamp();

            postData.Add("device_data", Uri.EscapeDataString(device_data));
            postData.Add("oauth_body_hash", oauth_body_hash);
            postData.Add("oauth_consumer_key", client_id);
            postData.Add("oauth_nonce", oauth_nonce);
            postData.Add("oauth_signature_method", oauth_signature_method);
            postData.Add("oauth_timestamp", oauth_timestamp);
            postData.Add("oauth_token", mySession.token);
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
            var signed = CreateToken(base_string, client_secret + "&");
            signed = Uri.EscapeDataString(signed);
            
            // Lets make a request to list all the subjects in the database
            var authorization =
                "OAuth realm=\"\"" +
                ", oauth_body_hash=\"" + oauth_body_hash + "\"" +
                ", oauth_nonce=\"" + oauth_nonce + "\"" +
                ", oauth_timestamp=\"" + oauth_timestamp + "\"" +
                ", oauth_consumer_key=\"" + client_id + "\"" +
                ", oauth_signature_method=\"" + oauth_signature_method + "\"" +
                ", oauth_version=\"" + oauth_version + "\"" +
                ", oauth_token=\"" + mySession.token + "\"" +
                ", oauth_signature=\"" + signed + "\"";


            MyHeaders myHeaders = new MyHeaders();
            myHeaders.device_data = device_data;
            myHeaders.authorization = authorization;
            return myHeaders;
        }
        
        
        private static MySession Login()
        {
            string endpoint = "/auth/api_login";
            var method = "POST";
            var mySession = new MySession();
            mySession.token = "a_single_token";
            
            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            postData.Add("email", Uri.EscapeDataString(email));
            postData.Add("password", Uri.EscapeDataString(password));
            var postDataAsBytes = PostDataAsByteArray(postData);
            var myHeaders = BuildHeaders(mySession, method, endpoint, postData);
            

            WebRequest request = WebRequest.Create(auth_host + endpoint);
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Device", myHeaders.device_data);
            request.Headers.Add("Authorization", myHeaders.authorization);
            request.ContentLength = postDataAsBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(postDataAsBytes, 0, postDataAsBytes.Length);
            }
            var jss = new JavaScriptSerializer();
            var json = DoRequest(request);
            mySession.token = json["oauth_token"]["token"];
            mySession.api_url = json["user"]["api_url"];
            return mySession;
        }

        
        private static bool ListSubjects(MySession mySession)
        {
            var end_point = "/subject";
            var method = "GET";
            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            var myHeaders = BuildHeaders(mySession, method, end_point, postData);

            
            var request = WebRequest.Create(mySession.api_url + end_point);
            request.Method = method;
            request.Headers.Add("Device", myHeaders.device_data);
            request.Headers.Add("Authorization", myHeaders.authorization);
            var json_object = DoRequest(request);

            if (ResponseOk(json_object))
            {
                Console.WriteLine("Subject Status: " + json_object["status"] + " Subject filtered #: " + json_object["data"]["total_filtered"] + " total #: " + json_object["data"]["total_number"]);
            }
            else
            {
                Console.WriteLine("Failed to list subjects: " + json_object["message"]);
            }
            
            return true;
        }

        
        private static bool CreateWatchlist(MySession mySession, string watchlistName, string watchlistColour)
        {
            var end_point = "/watchlist";
            var method = "POST";

            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            postData.Add("name", Uri.EscapeDataString(watchlistName));
            postData.Add("colour", Uri.EscapeDataString(watchlistColour));
            var postDataAsBytes = PostDataAsByteArray(postData);
            var myHeaders = BuildHeaders(mySession, method, end_point, postData);
            
            var request = WebRequest.Create(mySession.api_url + end_point);
            request.Method = method;
            request.Headers.Add("Device", myHeaders.device_data);
            request.Headers.Add("Authorization", myHeaders.authorization);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postDataAsBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(postDataAsBytes, 0, postDataAsBytes.Length);
            }

            var json_object = DoRequest(request);
            if (ResponseOk(json_object))
            {
                Console.WriteLine("Created Watchlist with id: " + json_object["data"]["watchlist"]["id"]);
            }
            else
            {
                Console.WriteLine("Failed to create watchlist: " + json_object["message"]);
            }
            return true;
        }
        
        
        private static bool CreateSubject(MySession mySession, string subjectName)
        {
            var end_point = "/subject";
            var method = "POST";

            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            postData.Add("name", Uri.EscapeDataString(subjectName));
            var postDataAsBytes = PostDataAsByteArray(postData);
            var myHeaders = BuildHeaders(mySession, method, end_point, postData);
            
            var request = WebRequest.Create(mySession.api_url + end_point);
            request.Method = method;
            request.Headers.Add("Device", myHeaders.device_data);
            request.Headers.Add("Authorization", myHeaders.authorization);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postDataAsBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(postDataAsBytes, 0, postDataAsBytes.Length);
            }

            var json_data = DoRequest(request);
            if (ResponseOk(json_data))
            {
                Console.WriteLine("Created subject with id: " + json_data["data"]["subject"]["subject_id"]);
            }
            else
            {
                Console.WriteLine("Failed to create subject: " + json_data["message"]);
            }
            return true;
        }
        
        static void Main(string[] args)
        {
            // Login...
            MySession mySession = Login();
            Console.WriteLine("API: " + mySession.api_url + " Token: " + mySession.token);

            // List subjects...
            ListSubjects(mySession);
            
            // Create a watchlist
            CreateWatchlist(mySession, "Yipee2", "#ff0000");
            
            // Create as subject 
            CreateSubject(mySession, "John Smith");
        }
    }
}
