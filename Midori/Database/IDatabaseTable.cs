using System.Linq.Expressions;

namespace Midori.Database;

public interface IDatabaseTable<T>
{
    void Add(T item);
    long Count(Expression<Func<T, bool>> match);
    IEnumerable<T> Find(Expression<Func<T, bool>> match);
    void Replace(Expression<Func<T, bool>> match, T item);
    void Delete(Expression<Func<T, bool>> match);
    void DeleteMultiple(Expression<Func<T, bool>> match);
}
