namespace BE.ApiGateway.Configuration;

public sealed class GatewayOptions
{
    public const string SECTION_NAME = "Gateway";

    public IReadOnlyList<GatewayRouteConfig> Routes { get; init; } = [];
}

public sealed class GatewayRouteConfig
{
    public required string PathPrefix { get; init; }

    public required string DownstreamBaseUrl { get; init; }

    public required string ServiceName { get; init; }
}
