namespace ONFQ.ONFQ.Models;

public readonly ref struct FrequencyEntry
{
    public readonly int Index;
    public readonly float Value;

    public FrequencyEntry(int index, float value)
    {
        Index = index;
        Value = value;
    }
}

public ref struct FrequencyEnumerator
{
    private readonly ReadOnlySpan<float> frequencies;
    private int index;

    public FrequencyEnumerator(ReadOnlySpan<float> frequencies)
    {
        this.frequencies = frequencies;
        index = -1;
    }

    public bool MoveNext()
    {
        return ++index < frequencies.Length;
    }

    public readonly FrequencyEntry Current
    {
        get
        {
            return new(index, frequencies[index]);
        }
    }

    public readonly FrequencyEnumerator GetEnumerator() => this;
}
public ref struct NonZeroFrequencyEnumerator
{
    private readonly ReadOnlySpan<float> frequencies;
    private int index;

    public NonZeroFrequencyEnumerator(ReadOnlySpan<float> frequencies)
    {
        this.frequencies = frequencies;
        index = -1;
    }

    public bool MoveNext()
    {
        while (++index < frequencies.Length)
        {
            if (frequencies[index] != 0f)
            {
                return true;
            }
        }

        return false;
    }

    public readonly FrequencyEntry Current
    {
        get
        {
            return new(index, frequencies[index]);
        }
    }

    public readonly NonZeroFrequencyEnumerator GetEnumerator() => this;
}

public readonly ref struct FrequencyTable
{
    public const int TableSize = 128;

    public ReadOnlySpan<float> Frequencies => frequencies;
    private readonly ReadOnlySpan<float> frequencies;

    public int Total { get; }

    public float this[char c] => c < TableSize ? frequencies[c] : 0f;

    public FrequencyTable(ReadOnlySpan<char> input, Span<float> buffer)
    {
        if (buffer.Length < TableSize)
        {
            throw new ArgumentException($"Buffer must be atleast {TableSize} floats", nameof(buffer));
        }

        buffer.Clear();

        int total = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c < TableSize)
            {
                buffer[c]++;
                total++;
            }
        }

        if (total > 0)
        {
            int i = 0;

            for (; i <= TableSize - 4; i += 4)
            {
                buffer[i] /= total;
                buffer[i + 1] /= total;
                buffer[i + 2] /= total;
                buffer[i + 3] /= total;
            }

            for (; i < TableSize; i++)
            {
                buffer[i] /= total;
            }
        }

        frequencies = buffer[..TableSize];
        Total = total;
    }

    public void CopyTo(Span<float> destination)
    {
        if (destination.Length < TableSize)
        {
            throw new ArgumentException("Destination span is too small", nameof(destination));
        }

        frequencies.CopyTo(destination);
    }

    public bool TryGetValue(char c, out float value)
    {
        if (c < TableSize)
        {
            value = frequencies[c];
            return true;
        }

        value = 0f;
        return false;
    }

    public float[] CloneFrequencies()
    {
        float[] clone = new float[frequencies.Length];
        frequencies.CopyTo(clone);

        return clone;
    }

    public FrequencyEnumerator GetEnumerator() => new(frequencies);

    public NonZeroFrequencyEnumerator GetNonZeroEnumerator() => new(frequencies);
}