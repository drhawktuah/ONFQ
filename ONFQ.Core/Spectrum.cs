using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Jobs;
using ONFQ.ONFQ.Config;
using ONFQ.ONFQ.Models;
using ONFQ.ONFQ.Utilities;

namespace ONFQ.ONFQ.Core;

/*
public class Spectrum<T> where T : notnull
{
    public int Count => spectrums.Count;
    public int VectorSize => Constants.MaxCharCode + 1;

    public IEnumerable<KeyValuePair<T, float[]>> All => spectrums;

    private readonly Dictionary<T, float[]> spectrums = [];
    private readonly Func<T, ReadOnlySpan<char>> func;

    public Spectrum(Func<T, ReadOnlySpan<char>> func)
    {
        this.func = func ?? throw new ArgumentNullException(nameof(func));
    }

    public void Load(T item)
    {
        if (!spectrums.ContainsKey(item))
        {
            ReadOnlySpan<char> span = func(item);

            spectrums[item] = CharMapper.MapChars(span);
        }
    }

    public void Load(IEnumerable<T> values)
    {
        foreach (T item in values)
        {
            Load(item);
        }
    }

    public bool TryGetVector(T key, out float[]? vector)
    {
        return spectrums.TryGetValue(key, out vector);
    }

    public void Clear()
    {
        spectrums.Clear();
    }
}
*/

/*
public class Spectrum<T> where T : notnull
{
    public int Count => spectrums.Count;
    public int VectorSize => FrequencyTable.TableSize;

    public IEnumerable<KeyValuePair<T, Memory<float>>> All => spectrums;

    private readonly Dictionary<T, Memory<float>> spectrums = [];
    private readonly Func<T, ReadOnlySpan<char>> func;

    public Spectrum(Func<T, ReadOnlySpan<char>> func)
    {
        this.func = func ?? throw new ArgumentNullException(nameof(func));
    }

    public void Load(T item)
    {
        if (!spectrums.ContainsKey(item))
        {
            ReadOnlySpan<char> chars = func(item);
            Span<float> buffer = stackalloc float[FrequencyTable.TableSize];

            FrequencyTable table = new(chars, buffer);

            float[] vector = table.Frequencies.ToArray();
            spectrums[item] = new Memory<float>(vector);
        }
    }

    public void Load(IEnumerable<T> values)
    {
        foreach (T item in values)
        {
            Load(item);
        }
    }

    public bool TryGetVector(T key, out Memory<float> vector)
    {
        return spectrums.TryGetValue(key, out vector);
    }

    public void Clear()
    {
        spectrums.Clear();
    }
}
*/

/// <summary>
/// This is called a "spectrum". This ranges from 0 to the largest value (n) in order to cross-match values
/// </summary>
/// <typeparam name="T">The type to store</typeparam>
public class Spectrum<T> where T : notnull
{
    /// <summary>
    /// The amount of spectrums stored
    /// </summary>
    public int Count => spectrums.Count;

    /// <summary>
    /// Vector's max size
    /// </summary>
    public const int VectorSize = Constants.General.MaxCharCode + 1;

    /// <summary>
    /// The keys this spectrum has
    /// </summary>
    public IEnumerable<T> Keys => spectrums.Keys;

    /// <summary>
    /// The values this spectrum has
    /// </summary>
    public Dictionary<T, float[]>.ValueCollection Values => spectrums.Values;

    /// <summary>
    /// The keypair values this spectrum has
    /// </summary>
    public Dictionary<T, float[]> Spectrums => spectrums;

    private readonly Dictionary<T, float[]> spectrums = [];

    /// <summary>
    /// The delegate that converts a string literal to a list of chars efficiently 
    /// </summary>
    private readonly Func<T, ReadOnlySpan<char>> func;

    /// <summary>
    /// Creates a new Spectrum instance with the given mapping delegate
    /// </summary>
    /// <param name="func"></param>
    public Spectrum(Func<T, ReadOnlySpan<char>> func)
    {
        this.func = func ?? throw new ArgumentException(null, nameof(func));
    }

    /// <summary>
    /// Loads an object into the spectrum to iterate over
    /// </summary>
    /// <param name="item"></param>
    public void Load(T item)
    {
        if (spectrums.ContainsKey(item))
        {
            return;
        }

        Span<float> vector = stackalloc float[VectorSize];
        CharMapper.MapChars(func(item), vector);
        spectrums[item] = vector.ToArray();
    }

    public void Load(IEnumerable<T> values)
    {
        foreach (T value in values)
        {
            Load(value);
        }
    }

    /// <summary>
    /// Creates a enumerator to iterate over frequencies
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public FrequencyEnumerator GetFrequencies(T item)
    {
        if (!spectrums.TryGetValue(item, out float[]? vector))
        {
            return new FrequencyEnumerator([]);
        }

        return new FrequencyEnumerator(vector);
    }
}