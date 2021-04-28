using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmazonReview.Models;
using System.Text;

namespace AmazonReview.Controllers
{
    [ApiController]
    
    public class AmazonReviewController : Controller
    {

        private readonly IWordHandler dataTrainer;
        public AmazonReviewController(IWordHandler dataTrainer)
        {
            this.dataTrainer = dataTrainer;
        }

        [HttpGet]
        [Route("api/{wordText}")]
        public string AmazonReview(string wordText)
        {
            Dictionary<string, ReviewWord> reviewWordMap = dataTrainer.reviewWords;

            StringBuilder builder = new StringBuilder();

            Console.Write("Start Word: ");
            
            ReviewWord word = reviewWordMap.ContainsKey(wordText) ? reviewWordMap[wordText] : null;

            if (word == null)
                return "Word not found";
            
            while (word != null && word.Value != ReviewWord.lastWord)
            {
                builder.Append(word.Value);
                char punctuation = word.getPunctuation();
                if (punctuation != (char)0)
                    builder.Append(word.getPunctuation());
                builder.Append(" ");                
                word = word.getNextWord();
            }

            return builder.ToString();
        }

    }
}
