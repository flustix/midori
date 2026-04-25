using System.Linq.Expressions;
using MongoDB.Driver;

namespace Midori.Database.MongoDB;

internal class MongoDatabaseTable<T> : IDatabaseTable<T>
{
    private readonly IMongoCollection<T> collection;

    public MongoDatabaseTable(IMongoDatabase db, string name)
    {
        collection = db.GetCollection<T>(name);
    }

    public void Add(T item) => collection.InsertOne(item);
    public long Count(Expression<Func<T, bool>> match) => collection.CountDocuments(match);
    public IEnumerable<T> Find(Expression<Func<T, bool>> match) => collection.Find(match).ToEnumerable();
    public void Replace(Expression<Func<T, bool>> match, T item) => collection.ReplaceOne(match, item);
    public void Delete(Expression<Func<T, bool>> match) => collection.DeleteOne(match);
    public void DeleteMultiple(Expression<Func<T, bool>> match) => collection.DeleteMany(match);
}
