using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace System.Net.Http.Xml
{
    public static class XmlExtensions
    {
        public static async Task<T> ReadFromXmlAsync<T>(this HttpContent content, CancellationToken? token = default)
        {
            return (T)new XmlSerializer(typeof(T)).Deserialize(await content.ReadAsStreamAsync());
        }
    }
}
