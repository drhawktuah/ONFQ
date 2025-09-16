using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONFQ.ONFQ.Interfaces;

public interface IQueryFinder<T> where T : notnull
{
    T? FindBestMatch(T query, float threshold = 0.75f);
    IEnumerable<(T Item, float Score)> FindSimilarMatches(T query, float threshold = 0.75f);

    void Build(IEnumerable<T> items);
}