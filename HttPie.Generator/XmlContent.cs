#nullable enable
using System.Buffers.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace System.Net.Http.Xml;


public class XmlContent : HttpContent
{
    private object _from;
    private XmlSerializer _xmlSerializer;
    private long _length = 0;
    private bool _read;

    private XmlContent(object from, XmlSerializer xmlSerializer)
    {
        _from = from;
        _xmlSerializer = xmlSerializer;
    }

    public static XmlContent Create<T>(T from) where T : class
    {
        return new XmlContent(from, new XmlSerializer(typeof(T)));
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        await Task.Run(() => _xmlSerializer.Serialize(stream, _from));
        _length = stream.Length;
        _read = true;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length;
        return _read;
    }
}

