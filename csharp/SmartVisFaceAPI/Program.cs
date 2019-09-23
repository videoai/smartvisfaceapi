
using System;
using System.Collections.Generic;

namespace SmartVisFaceAPI
{

    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                // Login...
                MySession mySession = new MySession();
                mySession.Authenticate();
                Console.WriteLine("API: " + mySession.ApiUrl + " Token: " + mySession.Token);
                var myRequest = new MyRequest(mySession);


                // List all the subjects
                //myRequest.Verbose = true;
                var subjects = myRequest.DoRequest("GET", "/subject");
                
                // Get a subject thumbnail
                //var subjectId = "b28e2f46-d8bd-41ae-8d5a-68293447678c";
                //myRequest.DoRequestToFile("/thumbnail/subject/" + subjectId, "/tmp/file.jpg");


                // Create a new watchlist
                SortedDictionary<string, string> formData = new SortedDictionary<string, string>();
                var random = new Random();
                formData.Add("name", Uri.EscapeDataString("MyWatchlist_" + random.Next().ToString()));
                formData.Add("colour", Uri.EscapeDataString("ff0000"));
                var createWatchlist = myRequest.DoRequest("POST", "/watchlist", formData);
                var watchlistId = createWatchlist["watchlist"]["id"];
                var watchlistName = createWatchlist["watchlist"]["name"];
                Console.WriteLine("Created watchlist " + watchlistName + " with id " + watchlistId);

                // Create a new subject
                var createSubjectData = new SortedDictionary<string, string>();
                createSubjectData.Add("name", Uri.EscapeDataString("John Smith"));
                var createSubject = myRequest.DoRequest("POST", "/subject", createSubjectData);
                var subjectId = createSubject["subject"]["subject_id"];
                var subjectName = createSubject["subject"]["name"];
                Console.WriteLine("Created subject " + subjectName + " with id " + subjectId);

                // Add the subject to the watchlist
                var addSubjectToWatchlistData = new SortedDictionary<string, string>();
                string watchlistIds = "[{0}]";
                watchlistIds = string.Format(watchlistIds, watchlistId);
                addSubjectToWatchlistData.Add("watchlist_ids", Uri.EscapeDataString(watchlistIds));
                var addSubjectToWatchlist = myRequest.DoRequest("PUT", "/subject/" + subjectId + "/watchlist" , addSubjectToWatchlistData);
          
                // Lets do a FaceLogImage
                var faceLogImageData = new SortedDictionary<string, string>();
                faceLogImageData.Add("compare_threshold", Uri.EscapeDataString("0.6"));
                faceLogImageData.Add("recognition", Uri.EscapeDataString("true"));
                var imagePath = "/home/kieron/src/smartvisfaceapi/csharp/SmartVisFaceAPI/images/kieron01.jpg";
                var faceLogImage = myRequest.DoFaceLogImage(faceLogImageData, imagePath, true);
                var numberOfSightings = faceLogImage["number_of_sightings"];
                Console.WriteLine("Face Log Task: " + faceLogImage["job_id"] + " found " + numberOfSightings  +  " sightings");
                

            }
            catch (SmartVisFaceException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}