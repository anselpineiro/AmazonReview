using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonReview.Models
{
    public class ReviewWord
    {
        public const string lastWord = "!last";
        public static readonly char[] punctuations = { '.', ',', '!', '?' };

        private string wordText;

        private bool processed;
        private int timesUsed;
        private bool hasPunctuations;
        private bool punctuationsProcessed;
        private char[] punctuationMatrix;
        private Dictionary<char, int> punctuationUsage;
        private Dictionary<ReviewWord, int> nextWordCollection;        
        KeyValuePair<ReviewWord, int>[] probabilityMatrix;
        



        public string Value { get { return wordText; } }

        public ReviewWord(string wordText)
        {
            populatePuctuationUsage();
            this.wordText = handlePunctuation(wordText.ToLower());
            nextWordCollection = new Dictionary<ReviewWord, int>();
            processed = false;
            timesUsed = 1;


        }
        /// <summary>
        /// Tracks punctuation usage and strips punctuation from word
        /// </summary>
        /// <param name="wordText"></param>
        /// <returns></returns>
        private string handlePunctuation(string wordText)
        {
            char c = wordText[wordText.Length - 1];
            if (!punctuations.Contains(c))
                return wordText;

            punctuationUsage[c]++;
            this.hasPunctuations = true;

            return wordText.Substring(0, wordText.Length - 1);
        }

        /// <summary>
        /// Tracks Punctuation Usages.
        /// For use when training dataset only.
        /// </summary>
        /// <param name="punctuation"></param>
        public void trackPunctuation(char punctuation)
        {
            if (punctuation != (char)0)
                punctuationUsage[punctuation]++;
        }


        /// <summary>
        /// Adds a word that comes after this word.
        /// For use when training dataset only.
        /// </summary>
        /// <param name="nextWord"></param>
        public void addNextWord(ReviewWord nextWord)
        {
            if (nextWord == null)
                return;

            if (nextWordCollection.ContainsKey(nextWord))
                nextWordCollection[nextWord] += 1;
            else
                nextWordCollection.Add(nextWord, 1);

        }
        /// <summary>
        /// Gets a next word at random.  Probability determined by frequency in training data.
        /// </summary>
        /// <returns>ReviewWord</returns>
        public ReviewWord getNextWord()
        {
            if (Value == ReviewWord.lastWord)
                return null;
            processNextWords();
            return probabilityMatrix.Length == 0 ? null : probabilityMatrix[new Random().Next(probabilityMatrix.Length - 1)].Key;
        }
        /// <summary>
        /// Returns a punctuation or (char)0 with probability determined by frequency in training data
        /// </summary>
        /// <returns>Char</returns>
        public char getPunctuation()
        {

            //Calculate probability of punctuation being used
            processPunctuations();
            return !hasPunctuations ? (char)0 : punctuationMatrix[new Random().Next(punctuationMatrix.Length)];


        }


        /// <summary>
        /// Process next words to get the percentage of the next word is used after this word
        /// </summary>
        private void processNextWords()
        {

            if (processed)
                return;

            //Order words to start with most frequently used
            List<KeyValuePair<ReviewWord, int>> nextWordList = nextWordCollection.OrderByDescending(x => x.Value).ToList();

            Dictionary<ReviewWord, int> nextWordPercent = processAbsolutePercents();

            processRelativePercents(nextWordPercent);

            processed = true;

            nextWordCollection = nextWordPercent;


            populateProbabilityMatrix();

        }

        /// <summary>
        /// Process the percent of the times next words are used.  Not adjusted for words removed.
        /// </summary>
        /// <returns></returns>
        private Dictionary<ReviewWord, int> processAbsolutePercents()
        {
            List<KeyValuePair<ReviewWord, int>> nextWordList = nextWordCollection.OrderByDescending(x => x.Value).ToList();

            Dictionary<ReviewWord, int> nextWordPercent = new Dictionary<ReviewWord, int>();

            //Sum of all values in next word collection
            int sum = nextWordCollection.Sum(x => x.Value);

            //Find the absolute percent of the time a word is used. 
            foreach (KeyValuePair<ReviewWord, int> pair in nextWordList)
            {
                int percent = (int)(((float)pair.Value / (float)sum) * 100);
                //Only add if this word is used more than 1 percent of the time
                if (percent >= 1)
                    nextWordPercent.Add(pair.Key, percent);
            }

            return nextWordPercent;
        }


        /// <summary>
        /// Convert number of times a punctuation is used to a percentage
        /// </summary>
        private void processPunctuations()
        {
            if (punctuationsProcessed)
                return;

            foreach (char c in punctuations)
            {
                if (punctuationUsage[c] == 0)
                    continue;
                this.hasPunctuations = true;
                //Keep this word marked as having punctuations used at least 1% of the time                
                punctuationUsage[c] = (int)(((float)punctuationUsage[c] / (float)timesUsed));
            }

            populatePunctuationMatrix();

            this.punctuationsProcessed = true;
        }


        /// <summary>
        /// Process the percent of the times next words are used.  Adjusted for words removed.
        /// </summary>
        /// <param name="nextWordPercent"></param>
        private void processRelativePercents(Dictionary<ReviewWord, int> nextWordPercent)
        {
            int sum = nextWordPercent.Sum(x => x.Value);

            //Change percentages to relative
            for (int i = 0; i < nextWordPercent.Count(); i++)
            {
                KeyValuePair<ReviewWord, int> element = nextWordPercent.ElementAt(i);
                nextWordPercent[element.Key] = (int)(((float)element.Value / (float)sum) * 100);

            }

        }
        /// <summary>
        /// Populates the matrix used to pull a next word from a random index
        /// </summary>
        private void populateProbabilityMatrix()
        {
            //Populate the probability matrix
            probabilityMatrix = new KeyValuePair<ReviewWord, int>[nextWordCollection.Sum(x => x.Value)];

            int index = 0;
            foreach (KeyValuePair<ReviewWord, int> pair in nextWordCollection)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    probabilityMatrix[index] = pair;
                    index++;
                }
            }
        }
        /// <summary>
        /// Populates the matrix used to pull a punctuation from a random index provided there is a chance.
        /// </summary>
        private void populatePunctuationMatrix()
        {
            if (!hasPunctuations)
                return;

            punctuationMatrix = new char[100];
            int index = 0;

            //Populate matrix with punctuations as used.
            foreach (KeyValuePair<char, int> pair in punctuationUsage)
            {
                if (pair.Value == 0)
                    continue;
                for (int i = index; i < pair.Value; i++)
                {
                    punctuationMatrix[index] = pair.Key;
                    index++;
                }
            }

            if (index == 99)
                return;

            //Populate remaining matrix with empty chars
            for (int i = index; i < punctuationMatrix.Length; i++)
            {
                punctuationMatrix[i] = (char)0;
            }
        }

        /// <summary>
        /// Populates the punctuation map that keeps track of punctuations used.
        /// </summary>
        private void populatePuctuationUsage()
        {
            punctuationUsage = new Dictionary<char, int>();
            foreach (char c in punctuations)
            {
                punctuationUsage.Add(c, 0);
            }
        }

        /// <summary>
        /// Tally how many times this word is used.
        /// </summary>
        public void trackUsage()
        {
            timesUsed++;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
                return false;

            if (Object.ReferenceEquals(obj, this))
                return true;

            return ((ReviewWord)obj).Value == wordText;
        }


        public override int GetHashCode()
        {
            return wordText.GetHashCode();
        }


    }
}
