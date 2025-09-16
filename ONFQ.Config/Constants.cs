using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONFQ.ONFQ.Config;

public static class Constants
{
    public static class General
    {
        /// <summary>
        /// The max ASCII char value for this algorithm.
        /// </summary>
        public const int MaxCharCode = 127;

        /// <summary>
        /// The max value (for the best results)
        /// </summary>
        public const float MaxFloatValue = 1f;

        public const int MaxLength = 256;
    }

    public static class Typo
    {
        /// <summary>
        /// Default size of character n-grams used for comparison.
        /// </summary>
        public const int DefaultNGramSize = 2;

        /// <summary>
        /// Default threshold for typo detection.
        /// A score >= this is considered a probable typo match.
        /// </summary>
        public const float DefaultTypoThreshold = 0.5f;
    }
}