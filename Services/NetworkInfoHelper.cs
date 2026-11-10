using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;

namespace StockPortal.Services
{
    public static class NetworkInfoHelper
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhysAddrLen);

        /// <summary>
        /// Retrieves the client's IPv4 or IPv6 address from the current HTTP request.
        /// Accounts for X-Forwarded-For headers if behind a reverse proxy.
        /// </summary>
        public static string GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return "127.0.0.1";

            try
            {
                string? forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedHeader))
                {
                    var split = forwardedHeader.Split(',');
                    if (split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
                    {
                        var candidate = split[0].Trim();
                        if (candidate == "::1") return "127.0.0.1";
                        return candidate;
                    }
                }

                var remoteIp = httpContext.Connection.RemoteIpAddress;
                if (remoteIp != null)
                {
                    if (remoteIp.IsIPv4MappedToIPv6)
                    {
                        remoteIp = remoteIp.MapToIPv4();
                    }
                    string str = remoteIp.ToString();
                    if (str == "::1") return "127.0.0.1";
                    return str;
                }
            }
            catch
            {
                // Fallback to loopback
            }

            return "127.0.0.1";
        }

        /// <summary>
        /// Resolves the physical MAC address of the client machine.
        /// In intranet environments, utilizes SendARP. If running locally/loopback, queries active host network adapters.
        /// </summary>
        public static string GetClientMacAddress(HttpContext? httpContext, string? clientIp = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clientIp))
                {
                    clientIp = GetClientIpAddress(httpContext);
                }

                // If loopback or local host
                if (clientIp == "127.0.0.1" || clientIp == "::1" || clientIp.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return GetLocalPrimaryMacAddress();
                }

                // If on local intranet, attempt SendARP
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    IPAddress.TryParse(clientIp, out var parsedIp) &&
                    parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    byte[] macBytes = new byte[6];
                    uint len = (uint)macBytes.Length;
                    #pragma warning disable CS0618
                    int destIp = (int)parsedIp.Address;
                    #pragma warning restore CS0618

                    int arpResult = SendARP(destIp, 0, macBytes, ref len);
                    if (arpResult == 0 && len == 6)
                    {
                        return string.Join(":", macBytes.Select(b => b.ToString("X2")));
                    }
                }

                // Check if the IP matches any local network interface IP on this machine
                var localInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in localInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    if (ipProps.UnicastAddresses.Any(u => u.Address.ToString() == clientIp))
                    {
                        var mac = ni.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
                        {
                            return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                        }
                    }
                }

                // Fallback if SendARP returned no route (e.g. dev workstation)
                return GetLocalPrimaryMacAddress();
            }
            catch
            {
                return "N/A";
            }
        }

        /// <summary>
        /// Retrieves the primary physical MAC address of the active local network interface.
        /// </summary>
        public static string GetLocalPrimaryMacAddress()
        {
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                                !n.Description.Contains("Pseudo", StringComparison.OrdinalIgnoreCase) &&
                                !n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                                !n.Description.Contains("WSL", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var primary = nics.FirstOrDefault(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                           ?? nics.FirstOrDefault(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                           ?? nics.FirstOrDefault();

                if (primary != null)
                {
                    var mac = primary.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
                    {
                        return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                    }
                }
            }
            catch
            {
                // Ignore network query errors
            }

            return "N/A";
        }
    }
}
