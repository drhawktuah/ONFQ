using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ONFQ.ONFQ.Config;

namespace ONFQ.ONFQ.Utilities;

public static class CharMapper
{
    public static Dictionary<char, float> Mapping => GenerateTable();

    private static readonly Dictionary<char, char> substitutions = new()
    {
        ['0'] = 'o',
        ['1'] = 'l',
        ['2'] = 'z',
        ['3'] = 'e',
        ['4'] = 'a',
        ['5'] = 's',
        ['6'] = 'g',
        ['7'] = 't',
        ['8'] = 'b',
        ['9'] = 'g',
        ['@'] = 'a',
        ['$'] = 's',
        ['!'] = 'i',
        ['+'] = 't',
        ['%'] = 'x',
        ['^'] = 'v',
        ['&'] = 'n'
    };

    /// <summary>
    /// Maps each character in the input span to a float using a custom mapping dictionary. If a char is not in the dictionary, default mapping is used
    /// </summary>
    public static float[] MapChars(ReadOnlySpan<char> input)
    {
        float[] mapped = new float[Constants.General.MaxCharCode + 1];

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            mapped[i] = c <= Constants.General.MaxCharCode ? c / (float)Constants.General.MaxCharCode : 0f;
        }

        return mapped;
    }

    /// <summary>
    /// Maps each character in the input span to a float using a custom mapping dictionary. If a char is not in the dictionary, default mapping is used
    /// </summary>
    public static void MapChars(ReadOnlySpan<char> input, Span<float> output)
    {
        output.Clear();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (Mapping.TryGetValue(c, out float mappedValue))
            {
                output[i] = mappedValue;
            }
            else
            {
                output[i] = 1f;
            }
        }
    }

    /// <summary>
    /// Creates a table of chars
    /// </summary>
    /// <returns></returns>
    public static Dictionary<char, float> GenerateTable()
    {
        var table = new Dictionary<char, float>(Constants.General.MaxCharCode + 1);

        for (char c = (char)0; c <= Constants.General.MaxCharCode; c++)
        {
            table[c] = c / (float)Constants.General.MaxCharCode;
        }

        return table;
    }

    public static void NormalizeText(ReadOnlySpan<char> input, Span<char> output)
    {
        if (output.Length < input.Length)
        {
            throw new ArgumentException("Output buffer too small");
        }

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            output[i] = substitutions.TryGetValue(c, out char replacement) ? replacement : c;
        }
    }
}