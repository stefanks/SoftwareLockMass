using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spectra
{
    static class ClassExtensions
    {
        public static double[] BoxCarSmooth(this double[] data, int points)
        {
            // Force to be odd
            points = points - (1 - points % 2);

            int count = data.Length;

            if (points <= 0 || points > count)
            {
                return null;
            }

            int newCount = count - points + 1;

            double[] smoothedData = new double[newCount];

            for (int i = 0; i < newCount; i++)
            {
                double value = 0;

                for (int j = i; j < i + points; j++)
                {
                    value += data[j];
                }

                smoothedData[i] = value / points;
            }
            return smoothedData;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Compares two doubles for equality based on their absolute difference being less
        /// than some specified tolerance.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool FuzzyEquals(this double item1, double item2, double tolerance = 1e-10)
        {
            return Math.Abs(item1 - item2) < tolerance;
        }

        /// <summary>
        /// Compares two floats for equality based on their absolute difference being less
        /// than some specified tolerance.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool FuzzyEquals(this float item1, float item2, double tolerance = 1e-10)
        {
            return Math.Abs(item1 - item2) < tolerance;
        }
        public static int MaxIndex<TSource>(this IEnumerable<TSource> items) where TSource : IComparable<TSource>
        {
            TSource maxItem;
            return MaxIndex(items, o => o, out maxItem);
        }

        public static int MaxIndex<TSource>(this IEnumerable<TSource> items, out TSource maxItem) where TSource : IComparable<TSource>
        {
            return MaxIndex(items, o => o, out maxItem);
        }

        /// <summary>
        /// Finds the index of the maximum value in a collection
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="items">The collection of items</param>
        /// <returns>An index to the place of the maximum value in the collection</returns>
        public static int MaxIndex<TSource, TResult>(this IEnumerable<TSource> items, Func<TSource, TResult> selectFunc) where TResult : IComparable<TResult>
        {
            TSource maxItem;
            return MaxIndex(items, selectFunc, out maxItem);
        }

        /// <summary>
        /// Finds the index of the maximum value in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The collection of items</param>
        /// <param name="maxValue">The maximum value in the collection</param>
        /// <returns>An index to the place of the maximum value in the collection</returns>
        public static int MaxIndex<TSource, TResult>(this IEnumerable<TSource> items, Func<TSource, TResult> selectFunc, out TSource maxItem) where TResult : IComparable<TResult>
        {
            // From: http://stackoverflow.com/questions/462699/how-do-i-get-the-index-of-the-highest-value-in-an-array-using-linq
            int maxIndex = -1;
            TResult maxValue = default(TResult);
            maxItem = default(TSource);

            int index = 0;
            foreach (TSource item in items)
            {
                TResult value = selectFunc(item);

                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxItem = item;
                    maxValue = value;
                }
                index++;
            }
            return maxIndex;
        }
        /// <summary>
        /// Decompresses a byte array using Gzip decompression
        /// </summary>
        /// <param name="bytes">The byte array to decompress</param>
        /// <returns>The decompressed byte array</returns>
        public static byte[] Decompress(this byte[] bytes)
        {
            var bigStreamOut = new MemoryStream();
            new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress).CopyTo(bigStreamOut);
            return bigStreamOut.ToArray();
        }

        /// <summary>
        /// Checks if the byte array is compressed using Gzip compression.
        /// </summary>
        /// <param name="bytes">The byte array to check for compression</param>
        /// <returns></returns>
        public static bool IsCompressed(this byte[] bytes)
        {
            // From http://stackoverflow.com/questions/19364497/how-to-tell-if-a-byte-array-is-gzipped
            return bytes.Length >= 2 && bytes[0] == 31 && bytes[1] == 139;
        }

        /// <summary>
        /// Compresses a byte array using Gzip compression
        /// </summary>
        /// <param name="bytes">The byte array to compress</param>
        /// <returns>The compressed byte array</returns>
        public static byte[] Compress(this byte[] bytes)
        {
            var compressedStream = new MemoryStream();
            using (var stream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                new MemoryStream(bytes).CopyTo(stream);
            }
            return compressedStream.ToArray();
        }
    }
}
