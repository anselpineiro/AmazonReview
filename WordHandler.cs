using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using AmazonReview.Models;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace AmazonReview
{
    public class WordHandler : IWordHandler
    {

        private IConfiguration config;

        Dictionary<string, ReviewWord> _reviewWords;

        public Dictionary<string, ReviewWord> reviewWords { get { return _reviewWords; } }

        public WordHandler(IConfiguration config)
        {
            this.config = config;
        }

        public void trainReviewData()
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, string.Format("{0}\\{1}", config["DataDirectory"], config["DataFile"]));
            Dictionary<string, ReviewWord> reviewWordMap = new Dictionary<string, ReviewWord>();

            //File doesn't yet exist.  Download and extract it
            if (!File.Exists(filePath))
                downloadWordData(filePath);

            //Read the file line by line and parse the json into ReviewWord objects
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        JObject o = JObject.Parse(line);
                        string[] reviewText = o["reviewText"].ToString().ToLower().Split(new char[] { ' ', ';', ':' });

                        for (int i = 0; i < reviewText.Length; i += 2)
                        {
                            string text = reviewText[i];
                            char punctuation;
                            if (parsePunctuation(text, out punctuation))
                                text = text.Substring(0, text.Length - 1);


                            if (text.Trim() == "")
                                continue;

                            ReviewWord reviewWord = AddReviewWord(reviewWordMap, text, punctuation);
                            if (reviewWord == null)
                                continue;


                            string nextWord = i < reviewText.Length - 1 ? reviewText[i + 1].Trim() : ReviewWord.lastWord;
                            parsePunctuation(nextWord, out punctuation);
                            if (nextWord.Trim() != "")
                                reviewWord.addNextWord(AddReviewWord(reviewWordMap, nextWord, punctuation));

                        }

                    }
                }
            }
            this._reviewWords = reviewWordMap;
        }

        private void downloadWordData(string filePath)
        {
            //Filename is after the last / in the DataDownloadUrl appsettings.json value
            string[] pathParts = config["DataDownloadUrl"].Split("/");
            string compressedPath = Path.Combine(Environment.CurrentDirectory, string.Format("{0}\\{1}", config["DataDirectory"], pathParts[pathParts.Length -1]));

            //Download the file defined at the url in DataDownloadUrl appsettings.json value
            if (!File.Exists(compressedPath)) 
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(config["DataDownloadUrl"], compressedPath);
                }
            }
            //Decompress the file to make it ready to read
            using (FileStream compressedFileStream = File.OpenRead(compressedPath))
            {
                using (FileStream decompressedStream = File.Create(filePath))
                {
                    using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                    }
                }
            }
        }

        private static ReviewWord AddReviewWord(Dictionary<string, ReviewWord> reviewWordMap, string text, char punctuation)
        {

            if (text.Length == 1 && ReviewWord.punctuations.Contains(text[0]))
                return null;


            bool exists = reviewWordMap.ContainsKey(text);
            ReviewWord reviewWord = !exists ? new ReviewWord(text) : reviewWordMap[text];
            if (!exists)
            {
                reviewWordMap.Add(text, reviewWord);
                reviewWord.trackUsage();
                reviewWord.trackPunctuation(punctuation);
            }

            return reviewWord;
        }

        private static bool parsePunctuation(string text, out char punctuation)
        {
            punctuation = (char)0;

            if (text.Length > 1 && ReviewWord.punctuations.Contains(text[text.Length - 1]))
            {
                punctuation = text[text.Length - 1];
                return true;
            }

            return false;
        }
    }
}
