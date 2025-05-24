// File: haha/RespawnApi/RespawnApi/Application/Services/GameServerQueryService.cs
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Enums; // Required for GameType
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services
{
    public class GameServerQueryService : IGameServerQueryService
    {
        private readonly ILogger<GameServerQueryService> _logger;
        private const int DefaultTimeoutMilliseconds = 3000; // Timeout for UDP operations

        // A2S Payloads
        private static readonly byte[] A2S_INFO_REQUEST_PAYLOAD = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
        private static readonly byte[] A2S_PLAYER_REQUEST_PAYLOAD_INITIAL = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF }; // Initial request, server should respond with challenge
        private static readonly byte A2S_CHALLENGE_RESPONSE_HEADER = 0x41; // 'A'
        private static readonly byte A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_NEW = 0x6D; // 'm' - Newer GoldSource / Source
        private static readonly byte A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD = 0x49; // 'I' - Older GoldSource
        private static readonly byte A2S_PLAYER_RESPONSE_HEADER = 0x44; // 'D'


        public GameServerQueryService(ILogger<GameServerQueryService> logger)
        {
            _logger = logger;
        }

        private async Task<byte[]?> SendAndReceiveUdpPacketAsync(IPEndPoint target, byte[] payload, int timeoutMilliseconds = DefaultTimeoutMilliseconds)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = timeoutMilliseconds;
                    udpClient.Client.ReceiveTimeout = timeoutMilliseconds;

                    await udpClient.SendAsync(payload, payload.Length, target);
                    _logger.LogDebug("Sent UDP packet ({PayloadLength} bytes) to {TargetEndpoint}", payload.Length, target);

                    var receiveTask = udpClient.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds)) == receiveTask && receiveTask.Result.Buffer != null)
                    {
                        _logger.LogDebug("Received UDP packet ({BufferLength} bytes) from {RemoteEndpoint}", receiveTask.Result.Buffer.Length, receiveTask.Result.RemoteEndPoint);
                        return receiveTask.Result.Buffer;
                    }
                    else
                    {
                        _logger.LogWarning("Timeout or no data received from {TargetEndpoint} after sending {PayloadLength} bytes.", target, payload.Length);
                        return null;
                    }
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "SocketException during UDP communication with {TargetEndpoint}. ErrorCode: {ErrorCode}", target, ex.SocketErrorCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception during UDP communication with {TargetEndpoint}", target);
                return null;
            }
        }

        public async Task<GameServerDetailDto?> GetServerDetailsA2SAsync(string ipAddress, int queryPort, GameServerDto basicServerInfo)
        {
            _logger.LogInformation("Attempting A2S query (UdpClient) for {IpAddress}:{QueryPort}. Server: {ServerName}, GameType: {GameType}",
                ipAddress, queryPort, basicServerInfo.Name, basicServerInfo.GameType);

            IPEndPoint? targetEndpoint = null;
            try
            {
                if (IPAddress.TryParse(ipAddress, out IPAddress? parsedIp))
                {
                    targetEndpoint = new IPEndPoint(parsedIp, queryPort);
                }
                else
                {
                    _logger.LogInformation("'{IpAddress}' is not a valid IP, attempting to resolve as hostname.", ipAddress);
                    IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                    if (hostEntry.AddressList.Any())
                    {
                        targetEndpoint = new IPEndPoint(hostEntry.AddressList[0], queryPort);
                        _logger.LogInformation("Resolved hostname {Hostname} to {ResolvedIpAddress}", ipAddress, hostEntry.AddressList[0]);
                    }
                    else
                    {
                        _logger.LogWarning("Could not resolve hostname: {Hostname}", ipAddress);
                        return CreateFallbackDto(basicServerInfo, "Hostname resolution failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IPEndPoint for A2S query to {IpAddress}:{QueryPort}.", ipAddress, queryPort);
                return CreateFallbackDto(basicServerInfo, $"Error setting up connection: {ex.Message}");
            }

            if (targetEndpoint == null) return CreateFallbackDto(basicServerInfo, "Invalid target endpoint.");

            byte[]? responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, A2S_INFO_REQUEST_PAYLOAD);

            if (responseBytes == null || responseBytes.Length < 5)
            {
                _logger.LogWarning("No valid response or too short response from server {TargetEndpoint} for initial A2S_INFO.", targetEndpoint);
                return CreateFallbackDto(basicServerInfo, "Server did not respond to A2S_INFO query or response was too short.");
            }

            if (responseBytes[4] == A2S_CHALLENGE_RESPONSE_HEADER)
            {
                _logger.LogInformation("Received A2S_CHALLENGE (0x{HeaderByte:X2}) from {TargetEndpoint}. Responding with challenge.", responseBytes[4], targetEndpoint);
                if (responseBytes.Length < 9)
                {
                    _logger.LogWarning("A2S_CHALLENGE response from {TargetEndpoint} is too short (length {Length}) to contain a challenge number.", targetEndpoint, responseBytes.Length);
                    return CreateFallbackDto(basicServerInfo, "Invalid A2S_CHALLENGE response (too short).");
                }

                List<byte> challengedRequestList = new List<byte>(A2S_INFO_REQUEST_PAYLOAD);
                challengedRequestList.AddRange(responseBytes.Skip(5).Take(4));

                responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, challengedRequestList.ToArray());

                if (responseBytes == null || responseBytes.Length < 5)
                {
                    _logger.LogWarning("No valid response or too short response from server {TargetEndpoint} after sending A2S_INFO with challenge.", targetEndpoint);
                    return CreateFallbackDto(basicServerInfo, "Server did not respond to A2S_INFO with challenge or response was too short.");
                }
            }

            // Now, parse the A2S_INFO response
            // Check for either old (0x49) or new (0x6D) GoldSource/Source INFO response header
            if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD || responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_NEW)
            {
                _logger.LogInformation("Received A2S_INFO response (Header: 0x{HeaderByte:X2}) from {TargetEndpoint}. Parsing...", responseBytes[4], targetEndpoint);

                GameServerDetailDto? parsedInfo;
                if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD) // 'I'
                {
                    parsedInfo = ParseA2SInfo_GoldSource_TypeI_Variant(responseBytes, basicServerInfo);
                }
                else // 0x6D ('m') or other future compatible types if parser is generic enough
                {
                    // Assuming the existing ParseA2SInfo_GoldSource was intended for 0x6D or a similar structure
                    // This might need its own specific parser if 0x6D structure differs significantly from 0x49 variant
                    parsedInfo = ParseA2SInfo_GoldSource_TypeM_Likely(responseBytes, basicServerInfo);
                }

                if (parsedInfo == null)
                {
                    return CreateFallbackDto(basicServerInfo, $"Failed to parse A2S_INFO response (Header: 0x{responseBytes[4]:X2}).");
                }

                var players = await GetA2SPlayerInfoAsync(targetEndpoint, basicServerInfo.GameType);
                parsedInfo.Players = players.OrderByDescending(p => p.Score).ToList();
                // CurrentPlayers from A2S_INFO might be more accurate before A2S_PLAYER, or update from A2S_PLAYER if preferred.
                // For now, let A2S_INFO's player count be primary, A2S_PLAYER list is supplementary.
                // If A2S_PLAYER returns a list, we can update CurrentPlayers:
                if (players.Any())
                {
                    parsedInfo.CurrentPlayers = players.Count;
                }


                return parsedInfo;
            }
            else
            {
                _logger.LogWarning("Received unexpected response header 0x{HeaderByte:X2} from {TargetEndpoint} instead of A2S_INFO (0x49 or 0x6D).", responseBytes[4], targetEndpoint);
                return CreateFallbackDto(basicServerInfo, $"Unexpected A2S response header: 0x{responseBytes[4]:X2}.");
            }
        }

        private GameServerDetailDto CreateFallbackDto(GameServerDto basicInfo, string a2sStatusDetail)
        {
            _logger.LogWarning("Creating fallback DTO for {ServerName}. A2S Status: {A2SStatus}", basicInfo.Name, a2sStatusDetail);
            return new GameServerDetailDto
            {
                GameServerId = basicInfo.GameServerId,
                Name = basicInfo.Name,
                GameType = basicInfo.GameType,
                Status = basicInfo.Status,
                IpAddress = basicInfo.IpAddress,
                Port = basicInfo.Port,
                ContainerId = basicInfo.ContainerId,
                CreatedAt = basicInfo.CreatedAt,
                StatusDetails = $"{basicInfo.StatusDetails ?? ""}{(string.IsNullOrEmpty(basicInfo.StatusDetails) ? "" : "; ")}A2S: {a2sStatusDetail}",
                Players = new List<PlayerDetailDto>(),
                MapName = "N/A (A2S selhalo)",
                CurrentPlayers = 0,
                MaxPlayers = 0,
                GameName = basicInfo.Name
            };
        }

        private async Task<List<PlayerDetailDto>> GetA2SPlayerInfoAsync(IPEndPoint targetEndpoint, GameType gameType)
        {
            var players = new List<PlayerDetailDto>();
            _logger.LogInformation("Attempting A2S_PLAYER query for {TargetEndpoint}", targetEndpoint);

            byte[]? playerResponseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, A2S_PLAYER_REQUEST_PAYLOAD_INITIAL);

            if (playerResponseBytes == null || playerResponseBytes.Length < 5)
            {
                _logger.LogWarning("No/short response for initial A2S_PLAYER from {TargetEndpoint}", targetEndpoint);
                return players;
            }

            if (playerResponseBytes[4] == A2S_CHALLENGE_RESPONSE_HEADER)
            {
                _logger.LogInformation("Received A2S_CHALLENGE for A2S_PLAYER from {TargetEndpoint}", targetEndpoint);
                if (playerResponseBytes.Length < 9)
                {
                    _logger.LogWarning("A2S_CHALLENGE for players from {TargetEndpoint} is too short.", targetEndpoint);
                    return players;
                }

                byte[] challenge = playerResponseBytes.Skip(5).Take(4).ToArray();
                byte[] playerRequestWithChallenge = A2S_PLAYER_REQUEST_PAYLOAD_INITIAL.Take(5)
                                                    .Concat(challenge)
                                                    .ToArray();

                playerResponseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, playerRequestWithChallenge);

                if (playerResponseBytes == null || playerResponseBytes.Length < 5)
                {
                    _logger.LogWarning("No/short response for A2S_PLAYER with challenge from {TargetEndpoint}", targetEndpoint);
                    return players;
                }
            }
            else
            {
                _logger.LogWarning("Expected A2S_CHALLENGE for A2S_PLAYER from {TargetEndpoint}, but got header 0x{Header:X2}", targetEndpoint, playerResponseBytes[4]);
                return players;
            }

            if (playerResponseBytes[4] == A2S_PLAYER_RESPONSE_HEADER)
            {
                _logger.LogInformation("Received A2S_PLAYER response from {TargetEndpoint}. Parsing...", targetEndpoint);
                return ParseA2SPlayer_GoldSource(playerResponseBytes);
            }
            else
            {
                _logger.LogWarning("Received unexpected header 0x{HeaderByte:X2} for A2S_PLAYER from {TargetEndpoint}", playerResponseBytes[4], targetEndpoint);
            }
            return players;
        }

        private int _parserOffset; // Renamed from _currentOffset to avoid conflict if this class becomes non-static helper

        private string ReadNullTerminatedString(byte[] buffer, Encoding encoding)
        {
            int end = _parserOffset;
            while (end < buffer.Length && buffer[end] != 0x00)
            {
                end++;
            }
            if (end >= buffer.Length && buffer[buffer.Length - 1] != 0x00)
            { // String not null-terminated within buffer
                _logger.LogWarning("ReadNullTerminatedString: String not null-terminated or extends beyond buffer. Offset: {Offset}, BufferLength: {Length}", _parserOffset, buffer.Length);
                string partialResult = encoding.GetString(buffer, _parserOffset, buffer.Length - _parserOffset);
                _parserOffset = buffer.Length;
                return partialResult; // Or throw
            }
            string result = encoding.GetString(buffer, _parserOffset, end - _parserOffset);
            _parserOffset = end + 1;
            return result;
        }

        private byte ReadByte(byte[] buffer)
        {
            if (_parserOffset >= buffer.Length) throw new IndexOutOfRangeException($"Attempt to read byte at offset {_parserOffset} beyond buffer length {buffer.Length}.");
            byte result = buffer[_parserOffset];
            _parserOffset++;
            return result;
        }

        // private short ReadInt16(byte[] buffer) // Not currently used, but keep if needed
        // {
        //     if (_parserOffset + 2 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Int16.");
        //     short result = BitConverter.ToInt16(buffer, _parserOffset);
        //     _parserOffset += 2;
        //     return result;
        // }

        private float ReadFloat(byte[] buffer)
        {
            if (_parserOffset + 4 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Float.");
            float result = BitConverter.ToSingle(buffer, _parserOffset);
            _parserOffset += 4;
            return result;
        }

        // Specific parser for GoldSource A2S_INFO response type 0x49 ('I')
        // based on observed Wireshark data: FFFFFFFF 49 Name\0 Map\0 Folder\0 Game\0 Players(byte) 00 00 MaxPlayers(byte) Protocol(byte) ServerType(char) Env(char) Visibility(byte) VAC(byte) [Version\0] [EDF...]
        private GameServerDetailDto? ParseA2SInfo_GoldSource_TypeI_Variant(byte[] buffer, GameServerDto basicServerInfo)
        {
            _parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) || buffer[4] != A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD)
            {
                _logger.LogError("Invalid A2S_INFO GoldSource Type I (0x49) header or insufficient data. Header: {HeaderByte}", buffer.Length > 4 ? buffer[4] : (byte)0);
                return null;
            }
            _parserOffset = 5; // Skip 0xFFFFFFFF and header byte 0x49

            try
            {
                string serverName = ReadNullTerminatedString(buffer, Encoding.UTF8); // Encoding might need to be Windows-1250 or similar for CS 1.6
                string mapName = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string folder = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string gameDescription = ReadNullTerminatedString(buffer, Encoding.UTF8);

                byte playerCount = ReadByte(buffer);
                // Based on Wireshark: 0a (players) 00 00 10 (max_players) 00 (protocol)
                ReadByte(buffer); // Skip first 0x00 after player count
                ReadByte(buffer); // Skip second 0x00 after player count
                byte maxPlayers = ReadByte(buffer); // This should be 0x10 (16)
                byte protocol = ReadByte(buffer); // This should be the 0x00 after maxPlayers

                char serverTypeChar = (char)ReadByte(buffer);
                char environmentChar = (char)ReadByte(buffer);
                byte visibility = ReadByte(buffer); // 0 = public, 1 = private (password)
                byte vacEnabled = ReadByte(buffer); // 0 = unsecured, 1 = secured

                // Optional: Version string (if present, often after VAC)
                string version = "";
                if (_parserOffset < buffer.Length && buffer[_parserOffset] != 0x00) // Check if there's more data that's not an EDF
                {
                    // Heuristic: if next byte is not a known EDF start, assume it's version string
                    // A more robust way would be to check EDF if it exists.
                    // For now, let's try to read it if it looks like a string.
                    // This part is highly speculative without exact server spec.
                    try
                    {
                        if (buffer.Skip(_parserOffset).TakeWhile(b => b != 0x00).Any(b => b >= 0x20 && b <= 0x7E)) // Check for printable ASCII
                        {
                            version = ReadNullTerminatedString(buffer, Encoding.UTF8);
                            _logger.LogDebug("Parsed optional version string: {Version}", version);
                        }
                    }
                    catch (Exception) { /* Ignore if reading version fails, it's optional */ }
                }

                // EDF parsing would go here if needed and supported by server.

                return new GameServerDetailDto
                {
                    GameServerId = basicServerInfo.GameServerId,
                    Name = serverName,
                    GameType = basicServerInfo.GameType,
                    Status = ServerStatus.Online,
                    IpAddress = basicServerInfo.IpAddress,
                    Port = basicServerInfo.Port,
                    ContainerId = basicServerInfo.ContainerId,
                    CreatedAt = basicServerInfo.CreatedAt,
                    StatusDetails = "Úspěšně dotazováno přes A2S (Typ I - 0x49).",

                    GameName = gameDescription,
                    MapName = mapName,
                    CurrentPlayers = playerCount,
                    MaxPlayers = maxPlayers,
                    IsVacSecured = vacEnabled == 1,
                    Players = new List<PlayerDetailDto>()
                };
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error parsing A2S_INFO GoldSource Type I (0x49) response: Index out of range. Buffer length: {BufferLength}, CurrentOffset: {Offset}", buffer.Length, _parserOffset);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing A2S_INFO GoldSource Type I (0x49) response. Buffer length: {BufferLength}", buffer.Length);
                return null;
            }
        }

        // Parser for GoldSource/Source A2S_INFO response type 0x6D ('m') - needs verification for exact GoldSrc 0x6D structure
        private GameServerDetailDto? ParseA2SInfo_GoldSource_TypeM_Likely(byte[] buffer, GameServerDto basicServerInfo)
        {
            _parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) || buffer[4] != A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_NEW)
            {
                _logger.LogError("Invalid A2S_INFO GoldSource Type M (0x6D) header or insufficient data. Header: {HeaderByte}", buffer.Length > 4 ? buffer[4] : (byte)0);
                return null;
            }
            _parserOffset = 5; // Skip 0xFFFFFFFF and header byte 0x6D

            try
            {
                // Structure for 0x6D (often for Source, but some GoldSrc might use parts)
                // byte: Protocol
                // string: Server Name
                // string: Map Name
                // string: Folder
                // string: Game Description
                // short: AppID
                // byte: Number of Players
                // byte: Max Players
                // byte: Number of Bots
                // char: Server Type ('d', 'l', 'p')
                // char: Environment ('w', 'l', 'm'/'o')
                // byte: Visibility (0 public, 1 private)
                // byte: VAC Secured (0 unsecured, 1 secured)
                // string: Game Version
                // (Optional EDF byte and fields)

                byte protocol = ReadByte(buffer);
                string serverName = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string mapName = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string folder = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string gameDescription = ReadNullTerminatedString(buffer, Encoding.UTF8);

                short appId = ReadInt16LittleEndian(buffer); // AppID

                byte playerCount = ReadByte(buffer);
                byte maxPlayers = ReadByte(buffer);
                byte botCount = ReadByte(buffer);
                char serverTypeChar = (char)ReadByte(buffer);
                char environmentChar = (char)ReadByte(buffer);
                byte visibility = ReadByte(buffer);
                byte vacEnabled = ReadByte(buffer);
                string version = ReadNullTerminatedString(buffer, Encoding.UTF8); // Game Version

                // EDF parsing would go here

                return new GameServerDetailDto
                {
                    GameServerId = basicServerInfo.GameServerId,
                    Name = serverName,
                    GameType = basicServerInfo.GameType,
                    Status = ServerStatus.Online,
                    IpAddress = basicServerInfo.IpAddress,
                    Port = basicServerInfo.Port,
                    ContainerId = basicServerInfo.ContainerId,
                    CreatedAt = basicServerInfo.CreatedAt,
                    StatusDetails = "Úspěšně dotazováno přes A2S (Typ M - 0x6D).",

                    GameName = gameDescription,
                    MapName = mapName,
                    CurrentPlayers = playerCount,
                    MaxPlayers = maxPlayers,
                    IsVacSecured = vacEnabled == 1,
                    Players = new List<PlayerDetailDto>()
                };
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error parsing A2S_INFO GoldSource Type M (0x6D) response: Index out of range. Buffer length: {BufferLength}, CurrentOffset: {Offset}", buffer.Length, _parserOffset);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing A2S_INFO GoldSource Type M (0x6D) response. Buffer length: {BufferLength}", buffer.Length);
                return null;
            }
        }

        private List<PlayerDetailDto> ParseA2SPlayer_GoldSource(byte[] buffer)
        {
            var players = new List<PlayerDetailDto>();
            _parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) || buffer[4] != A2S_PLAYER_RESPONSE_HEADER)
            {
                _logger.LogError("Invalid A2S_PLAYER GoldSource header or insufficient data. Header: {HeaderByte}", buffer.Length > 4 ? buffer[4] : (byte)0);
                return players;
            }
            _parserOffset = 5;

            try
            {
                byte playerCount = ReadByte(buffer);
                _logger.LogDebug("A2S_PLAYER: Reported player count: {PlayerCount}", playerCount);

                for (int i = 0; i < playerCount; i++)
                {
                    if (_parserOffset + 9 > buffer.Length && i < playerCount) // Minimum for index (1) + name (1 null) + score (4) + duration (4) = 10
                    {
                        _logger.LogWarning("A2S_PLAYER: Buffer potentially too short for full player entry {PlayerNum}/{TotalPlayers}. Offset: {Offset}, Remaining: {Remaining}", i + 1, playerCount, _parserOffset, buffer.Length - _parserOffset);
                        break;
                    }
                    byte index = ReadByte(buffer);
                    string name = ReadNullTerminatedString(buffer, Encoding.UTF8); // Use appropriate encoding
                    int score = ReadInt32LittleEndian(buffer);
                    float duration = ReadFloat(buffer);

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        players.Add(new PlayerDetailDto
                        {
                            Name = name,
                            Score = score,
                            Duration = duration
                        });
                    }
                }
                _logger.LogInformation("A2S_PLAYER: Successfully parsed {ParsedCount} players from {ReportedCount} reported.", players.Count, playerCount);
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error parsing A2S_PLAYER GoldSource response: Index out of range. Buffer length: {BufferLength}, CurrentOffset: {Offset}", buffer.Length, _parserOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing A2S_PLAYER GoldSource response. Buffer length: {BufferLength}", buffer.Length);
            }
            return players;
        }

        private int ReadInt32LittleEndian(byte[] buffer)
        {
            if (_parserOffset + 4 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Int32.");
            int result = BitConverter.ToInt32(buffer, _parserOffset);
            _parserOffset += 4;
            return result;
        }
        private short ReadInt16LittleEndian(byte[] buffer)
        {
            if (_parserOffset + 2 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Int16.");
            short result = BitConverter.ToInt16(buffer, _parserOffset);
            _parserOffset += 2;
            return result;
        }
    }
}
