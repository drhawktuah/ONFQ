using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ONFQ.ONFQ.Config;
using ONFQ.ONFQ.Models;

namespace ONFQ.ONFQ.Utilities;

public static class SpectrumMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        float dot = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        int length = Math.Min(a.Length, b.Length);

        for (int i = 0; i < length; i++)
        {
            float va = a[i];
            float vb = b[i];

            dot += va * vb;
            magnitudeA += va * va;
            magnitudeB += vb * vb;
        }

        float magnitude = magnitudeA * magnitudeB;
        if (magnitude == 0f)
        {
            return 0f;
        }

        return dot / MathF.Sqrt(magnitude);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TranspositionScore(ReadOnlySpan<float> query, ReadOnlySpan<float> candidate)
    {
        if (query.Length != candidate.Length)
        {
            throw new ArgumentException("Vectors must be the same length");
        }

        float sumDiff = 0f;
        float sumMax = 0f;

        for (int i = 0; i < query.Length; i++)
        {
            float a = query[i];
            float b = candidate[i];

            sumDiff += MathF.Abs(a - b);
            sumMax += (a > b) ? a : b;
        }

        if (sumMax == 0)
        {
            return 1f;
        }

        return 1f - (sumDiff / sumMax);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CosineSimilaritySIMD(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        int length = Math.Min(a.Length, b.Length);
        int vectorSize = Vector<float>.Count;

        Vector<float> dot = Vector<float>.Zero;
        Vector<float> magA = Vector<float>.Zero;
        Vector<float> magB = Vector<float>.Zero;

        int i = 0;
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ReadOnlySpan<float> slicedA = a.Slice(i, vectorSize);
            ReadOnlySpan<float> slicedB = b.Slice(i, vectorSize);

            Vector<float> vectorA = MemoryMarshal.Cast<float, Vector<float>>(slicedA)[0];
            Vector<float> vectorB = MemoryMarshal.Cast<float, Vector<float>>(slicedB)[0];

            dot += vectorA * vectorB;
            magA += vectorA * vectorA;
            magB += vectorB * vectorB;

            /*
            Vector<float> vectorA = new(a.Slice(i, vectorSize));
            Vector<float> vectorB = new(b.Slice(i, vectorSize));

            dot += vectorA * vectorB;
            magA += vectorA * vectorA;
            magB += vectorB * vectorB;
            */
        }

        float dotSum = 0;
        float magASum = 0;
        float magBSum = 0;

        for (int j = 0; j < Vector<float>.Count; j++)
        {
            dotSum += dot[j];
            magASum += magA[j];
            magBSum += magB[j];
        }

        for (; i < length; i++)
        {
            float va = a[i];
            float vb = b[i];

            dotSum += va * vb;
            magASum += va * va;
            magBSum += vb * vb;
        }

        float magnitude = magASum * magBSum;
        if (magnitude == 0f)
        {
            return 0f;
        }

        return dotSum / MathF.Sqrt(magnitude);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TranspositionScoreSIMD(ReadOnlySpan<float> query, ReadOnlySpan<float> candidate)
    {
        if (query.Length != candidate.Length)
        {
            throw new ArgumentException("Vectors must be the same length");
        }

        int length = query.Length;
        int vectorSize = Vector<float>.Count;

        Vector<float> sumDiffVec = Vector<float>.Zero;
        Vector<float> sumMaxVec = Vector<float>.Zero;

        int i = 0;
        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<float> vectorA = MemoryMarshal.Cast<float, Vector<float>>(query.Slice(i, vectorSize))[0];
            Vector<float> vectorB = MemoryMarshal.Cast<float, Vector<float>>(candidate.Slice(i, vectorSize))[0];

            sumDiffVec += Vector.Abs(vectorA - vectorB);
            sumMaxVec += Vector.Max(vectorA, vectorB);

            /*
            Vector<float> vQuery = new(query.Slice(i, vectorSize));
            Vector<float> vCandidate = new(candidate.Slice(i, vectorSize));

            sumDiffVec += Vector.Abs(vQuery - vCandidate);
            sumMaxVec += Vector.Max(vQuery, vCandidate);
            */
        }

        float sumDiff = 0f;
        float sumMax = 0f;

        for (int j = 0; j < vectorSize; j++)
        {
            sumDiff += sumDiffVec[j];
            sumMax += sumMaxVec[j];
        }

        for (; i < length; i++)
        {
            float q = query[i];
            float c = candidate[i];

            sumDiff += MathF.Abs(q - c);
            sumMax += (q > c) ? q : c;
        }

        if (sumMax == 0)
        {
            return 1f;
        }

        return 1f - (sumDiff / sumMax);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BlendScores(
        float similarityScore,
        float differenceScore,
        ReadOnlySpan<char> queryText,
        ReadOnlySpan<char> candidateText,
        BlendMode mode,
        float similarityWeight = 0.6f,
        float typoWeight = 0.3f,
        int nGramSize = Constants.Typo.DefaultNGramSize,
        float typoThreshold = Constants.Typo.DefaultTypoThreshold,
        bool applyLengthPenalty = true,
        bool useLogLengthPenalty = false)
    {
        float typoScore = 0f;

        if (mode == BlendMode.TypoBlend)
        {
            typoScore = TypoDetector.JaccardNGramSimilarity(queryText, candidateText, nGramSize);

            if (typoScore < typoThreshold)
            {
                typoWeight = 0f;
            }
        }

        float blendedScore = mode switch
        {
            BlendMode.SimilarityOnly => similarityScore,
            BlendMode.TranspositionOnly => differenceScore,
            BlendMode.CombinedAverage => (similarityScore + differenceScore) / 2f,
            BlendMode.WeightedSimilarityFirst => (similarityScore * similarityWeight) + (differenceScore * (1f - similarityWeight)),
            BlendMode.NonLinearBlend => similarityScore * similarityScore * (1f - MathF.Abs(similarityScore - differenceScore)),
            BlendMode.MaxScore => MathF.Max(similarityScore, differenceScore),
            BlendMode.Minscore => MathF.Min(similarityScore, differenceScore),
            BlendMode.TypoBlend => (similarityScore * similarityWeight) + (differenceScore * (1f - similarityWeight - typoWeight)) + (typoScore * typoWeight),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown blend mode")
        };

        return applyLengthPenalty ? ApplyLengthPenalty(blendedScore, queryText, candidateText, useLogLengthPenalty) : blendedScore;
    }

    private static float ApplyLengthPenalty(float blendedScore, ReadOnlySpan<char> queryText, ReadOnlySpan<char> candidateText, bool useLogPenalty = false)
    {
        if (queryText.IsEmpty || candidateText.IsEmpty)
        {
            return 0f;
        }

        int minLen = Math.Min(queryText.Length, candidateText.Length);
        int maxLen = Math.Max(queryText.Length, candidateText.Length);

        float ratio = useLogPenalty ? MathF.Log2(1f + minLen) / MathF.Log2(1f + maxLen) : (float)minLen / maxLen;

        return blendedScore * ratio;
    }
}