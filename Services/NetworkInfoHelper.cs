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
        /// Retrieves the primary physical IPv4 address of the active network interface on this machine.
        /// </summary>
        public static string GetLocalPrimaryIpAddress()
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

                var primary = nics.FirstOrDefault(n => n.GetIPProperties().GatewayAddresses.Any())
                           ?? nics.FirstOrDefault(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                           ?? nics.FirstOrDefault(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                           ?? nics.FirstOrDefault();

                if (primary != null)
                {
                    var ip = primary.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ip != null)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return "127.0.0.1";
        }

        /// <summary>
        /// Determines whether the specified IP or remote connection is originating from the local host machine itself.
        /// </summary>
        public static bool IsLocalHost(IPAddress? remoteIp)
        {
            if (remoteIp == null) return false;
            if (IPAddress.IsLoopback(remoteIp) || remoteIp.ToString() == "127.0.0.1" || remoteIp.ToString() == "::1")
            {
                return true;
            }

            var str = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();
            if (str == GetLocalPrimaryIpAddress())
            {
                return true;
            }

            try
            {
                var localInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in localInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    if (ipProps.UnicastAddresses.Any(u => u.Address.ToString() == str))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Retrieves the client's IPv4 or IPv6 address from the current HTTP request.
        /// Accounts for X-Forwarded-For headers if behind a reverse proxy.
        /// When connecting locally via loopback (127.0.0.1 / ::1), resolves to the host machine's primary LAN IP.
        /// </summary>
        public static string GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return GetLocalPrimaryIpAddress();

            try
            {
                string? forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedHeader))
                {
                    var split = forwardedHeader.Split(',');
                    if (split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
                    {
                        var candidate = split[0].Trim();
                        if (candidate == "::1" || candidate == "127.0.0.1") return GetLocalPrimaryIpAddress();
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
                    if (str == "::1" || str == "127.0.0.1")
                    {
                        return GetLocalPrimaryIpAddress();
                    }
                    return str;
                }
            }
            catch
            {
                // Fallback to primary local IP
            }

            return GetLocalPrimaryIpAddress();
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

                // If loopback or local host / local domain
                if (clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == GetLocalPrimaryIpAddress() || clientIp.Equals("localhost", StringComparison.OrdinalIgnoreCase) || clientIp.Equals("solvis.local", StringComparison.OrdinalIgnoreCase))
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

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _deviceCache = new();

        /// <summary>
        /// Queries the NetBIOS Node Status (UDP 137, RFC 1002) of a LAN IP to retrieve the Windows computer name.
        /// </summary>
        public static string? ResolveNetBiosName(string ipAddress, int timeoutMs = 500)
        {
            try
            {
                if (!IPAddress.TryParse(ipAddress, out var ip) || IPAddress.IsLoopback(ip))
                    return null;

                using var client = new System.Net.Sockets.UdpClient();
                client.Client.ReceiveTimeout = timeoutMs;
                client.Client.SendTimeout = timeoutMs;

                // RFC 1002 NetBIOS Node Status query for wildcard * (half-ASCII CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA)
                byte[] query = new byte[50];
                query[0] = 0x82; query[1] = 0x28; // Transaction ID
                query[4] = 0x00; query[5] = 0x01; // Questions = 1
                query[12] = 0x20; // Length = 32
                System.Text.Encoding.ASCII.GetBytes("CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA").CopyTo(query, 13);
                query[45] = 0x00; // Null terminator
                query[46] = 0x00; query[47] = 0x21; // Type = NBSTAT (0x0021)
                query[48] = 0x00; query[49] = 0x01; // Class = IN (0x0001)

                var endpoint = new IPEndPoint(ip, 137);
                client.Send(query, query.Length, endpoint);

                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = client.Receive(ref remoteEp);

                if (response.Length >= 57)
                {
                    int numNames = response[56];
                    for (int i = 0; i < numNames; i++)
                    {
                        int offset = 57 + (i * 18);
                        if (offset + 18 <= response.Length)
                        {
                            byte type = response[offset + 15];
                            byte flags = response[offset + 16];
                            bool isGroup = (flags & 0x80) != 0;
                            // Type 0x00 or 0x20 with unique flag indicates workstation / computer name
                            if (!isGroup && (type == 0x00 || type == 0x20))
                            {
                                string name = System.Text.Encoding.ASCII.GetString(response, offset, 15).Trim('\0', ' ');
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    return name;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // NetBIOS query timed out or firewall
            }

            return null;
        }

        /// <summary>
        /// Resolves a human-readable client device/phone name based on User-Agent, OS, NetBIOS, and client IP.
        /// Used for non-Windows or mobile devices connecting over LAN without Windows Domain SSO.
        /// </summary>
        public static string GetClientDeviceName(HttpContext? httpContext)
        {
            if (httpContext == null) return "Unknown-Device";

            var clientIp = GetClientIpAddress(httpContext);

            // Only return local machine name if the actual client IP is local loopback
            if (clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == GetLocalPrimaryIpAddress())
            {
                return Environment.MachineName;
            }

            if (_deviceCache.TryGetValue(clientIp, out var cachedName))
            {
                return cachedName;
            }

            // 1. Attempt reverse DNS resolution on intranet LAN
            try
            {
                if (IPAddress.TryParse(clientIp, out var parsedIp) && !IPAddress.IsLoopback(parsedIp))
                {
                    var hostEntry = Dns.GetHostEntry(parsedIp);
                    if (!string.IsNullOrWhiteSpace(hostEntry.HostName))
                    {
                        var shortName = hostEntry.HostName.Split('.').First().Trim();
                        if (!string.IsNullOrWhiteSpace(shortName) && shortName != clientIp)
                        {
                            _deviceCache[clientIp] = shortName;
                            return shortName;
                        }
                    }
                }
            }
            catch { }

            // 2. Attempt NetBIOS Node Status query (UDP 137) for Windows workstations on LAN
            try
            {
                var netBiosName = ResolveNetBiosName(clientIp, 600);
                if (!string.IsNullOrWhiteSpace(netBiosName))
                {
                    _deviceCache[clientIp] = netBiosName;
                    return netBiosName;
                }
            }
            catch { }

            var lastOctet = clientIp.Split('.').Last();
            string resolved;

            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                resolved = $"Device-{lastOctet}";
            }
            else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            {
                resolved = $"iPhone-{lastOctet}";
            }
            else if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            {
                resolved = $"iPad-{lastOctet}";
            }
            else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(userAgent, @"Android[^;]*;\s*([^;)]+)");
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    var model = match.Groups[1].Value.Trim();
                    if (model.Contains("Build", StringComparison.OrdinalIgnoreCase))
                    {
                        model = model.Split("Build")[0].Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        model = System.Text.RegularExpressions.Regex.Replace(model, @"[^\w\-\.]", "");
                        resolved = $"{model}-{lastOctet}";
                    }
                    else
                    {
                        resolved = $"Android-{lastOctet}";
                    }
                }
                else
                {
                    resolved = $"Android-{lastOctet}";
                }
            }
            else if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
            {
                resolved = $"Mac-{lastOctet}";
            }
            else if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            {
                resolved = $"PC-{lastOctet}";
            }
            else
            {
                resolved = $"Device-{lastOctet}";
            }

            _deviceCache[clientIp] = resolved;
            return resolved;
        }
    }
}
