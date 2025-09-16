using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ONFQ.ONFQ.Config;
using ONFQ.ONFQ.Interfaces;
using ONFQ.ONFQ.Models;
using ONFQ.ONFQ.Utilities;

namespace ONFQ.ONFQ.Core;

public class SpectrumFinder<T> where T : notnull
{
    private readonly Spectrum<T> spectrum;
    private readonly float threshold;
    private readonly Func<T, ReadOnlySpan<char>> func;

    private readonly BlendMode mode;

    private readonly Cache<T, float[]> vectorCache;

    public SpectrumFinder(Func<T, ReadOnlySpan<char>> func, BlendMode mode = BlendMode.SimilarityOnly, float threshold = 0.7f)
    {
        this.func = func ?? throw new ArgumentNullException(nameof(func));
        this.threshold = threshold;
        this.mode = mode;

        spectrum = new Spectrum<T>(func);

        vectorCache = new Cache<T, float[]>(key =>
        {
            Span<float> vector = stackalloc float[Constants.General.MaxCharCode + 1];
            CharMapper.MapChars(func(key), vector);

            return vector.ToArray();
        });
    }

    public void Build(IEnumerable<T> values) => spectrum.Load(values);

    /*
    public T? FindBestMatch(T query)
    {
        ReadOnlySpan<float> queryVector = GetVector(query);

        T? bestMatch = default;
        float bestScore = float.NegativeInfinity;

        foreach (var (key, value) in spectrum.Spectrums)
        {
            if (key.Equals(query))
            {
                continue;
            }

            float score = ComputeScore(queryVector, value, func(query), func(key));

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = key;
            }
        }

        return bestMatch;
    }
    */

    /*
    public T? FindBestMatch(T query)
    {
        if (!spectrum.Spectrums.TryGetValue(query, out var queryVector))
        {
            var span = func(query);

            Span<float> vector = stackalloc float[Constants.General.MaxCharCode + 1];
            CharMapper.MapChars(func(query), vector);

            queryVector = vector.ToArray();
        }

        ReadOnlySpan<float> querySpan = queryVector;

        T? bestMatch = default;
        float bestScore = float.NegativeInfinity;

        foreach (var (key, value) in spectrum.Spectrums)
        {
            if (key.Equals(query))
            {
                continue;
            }

            ReadOnlySpan<float> candidateVector = value;

            float score = ComputeScore(querySpan, candidateVector);

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = key;
            }
        }

        return bestMatch;
    }
    */

    public T? FindBestMatch(T query)
    {
        ReadOnlySpan<float> queryVector = GetVector(query);
        ReadOnlySpan<char> queryText = func(query);

        const int MaxLength = 256;

        if (queryText.Length > MaxLength)
        {
            throw new ArgumentException("Query text is too long");
        }

        Span<char> normalizedQuery = stackalloc char[MaxLength];
        Span<char> normalizedCandidate = stackalloc char[MaxLength];

        CharMapper.NormalizeText(queryText, normalizedQuery[..queryText.Length]);

        T? bestMatch = default;
        float bestScore = float.NegativeInfinity;

        foreach (var (key, candidateVector) in spectrum.Spectrums)
        {
            if (key.Equals(query))
            {
                bestScore = 1.0f;
                bestMatch = query;

                break;
            }

            ReadOnlySpan<char> candidateText = func(key);

            if (candidateText.Length > MaxLength)
            {
                continue;
            }

            CharMapper.NormalizeText(candidateText, normalizedCandidate[..candidateText.Length]);

            float score = ComputeScore(queryVector, candidateVector, normalizedQuery[..queryText.Length], normalizedCandidate[..candidateText.Length]);

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = key;
            }
        }

        return bestMatch;
    }

    public Dictionary<T, float> FindSimilarMatches(T query)
    {
        ReadOnlySpan<float> queryVector = GetVector(query);
        ReadOnlySpan<char> queryText = func(query);

        if (queryText.Length > Constants.General.MaxLength)
        {
            return [];
        }

        Span<char> normalizedQuery = stackalloc char[Constants.General.MaxLength];
        CharMapper.NormalizeText(queryText, normalizedQuery[..queryText.Length]);

        Span<char> normalizedCandidate = stackalloc char[Constants.General.MaxLength];

        Dictionary<T, float> results = [];

        foreach (var (key, candidateVector) in spectrum.Spectrums)
        {
            if (key.Equals(query))
            {
                results[key] = 1.0f;

                break;
            }

            ReadOnlySpan<char> candidateText = func(key);

            if (candidateText.Length > Constants.General.MaxLength)
            {
                continue;
            }

            CharMapper.NormalizeText(candidateText, normalizedCandidate[..candidateText.Length]);

            float similarity = ComputeScore(queryVector, candidateVector, normalizedQuery[..queryText.Length], normalizedCandidate[..candidateText.Length]);

            if (similarity >= threshold)
            {
                results[key] = similarity;
            }
        }

        return results;
    }

    /*
    public Dictionary<T, float> FindSimilarMatches(T query)
    {
        if (!spectrum.Spectrums.TryGetValue(query, out float[]? queryVector))
        {
            var span = func(query);

            Span<float> vector = stackalloc float[Constants.General.MaxCharCode + 1];
            CharMapper.MapChars(func(query), vector);

            queryVector = vector.ToArray();
        }

        ReadOnlySpan<float> querySpan = queryVector;

        Dictionary<T, float> results = [];

        foreach (var (key, value) in spectrum.Spectrums)
        {
            if (key.Equals(query))
            {
                continue;
            }

            float similarity = ComputeScore(queryVector, value);

            if (similarity >= threshold)
            {
                results[key] = similarity;
            }
        }

        return results;
    }
    */

    private ReadOnlySpan<float> GetVector(T key)
    {
        if (spectrum.Spectrums.TryGetValue(key, out float[]? vector))
        {
            return vector;
        }
        else
        {
            return vectorCache.GetOrAdd(key);
        }
    }

    private float ComputeScore(ReadOnlySpan<float> queryVector, ReadOnlySpan<float> candidateVector, ReadOnlySpan<char> queryText, ReadOnlySpan<char> candidateText)
    {
        float similarity = SpectrumMath.CosineSimilaritySIMD(queryVector, candidateVector);
        float transposition = SpectrumMath.TranspositionScoreSIMD(queryVector, candidateVector);

        return SpectrumMath.BlendScores(similarity, transposition, queryText, candidateText, mode);
    }
}