using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmazonReview.Models;

namespace AmazonReview
{
    public interface IWordHandler
    {
        Dictionary<string, ReviewWord> reviewWords { get; }
        void trainReviewData();

    }
}
