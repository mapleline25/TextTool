// Copyright (c) Victor J. Duvanenko
// Copyright (c) mapleline25
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License. 

// Forked and adapted from HPCsharp (DragonSpit/HPCsharp).
// See: https://github.com/DragonSpit/HPCsharp/blob/master/HPCsharp/MergeSortParallel.cs; and
//      https://github.com/DragonSpit/HPCsharp/blob/master/HPCsharp/MergeParallel.cs
//
// Summary of changes:
//
// 1. The file modifies HPCsharp.ParallelAlgorithm.SortMergePar<T>(this T[] src, int startIndex, int length, IComparer<T> comparer, Int32 parallelThreshold, Int32 parallelMergeThreshold)
//    to provide a 'dst' array parameter for holding the output sorted array, and to use the src array to perform the sorting to avoid unnecessary array allocation.
//
// 2. The file copies/modifies ParallelAlgorithm.SortMergeInnerPar<T>() and ParallelAlgorithm.MergeInnerFasterPar<T>() from HPCsharp
//    to fit the modified SortMergePar<T>().
//
// Details of changes are listed before the summary of each method as the following.

using HPCsharp;

namespace TextTool;

public static class ParallelAlgorithm
{
    // Change details:
    // The method is forked and modified from HPCsharp.ParallelAlgorithm.SortMergePar<T>(this T[] src, int startIndex, int length, IComparer<T> comparer, Int32 parallelThreshold, Int32 parallelMergeThreshold).
    // It is modified to return void and accept a 'dst' array parameter for holding the output sorted array,
    // therefore the caller can use a buffer array (ex: array from ArrayPool<T>) as the parameter of this method.
    // The src array is directly sorted by SortMergeInnerPar() by using adjusted indexes and offset,
    // so there is no need to use the 'srcTrimmed' array in the original version for sorting and the memory allocation of srcTrimmed array can be avoided.
    /// <summary>
    /// Parallel Merge Sort. Takes a range of the src array, sorts it, and then write the sorted range into the specified dst array.
    /// </summary>
    /// <typeparam name="T">array of type T</typeparam>
    /// <param name="src">source array</param>
    /// <param name="startIndex">index within the src array where sorting starts</param>
    /// <param name="length">number of elements starting with startIndex to be sorted</param>
    /// <param name="dst">destination array</param>
    /// <param name="comparer">comparer used to compare two array elements of type T</param>
    /// <param name="parallelThreshold">arrays larger than this value will be sorted using multiple cores</param>
    public static void SortMergePar<T>(this T[] src, int startIndex, int length, T[] dst, IComparer<T> comparer = null, int parallelThreshold = 24 * 1024, int parallelMergeThreshold = 128 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startIndex, 0, nameof(startIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, dst.Length, nameof(length));

        int left = startIndex;
        int right = startIndex + length - 1;
        
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(right, src.Length, nameof(length));
        
        // The src array starts at 'left' and the dst array starts at '0', therefore an offest '-left' should be used to map the left index of src array to the index '0' of dst array.
        src.SortMergeInnerPar(left, right, dst, -left, true, comparer, parallelThreshold, parallelMergeThreshold);
    }

