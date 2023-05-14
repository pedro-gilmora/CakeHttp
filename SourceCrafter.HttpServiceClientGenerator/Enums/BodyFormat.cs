namespace SourceCrafter.HttpServiceClient.Enums;

public enum BodyFormat : byte
{
    Any = 0,
    Json = 1,
    Xml = 2,
    MultiPartFormData = 4,
    FormUrlEncoded = 8
}
