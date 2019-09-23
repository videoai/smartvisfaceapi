
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web.Script.Serialization;

namespace SmartVisFaceAPI
{
    
    
    public class MyRequest
    {

        private MySession m_session;
        public bool Verbose { get; set; }
        
        public MyRequest(MySession mySession, bool verbose = false)
        {
            m_session = mySession;
            Verbose = verbose;
        }
        
        private dynamic DoRequestToJSON(WebRequest request)
        {
            try
            {
                using (System.IO.Stream s = request.GetResponse().GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                    {
                        var jss = new JavaScriptSerializer();
                        var json = sr.ReadToEnd();
                        if(Verbose) {
                            Console.WriteLine(json);
                        }
                        var responseData = jss.Deserialize<dynamic>(json);
                        return responseData;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return new System.Dynamic.ExpandoObject();
        }
        
        private void DoRequestToFile(WebRequest request, string filePath)
        {
        }
        


        public dynamic DoRequest(string method, 
            string endPoint, 
            SortedDictionary<string, string> formData = null)
        {
            
            if(formData == null)
                formData = new SortedDictionary<string, string>();
            
            var request = WebRequest.Create(m_session.ApiUrl + endPoint);
            request.Method = method;
            request.Headers.Add("Device", m_session.DeviceData);
            if (method.Equals("POST") || method.Equals("PUT") || method.Equals("DELETE"))
            {
                request.ContentType = "application/x-www-form-urlencoded";
                var postDataAsBytes = MySession.PostDataAsByteArray(formData);
                request.ContentLength = postDataAsBytes.Length;
                var authorization = m_session.Authorization(method, endPoint, formData);
                request.Headers.Add("Authorization", authorization);
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(postDataAsBytes, 0, postDataAsBytes.Length);
                }
            }
            else
            {
                var authorization = m_session.Authorization(method, endPoint, formData);
                request.Headers.Add("Authorization", authorization);
            }

            // Finally lets do the request
            var responseData = DoRequestToJSON(request);
            if (!ResponseOk(responseData))
            {
                throw new SmartVisFaceException("Failed to perform request: " + responseData["message"]);
            }

            if(((IDictionary<String, object>) responseData).ContainsKey("task"))
            {
                return responseData["task"];
            }
            return responseData["data"];
        }
        
        
        public void DoRequestToFile(string endPoint, string filePath)
        {
            var method = "GET"; 
            var formData = new SortedDictionary<string, string>(); 
            var request = WebRequest.Create(m_session.ApiUrl + endPoint);
            request.Method = method;
            request.Headers.Add("Device", m_session.DeviceData);
            var authorization = m_session.Authorization(method, endPoint, formData);
            request.Headers.Add("Authorization", authorization);

            byte[] buffer = new byte[4096];
            
            var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create));
            try
            {
                using (System.IO.Stream s = request.GetResponse().GetResponseStream())
                {
                    int count = 0;
                    do
                    {
                        count = s.Read(buffer, 0, buffer.Length);
                        bw.Write(buffer, 0, count);
                    } while (count != 0);
                }
            }
            catch (Exception ex)
            {
                throw new SmartVisFaceException(ex.ToString());
            }
        }

        private dynamic WaitForTask(string taskId, string task = "/task")
        {
            dynamic taskData = null;
            while (true)
            {
                try
                {
                    taskData = DoRequest("GET",  task + "/" + taskId);
                    if (taskData["complete"])
                    {
                        return taskData;
                    }
                    Thread.Sleep(250);
                }
                catch (SmartVisFaceException e)
                {
                    Console.WriteLine("Trouble waiting for task! " + e.Message);
                    return taskData;
                }
            }
        }
        
        
        public dynamic DoFaceLogImage(SortedDictionary<string, string> formData = null, string imagePath = null, bool waitForTask=true)
        {

            if(formData == null)
                formData = new SortedDictionary<string, string>();
            string method = "POST";
            string endPoint = "/face_log_image";
            
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            
            string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";
            Stream memStream = new System.IO.MemoryStream();
            if (formData != null)
            {
                foreach (string key in formData.Keys)
                {
                    string formitem = string.Format(formdataTemplate, key, formData[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    memStream.Write(formitembytes, 0, formitembytes.Length);
                }
            }

            var postDataAsBytes = MySession.PostDataAsByteArray(formData);
            var request = (HttpWebRequest) WebRequest.Create(m_session.ApiUrl + endPoint);
            request.Method = method;
            request.KeepAlive = true;
            request.Headers.Add("Device", m_session.DeviceData);
            var authorization = m_session.Authorization("POST", endPoint, formData);
            request.Headers.Add("Authorization", authorization);
            request.ContentType = "multipart/form-data; boundary=" + boundary;

            // Do we have to post an image
            var boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var endBoundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--");


            string headerTemplate =
                "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
                "Content-Type: application/octet-stream\r\n\r\n";

            memStream.Write(boundarybytes, 0, boundarybytes.Length);
            var header = string.Format(headerTemplate, "image", "image.jpg");
            var headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

            memStream.Write(headerbytes, 0, headerbytes.Length);

            using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[1024];
                var bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
            }

            memStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
            request.ContentLength = memStream.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                memStream.Close();
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            }

            // Finally lets do the request
            var responseData = DoRequestToJSON(request);
            if (!ResponseOk(responseData))
            {
                throw new SmartVisFaceException("Failed to perform request: " + responseData["message"]);
            }

            var taskData = responseData["task"];
            if(waitForTask)
            {
                var taskId = responseData["task"]["job_id"];
                taskData = WaitForTask(taskId, endPoint);
            }    
            return taskData;
        }
        
        
        private static bool ResponseOk(dynamic response)
        {
            string status = response["status"];
            return String.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
        }
        
        
    }
}