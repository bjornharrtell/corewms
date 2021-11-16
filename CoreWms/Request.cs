namespace CoreWms;

public enum Version
{
    v1_3_0,
    v1_1_0
}

public enum RequestEnum
{
    GetCapabilities,
    GetMap
}

public abstract class Request
{
    public Version Version { get; set; }
    public RequestEnum RequestEnum { get; set; }

    public void Parse(string service, string version, string request)
    {
        if (service != "WMS")
            throw new Exception($"Service {service} is not supported");
        Version = ParseVersion(version);
        RequestEnum = ParseRequest(request);
    }

    private Version ParseVersion(string version)
    {
        if (version == "1.3.0")
            return Version.v1_3_0;
        else if (version == "1.1.0")
            return Version.v1_1_0;
        else
            return Version.v1_3_0;
        throw new Exception($"Version {version} is not supported");
    }

    private RequestEnum ParseRequest(string request)
    {
        if (request == "GetMap")
            return RequestEnum.GetMap;
        throw new Exception($"Request {request} is not supported");
    }
}
