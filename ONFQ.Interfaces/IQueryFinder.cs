using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ONFQ.ONFQ.Interfaces;

public interface IQueryFinder<T> where T : notnull
{
    T? FindBestMatch(T query);
    Dictionary<T, float> FindSimilarMatches(T query);
    void Build(IEnumerable<T> items);
}