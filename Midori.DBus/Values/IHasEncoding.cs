using System.Text;

namespace Midori.DBus.Values;

public interface IHasEncoding
{
    Encoding Encoding { get; set; }
}
