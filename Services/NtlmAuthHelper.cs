using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StockPortal.Services
{
    public static class NtlmAuthHelper
    {
        private static readonly byte[] NtlmSignature = Encoding.ASCII.GetBytes("NTLMSSP\0");

        public static int FindNtlmSignature(byte[] data)
        {
            if (data == null || data.Length < 8) return -1;
            for (int i = 0; i <= data.Length - 8; i++)
            {
                bool match = true;
                for (int j = 0; j < 8; j++)
                {
                    if (data[i + j] != NtlmSignature[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        public static bool IsType1(byte[] message)
        {
            if (message.Length < 16) return false;
            for (int i = 0; i < 8; i++)
            {
                if (message[i] != NtlmSignature[i]) return false;
            }
            int msgType = BitConverter.ToInt32(message, 8);
            return msgType == 1;
        }

        public static bool IsType3(byte[] message)
        {
            if (message.Length < 52) return false;
            for (int i = 0; i < 8; i++)
            {
                if (message[i] != NtlmSignature[i]) return false;
            }
            int msgType = BitConverter.ToInt32(message, 8);
            return msgType == 3;
        }

        /// <summary>
        /// Generates a fully compliant NTLMv2 Type 2 Challenge Message with Target Info (AV_PAIRS).
        /// Required by modern Windows SSPI clients (Windows 10/11) to avoid 0x80090302 SEC_E_UNSUPPORTED_FUNCTION.
        /// </summary>
        public static byte[] CreateType2Challenge(string serverName = "SOLVIS")
        {
            byte[] nameBytes = Encoding.Unicode.GetBytes(serverName);
            ushort nameLen = (ushort)nameBytes.Length;

            // Build Target Info (AV_PAIRS)
            byte[] avPairs;
            using (var msAv = new MemoryStream())
            {
                using (var bwAv = new BinaryWriter(msAv))
                {
                    // 1. MsvAvNbDomainName (Id = 2)
                    bwAv.Write((ushort)2);
                    bwAv.Write(nameLen);
                    bwAv.Write(nameBytes);

                    // 2. MsvAvNbComputerName (Id = 1)
                    bwAv.Write((ushort)1);
                    bwAv.Write(nameLen);
                    bwAv.Write(nameBytes);

                    // 3. MsvAvDnsDomainName (Id = 4)
                    bwAv.Write((ushort)4);
                    bwAv.Write(nameLen);
                    bwAv.Write(nameBytes);

                    // 4. MsvAvDnsComputerName (Id = 3)
                    bwAv.Write((ushort)3);
                    bwAv.Write(nameLen);
                    bwAv.Write(nameBytes);

                    // 5. MsvAvTimestamp (Id = 7, 8 bytes FILETIME)
                    bwAv.Write((ushort)7);
                    bwAv.Write((ushort)8);
                    bwAv.Write(DateTime.UtcNow.ToFileTime());

                    // 6. MsvAvEOL (Id = 0, Len = 0)
                    bwAv.Write((ushort)0);
                    bwAv.Write((ushort)0);
                }
                avPairs = msAv.ToArray();
            }

            ushort avLen = (ushort)avPairs.Length;

            int headerLen = 56;
            int targetNameOffset = headerLen;
            int targetInfoOffset = targetNameOffset + nameLen;
            int totalLen = targetInfoOffset + avLen;

            byte[] msg = new byte[totalLen];
            NtlmSignature.CopyTo(msg, 0);

            // Message Type = 2
            BitConverter.GetBytes(2).CopyTo(msg, 8);

            // Target Name Fields
            BitConverter.GetBytes(nameLen).CopyTo(msg, 12);
            BitConverter.GetBytes(nameLen).CopyTo(msg, 14);
            BitConverter.GetBytes(targetNameOffset).CopyTo(msg, 16);

            // Flags: 0xA28A8205
            // NegotiateUnicode (0x01) | RequestTarget (0x04) | NegotiateNTLM (0x0200) |
            // NegotiateAlwaysSign (0x8000) | TargetTypeServer (0x00020000) |
            // NegotiateExtendedSecurity (0x00080000) | NegotiateTargetInfo (0x00800000) |
            // NegotiateVersion (0x02000000) | Negotiate128 (0x20000000) | Negotiate56 (0x80000000)
            uint flags = 0xA28A8205;
            BitConverter.GetBytes(flags).CopyTo(msg, 20);

            // Server Challenge Nonce (8 random bytes)
            RandomNumberGenerator.Fill(msg.AsSpan(24, 8));

            // Target Info Fields
            BitConverter.GetBytes(avLen).CopyTo(msg, 40);
            BitConverter.GetBytes(avLen).CopyTo(msg, 42);
            BitConverter.GetBytes(targetInfoOffset).CopyTo(msg, 44);

            // Version (Windows 10.0 build 19041, NTLM current revision 15)
            byte[] version = new byte[] { 10, 0, 0x29, 0x4A, 0, 0, 0, 15 };
            version.CopyTo(msg, 48);

            // Payloads
            nameBytes.CopyTo(msg, targetNameOffset);
            avPairs.CopyTo(msg, targetInfoOffset);

            return msg;
        }

        /// <summary>
        /// Parses an NTLM Type 3 Authenticate Message and extracts the Windows username, workstation name, and domain.
        /// In standard NTLM SSP, these fields are transmitted in the payload.
        /// </summary>
        public static (string? username, string? workstation, string? domain) ParseType3(byte[] message)
        {
            try
            {
                if (!IsType3(message)) return (null, null, null);

                ushort domainLen = BitConverter.ToUInt16(message, 28);
                int domainOffset = BitConverter.ToInt32(message, 32);

                ushort userLen = BitConverter.ToUInt16(message, 36);
                int userOffset = BitConverter.ToInt32(message, 40);

                ushort wsLen = BitConverter.ToUInt16(message, 44);
                int wsOffset = BitConverter.ToInt32(message, 48);

                string DecodeField(int offset, int len)
                {
                    if (len <= 0 || offset < 0 || offset + len > message.Length) return string.Empty;
                    bool isUnicode = len >= 2 && message[offset + 1] == 0x00;
                    var enc = isUnicode ? Encoding.Unicode : Encoding.UTF8;
                    return enc.GetString(message, offset, len).Trim('\0', ' ');
                }

                string domain = DecodeField(domainOffset, domainLen);
                string username = DecodeField(userOffset, userLen);
                string workstation = DecodeField(wsOffset, wsLen);

                return (
                    string.IsNullOrWhiteSpace(username) ? null : username,
                    string.IsNullOrWhiteSpace(workstation) ? null : workstation,
                    string.IsNullOrWhiteSpace(domain) ? null : domain
                );
            }
            catch
            {
                return (null, null, null);
            }
        }
    }
}
