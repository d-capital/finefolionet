using System.Net;

namespace Finefolio.ValuationApi.RateLimiting;

public readonly record struct RateLimitPartitionKey(string Key, bool IsExempt);

public static class RateLimitClientAddress
{
    private static readonly IPNetwork[] PrivateNetworks =
    [
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("fc00::/7"),
        IPNetwork.Parse("fe80::/10")
    ];

    public static RateLimitPartitionKey Resolve(
        HttpContext context,
        IReadOnlyList<IPNetwork> exemptNetworks)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exemptNetworks);

        var remoteAddress = Normalize(context.Connection.RemoteIpAddress);
        if (remoteAddress is null)
        {
            return new RateLimitPartitionKey("unknown-client", false);
        }

        if (IPAddress.IsLoopback(remoteAddress))
        {
            return new RateLimitPartitionKey($"local:{remoteAddress}", true);
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (IsPrivateAddress(remoteAddress) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstForwardedAddress = forwardedFor
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (IPAddress.TryParse(firstForwardedAddress, out var forwardedAddress))
            {
                forwardedAddress = Normalize(forwardedAddress)!;
                return IsExempt(forwardedAddress, exemptNetworks)
                    ? new RateLimitPartitionKey($"internal:{forwardedAddress}", true)
                    : new RateLimitPartitionKey($"external:{forwardedAddress}", false);
            }

            return new RateLimitPartitionKey($"invalid-forwarded:{remoteAddress}", false);
        }

        return IsExempt(remoteAddress, exemptNetworks)
            ? new RateLimitPartitionKey($"internal:{remoteAddress}", true)
            : new RateLimitPartitionKey($"external:{remoteAddress}", false);
    }

    private static IPAddress? Normalize(IPAddress? address) =>
        address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address;

    private static bool IsPrivateAddress(IPAddress address) =>
        PrivateNetworks.Any(network => network.Contains(address));

    private static bool IsExempt(IPAddress address, IReadOnlyList<IPNetwork> exemptNetworks) =>
        exemptNetworks.Any(network => network.Contains(address));
}
