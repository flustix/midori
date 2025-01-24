using System.Collections;

namespace Midori.Networking;

public class HttpConnectionManager<T> : HttpConnectionManager, IEnumerable<T>
    where T : IHttpModule
{
    public IReadOnlyList<T> Connections => connections;

    private readonly List<T> connections = new();
    private int enumeratorVersion;

    internal override void Add(IHttpModule module)
    {
        enumeratorVersion++;
        connections.Add((T)module);
    }

    internal override void Remove(IHttpModule module)
    {
        enumeratorVersion++;
        connections.Remove((T)module);
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    internal class Enumerator : IEnumerator<T>
    {
        private HttpConnectionManager<T> manager;
        private int index;
        private readonly int version;

        public T Current => manager.connections[index];
        object IEnumerator.Current => Current;

        public Enumerator(HttpConnectionManager<T> manager)
        {
            this.manager = manager;
            index = -1;
            version = manager.enumeratorVersion;
        }

        public bool MoveNext()
        {
            if (version != manager.enumeratorVersion)
                throw new InvalidOperationException("May not modify during enumeration.");

            return ++index < manager.connections.Count;
        }

        public void Reset() => index = -1;
        public void Dispose() => manager = null!;
    }
}

public abstract class HttpConnectionManager
{
    internal abstract void Add(IHttpModule module);
    internal abstract void Remove(IHttpModule module);
}