    // This method is forked and modified from HPCsharp.ParallelAlgorithm.SortMergeInnerPar<T>().
    // It is modified to accept a 'offest' index of dst array, which is used to adjust the indexes of src array to map to the right range of dst array.
    // The adjusted indexes of dst array are passed to MergeInnerFasterPar<T>() as the right starting and ending indexes.
    /// <summary>
    /// Parallel Merge Sort that is not-in-place. Also, not stable, since Array.Sort is not stable, and is used as the recursion base-case.
    /// </summary>
    /// <typeparam name="T">data type of each array element</typeparam>
    /// <param name="src">source array</param>
    /// <param name="l">left  index of the source array, inclusive</param>
    /// <param name="r">right index of the source array, inclusive</param>
    /// <param name="dst">destination array</param>
    /// <param name="offset">offset for mapping the sorting range of source array to the range of the destination array</param>
    /// <param name="srcToDst">true => destination array will hold the sorted array; false => source array will hold the sorted array</param>
    /// <param name="comparer">method to compare array elements</param>
    /// <param name ="parallelThreshold">arrays larger than this value will be sorted using multiple cores</param>
    private static void SortMergeInnerPar<T>(this T[] src, Int32 l, Int32 r, T[] dst, Int32 offset, bool srcToDst = true, IComparer<T> comparer = null,
                                             Int32 parallelThreshold = 24 * 1024, Int32 parallelMergeThreshold = 128 * 1024)
    {
        if (r < l) return;
        if (r == l)
        {    // termination/base case of sorting a single element
            if (srcToDst) dst[l] = src[l];    // copy the single element from src to dst
            return;
        }
        //// TODO: This threshold may not be needed as C# Array.Sort already implements Insertion Sort and Heap Sort, with thresholds for each
        if ((r - l) <= HPCsharp.ParallelAlgorithm.SortMergeParallelInsertionThreshold)
        {
            Algorithm.InsertionSort<T>(src, l, r - l + 1, comparer);  // want to do dstToSrc, can just do it in-place, just sort the src, no need to copy
            if (srcToDst)
                Array.Copy(src, l, dst, l + offset, r - l + 1);
            return;
        }
        if ((r - l) <= parallelThreshold)
        {
            Array.Sort<T>(src, l, r - l + 1, comparer);     // not a stable sort
            if (srcToDst)
                Array.Copy(src, l, dst, l + offset, r - l + 1);
            return;
        }
        int m = r / 2 + l / 2 + (r % 2 + l % 2) / 2;    // (l + r) / 2 without overflow or underflow
        Parallel.Invoke(
            () => { SortMergeInnerPar<T>(src, l,     m, dst, offset, !srcToDst, comparer, parallelThreshold); },      // reverse direction of srcToDst for the next level of recursion
            () => { SortMergeInnerPar<T>(src, m + 1, r, dst, offset, !srcToDst, comparer, parallelThreshold); }
        );
        // reverse direction of srcToDst for the next level of recursion
        if (srcToDst) MergeInnerFasterPar<T>(src, l,          m,          m + 1,          r,          dst, l + offset, comparer, parallelMergeThreshold);
        else          MergeInnerFasterPar<T>(dst, l + offset, m + offset, m + 1 + offset, r + offset, src, l,          comparer, parallelMergeThreshold);
        //if (srcToDst) HPCsharp.Algorithm.MergeFaster<T>(src, l, m - l + 1, src, m + 1, r - (m + 1) + 1, dst, l, comparer);    // Usefull to see how much speedup is gained from using Parallel Merge
        //else          HPCsharp.Algorithm.MergeFaster<T>(dst, l, m - l + 1, dst, m + 1, r - (m + 1) + 1, src, l, comparer);
    }

    // This method is copied from HPCsharp.ParallelAlgorithm and is identical to the original.
    // It is an internal method used by SortMergeInnerPar<T>() and cannot be directly called in this project,
    // therefore it is copied into this file to be used with SortMergeInnerPar<T>().
    /// <summary>
    /// Divide-and-Conquer Merge of two ranges of source array src[ p1 .. r1 ] and src[ p2 .. r2 ] into destination array starting at index p3.
    /// </summary>
    /// <typeparam name="T">data type of each array element</typeparam>
    /// <param name="src">source array</param>
    /// <param name="p1">starting index of the first  segment, inclusive</param>
    /// <param name="r1">ending   index of the first  segment, inclusive</param>
    /// <param name="p2">starting index of the second segment, inclusive</param>
    /// <param name="r2">ending   index of the second segment, inclusive</param>
    /// <param name="dst">destination array</param>
    /// <param name="p3">starting index of the result</param>
    /// <param name="comparer">method to compare array elements</param>
    private static void MergeInnerFasterPar<T>(T[] src, Int32 p1, Int32 r1, Int32 p2, Int32 r2, T[] dst, Int32 p3, IComparer<T> comparer = null, Int32 mergeParallelThreshold = 128 * 1024)
    {
        //Console.WriteLine("#1 " + p1 + " " + r1 + " " + p2 + " " + r2);
        Int32 length1 = r1 - p1 + 1;
        Int32 length2 = r2 - p2 + 1;
        if (length1 < length2)
        {
            Algorithm.Swap(ref p1, ref p2);
            Algorithm.Swap(ref r1, ref r2);
            Algorithm.Swap(ref length1, ref length2);
        }
        if (length1 == 0) return;
        if ((length1 + length2) <= mergeParallelThreshold)
        {
            //Console.WriteLine("#3 " + p1 + " " + length1 + " " + p2 + " " + length2 + " " + p3);
            Algorithm.MergeFaster<T>(src, p1, length1,
                                     src, p2, length2,
                                     dst, p3, comparer);
        }
        else
        {
            Int32 q1 = p1 / 2 + r1 / 2 + (p1 % 2 + r1 % 2) / 2;                // (p1 + r1) / 2 without overflow
            Int32 q2 = Algorithm.BinarySearch(src[q1], src, p2, r2, comparer);
            Int32 q3 = p3 + (q1 - p1) + (q2 - p2);
            dst[q3] = src[q1];
            Parallel.Invoke(
                () => { MergeInnerFasterPar<T>(src, p1, q1 - 1, p2, q2 - 1, dst, p3, comparer, mergeParallelThreshold); },
                () => { MergeInnerFasterPar<T>(src, q1 + 1, r1, q2, r2, dst, q3 + 1, comparer, mergeParallelThreshold); }
            );
        }
    }
}
