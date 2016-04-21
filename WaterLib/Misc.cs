using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace WaterLib
{
    public class Misc
    {
        /// <summary>
        /// Normalize the List to be probabilities, i.e., sum of this List is 1.
        /// </summary>
        /// <param name="probs">The List to be normalized</param>
        public static void NormalizeToProb(List<double> probs)
        {
            double sum = 0.0; int i;
            for (i = 0; i < probs.Count; i++) sum += probs[i];
            for (i = 0; i < probs.Count; i++) probs[i] /= sum;
        }
        public static void NormalizeToProb(double[] probs)
        {
            double sum = 0.0; int i;
            for (i = 0; i < probs.Length; i++) sum += probs[i];
            for (i = 0; i < probs.Length; i++) probs[i] /= sum;
        }

        /// <summary>
        /// Max value of a double List
        /// </summary>
        /// <param name="values">The list to be searched.</param>
        /// <param name="max">The max value.</param>
        /// <param name="max_index">The position of the max value</param>
        public static void Max(List<double> values, out double max, out int max_index)
        {
            max = values[0]; max_index = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] > max)
                {
                    max = values[i]; max_index = i;
                }
            }
        }

    }

    public class ListEx<T>
    {
        /// <summary>
        /// conduct deep copy (clone) of list
        /// </summary>
        /// <param name="list_src">to be copied list.</param>
        /// <returns>The cloned list</returns>
        static public List<T> DeepCopy(List<T> list_src)
        {
            List<T> list = new List<T>();
            foreach (T value in list_src) list.Add(value);
            return list;
        }

        /// <summary>
        /// deep copy (clone) from an array to a list
        /// </summary>
        /// <param name="array">to be copyed array.</param>
        /// <returns>The copied list</returns>
        static public List<T> DeepCopy(T[] array)
        {
            List<T> list = new List<T>();
            foreach (T value in array) list.Add(value);
            return list;
        }

        ///// <summary>
        ///// convert an array to a list
        ///// </summary>
        ///// <param name="array">to be converted array.</param>
        ///// <returns>The converted list</returns>
        //static public List<T> ConvertFromArray(T[] array)
        //{
        //    List<T> list = new List<T>();
        //    foreach (T value in array) list.Add(value);
        //    return list;
        //}


        /// <summary>
        /// Add a sequence of duplicated values to the list
        /// </summary>
        /// <param name="list">the list to be manipulated</param>
        /// <param name="value">the value to added to the list</param>
        /// <param name="count">repeat how many times</param>
        static public void Add(List<T> list, T value, int count)
        {
            for (int i = 0; i < count; i++) list.Add(value);
        }

        /// <summary>
        /// Randomly shuffle the list
        /// </summary>
        /// <param name="listToShuffle"> the list to be shuffled </param>
        /// <returns>The randomly shuffled list</returns>
        static public List<T> RandomShuffle(List<T> listToShuffle)
        {
            List<int> ints = new List<int>(listToShuffle.Count); //0, 1, 2, ...
            for (int i = 0; i < listToShuffle.Count; i++) ints.Add(i);

            List<T> randList = new List<T>(listToShuffle.Capacity);

            Random random = new Random();
            for (int k = 0; k < listToShuffle.Count; k++)
            {
                int randIndx = random.Next(ints.Count); //random from 0, 1, 2, .. not already picked
                int randK = ints[randIndx];
                randList.Add(listToShuffle[randK]);
                ints.RemoveAt(randIndx);
            }

            return randList;
        }

        /// <summary>
        /// Randomly shuffle the list
        /// </summary>
        /// <param name="listToShuffle"> the list to be shuffled </param>
        /// <param name="n"> the number of selected elements </param>
        /// <returns>The randomly shuffled list</returns>
        static public List<T> RandomShuffle(List<T> listToShuffle, int seed)
        {
            List<int> ints = new List<int>(listToShuffle.Count); //0, 1, 2, ...
            for (int i = 0; i < listToShuffle.Count; i++) ints.Add(i);

            List<T> randList = new List<T>(listToShuffle.Capacity);

            Random random = new Random(seed);
            for (int k = 0; k < listToShuffle.Count; k++)
            {
                int randIndx = random.Next(ints.Count); //random from 0, 1, 2, .. not already picked
                int randK = ints[randIndx];
                randList.Add(listToShuffle[randK]);
                ints.RemoveAt(randIndx);
            }

            return randList;
        }

        /// <summary>
        /// Randomly select from a list
        /// </summary>
        /// <param name="listToShuffle"> the list to be selected from </param>
        /// <param name="n"> the number of selected elements </param>
        /// <returns>The randomly selected elements</returns>
        static public List<T> RandomSelect(List<T> listToSelect, int n)
        {
            List<int> ints = new List<int>(listToSelect.Count); //0, 1, 2, ...
            for (int i = 0; i < listToSelect.Count; i++) ints.Add(i);

            List<T> randList = new List<T>(n);

            Random random = new Random();
            for (int k = 0; k < n; k++)
            {
                int randIndx = random.Next(ints.Count); //random from 0, 1, 2, .. not already picked
                int randK = ints[randIndx];
                randList.Add(listToSelect[randK]);
                ints.RemoveAt(randIndx);
            }

            return randList;
        }

        /// <summary>
        /// Randomly select from a list
        /// </summary>
        /// <param name="listToShuffle"> the list to be selected from </param>
        /// <param name="n"> the number of selected elements </param>
        /// <param name="seed"> the seed for random number generator </param>
        /// <returns>The randomly selected elements</returns>
        static public List<T> RandomSelect(List<T> listToSelect, int n, int seed)
        {
            List<int> ints = new List<int>(listToSelect.Count); //0, 1, 2, ...
            for (int i = 0; i < listToSelect.Count; i++) ints.Add(i);

            List<T> randList = new List<T>(n);

            Random random = new Random(seed);
            for (int k = 0; k < n; k++)
            {
                int randIndx = random.Next(ints.Count); //random from 0, 1, 2, .. not already picked
                int randK = ints[randIndx];
                randList.Add(listToSelect[randK]);
                ints.RemoveAt(randIndx);
            }

            return randList;
        }
    }
}
