using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ONFQ.ONFQ.Config;

namespace ONFQ.ONFQ.Utilities;

public static class TypoDetector
{
    public static float JaccardNGramSimilarity(ReadOnlySpan<char> query, ReadOnlySpan<char> candidate, int ngramSize = Constants.Typo.DefaultNGramSize)
    {
        int queryCount = Math.Max(0, query.Length - ngramSize + 1);
        int candidateCount = Math.Max(0, candidate.Length - ngramSize + 1);

        if (queryCount == 0 || candidateCount == 0)
        {
            return 0f;
        }

        Span<int> queryHashes = stackalloc int[queryCount];
        Span<int> candidateHashes = stackalloc int[candidateCount];

        for (int i = 0; i < queryCount; i++)
        {
            queryHashes[i] = HashNGram(query.Slice(i, ngramSize));
        }

        for (int i = 0; i < candidateCount; i++)
        {
            candidateHashes[i] = HashNGram(candidate.Slice(i, ngramSize));
        }

        int intersectionCount = 0;

        Span<bool> matchedCandidate = stackalloc bool[candidateCount];
        matchedCandidate.Clear();

        for (int i = 0; i < queryCount; i++)
        {
            int hash = queryHashes[i];
            for (int j = 0; j < candidateCount; j++)
            {
                if (!matchedCandidate[j] && candidateHashes[j] == hash)
                {
                    intersectionCount++;
                    matchedCandidate[j] = true;

                    break;
                }
            }
        }

        int unionCount = queryCount + candidateCount - intersectionCount;

        if (unionCount == 0)
        {
            return 0f;
        }

        return (float)intersectionCount / unionCount;
    }

    private static int HashNGram(ReadOnlySpan<char> readOnlySpan)
    {
        unchecked
        {
            int hash = 17;

            for (int i = 0; i < readOnlySpan.Length; i++)
            {
                hash = hash * 31 + readOnlySpan[i];
            }

            return hash;
        }
    }
}