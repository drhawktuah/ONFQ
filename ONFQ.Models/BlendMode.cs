using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONFQ.ONFQ.Models;

public enum BlendMode
{
    SimilarityOnly,
    TranspositionOnly,
    CombinedAverage,
    WeightedSimilarityFirst,
    NonLinearBlend,
    MaxScore,
    Minscore,
    TypoBlend
}