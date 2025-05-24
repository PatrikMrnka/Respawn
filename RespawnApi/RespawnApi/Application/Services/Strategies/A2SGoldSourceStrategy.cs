// File: haha/RespawnApi/RespawnApi/Application/Services/Strategies/A2SGoldSourceStrategy.cs
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services.Strategies
{
    public class A2SGoldSourceStrategy : IGameServerInfoStrategy
    {
        private readonly ILogger<A2SGoldSourceStrategy> _logger;
        private const int DefaultTimeoutMilliseconds = 3000;

        private static readonly byte[] A2S_INFO_REQUEST_PAYLOAD = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
        private static readonly byte[] A2S_PLAYER_REQUEST_PAYLOAD_INITIAL = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly byte A2S_CHALLENGE_RESPONSE_HEADER = 0x41;
        private static readonly byte A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD = 0x49; // 'I'
        private static readonly byte A2S_PLAYER_RESPONSE_HEADER = 0x44; // 'D'

        private int _parserOffset;


        public GameType SupportedGameType => GameType.CounterStrike;

        public A2SGoldSourceStrategy(ILogger<A2SGoldSourceStrategy> logger)
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
                    var receiveTask = udpClient.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds)) == receiveTask && receiveTask.Result.Buffer != null)
                    {
                        return receiveTask.Result.Buffer;
                    }
                    _logger.LogWarning("Timeout or no data received from {TargetEndpoint} for A2SGoldSourceStrategy after sending {PayloadLength} bytes.", target, payload.Length);
                    return null;
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "SocketException during UDP communication with {TargetEndpoint} in A2SGoldSourceStrategy. ErrorCode: {ErrorCode}", target, ex.SocketErrorCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception during UDP communication with {TargetEndpoint} in A2SGoldSourceStrategy", target);
                return null;
            }
        }

        private string ReadNullTerminatedString(byte[] buffer, Encoding encoding)
        {
            int end = _parserOffset;
            while (end < buffer.Length && buffer[end] != 0x00)
            {
                end++;
            }
            if (end >= buffer.Length && (buffer.Length == 0 || buffer[buffer.Length - 1] != 0x00))
            {
                _logger.LogWarning("ReadNullTerminatedString: String not null-terminated or extends beyond buffer. Offset: {Offset}, BufferLength: {Length}", _parserOffset, buffer.Length);
                string partialResult = encoding.GetString(buffer, _parserOffset, buffer.Length - _parserOffset);
                _parserOffset = buffer.Length;
                return partialResult;
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

        private float ReadFloat(byte[] buffer)
        {
            if (_parserOffset + 4 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Float.");
            float result = BitConverter.ToSingle(buffer, _parserOffset);
            _parserOffset += 4;
            return result;
        }
        private int ReadInt32LittleEndian(byte[] buffer)
        {
            if (_parserOffset + 4 > buffer.Length) throw new IndexOutOfRangeException("Buffer too short to read Int32.");
            int result = BitConverter.ToInt32(buffer, _parserOffset);
            _parserOffset += 4;
            return result;
        }


        private GameServerDetailDto? ParseA2SInfo_GoldSource_TypeI_Variant(byte[] buffer, GameServerDto basicServerInfo)
        {
            _parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) || buffer[4] != A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD)
            {
                _logger.LogError("Invalid A2S_INFO GoldSource Type I (0x49) header or insufficient data. Header: {HeaderByte}", buffer.Length > 4 ? buffer[4] : (byte)0);
                return null;
            }
            _parserOffset = 5;

            try
            {
                // According to Wireshark: FFFFFFFF 49 Name\0 Map\0 Folder\0 Game\0 Players(byte) 00 00 MaxPlayers(byte) Protocol(byte) ServerType(char) Env(char) Visibility(byte) VAC(byte) [Version\0] [EDF...]
                // The first string is the Server Address:Port, but some servers (like the user's) send Server Name directly.
                // We will assume the user's server structure is: Name\0Map\0Folder\0GameDescription\0...
                string serverName = ReadNullTerminatedString(buffer, Encoding.UTF8); // Using UTF8, might need adjustment for specific server encodings
                string mapName = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string folder = ReadNullTerminatedString(buffer, Encoding.UTF8);
                string gameDescription = ReadNullTerminatedString(buffer, Encoding.UTF8);

                byte playerCount = ReadByte(buffer);
                // Wireshark showed: 0a (players) 00 00 10 (max_players) 00 (protocol)
                // This structure is unusual. Standard is usually players, max_players, protocol directly.
                // The two 0x00 bytes are unexpected in typical A2S_INFO_OLD.
                // Let's try to read them as they appeared in the user's Wireshark.
                byte unknownByte1 = ReadByte(buffer); // Potentially 0x00
                byte unknownByte2 = ReadByte(buffer); // Potentially 0x00
                byte maxPlayers = ReadByte(buffer);
                byte protocol = ReadByte(buffer);

                char serverTypeChar = (char)ReadByte(buffer);
                char environmentChar = (char)ReadByte(buffer);
                byte visibility = ReadByte(buffer);
                byte vacEnabled = ReadByte(buffer);

                _logger.LogDebug("Parsed A2S_INFO_OLD: Name='{sName}', Map='{mName}', Folder='{fld}', Game='{gDesc}', Players={pc}/{mp}, Proto={prot}, Type='{st}', Env='{env}', Vis={vis}, VAC={vac}",
                    serverName, mapName, folder, gameDescription, playerCount, maxPlayers, protocol, serverTypeChar, environmentChar, visibility, vacEnabled);


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
                    if (_parserOffset + 9 > buffer.Length && i < playerCount)
                    {
                        _logger.LogWarning("A2S_PLAYER: Buffer potentially too short for full player entry {PlayerNum}/{TotalPlayers}. Offset: {Offset}, Remaining: {Remaining}", i + 1, playerCount, _parserOffset, buffer.Length - _parserOffset);
                        break;
                    }
                    byte index = ReadByte(buffer);
                    string name = ReadNullTerminatedString(buffer, Encoding.UTF8);
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

        public async Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto)
        {
            if (serverEntity.IpAddress == null || !serverEntity.Port.HasValue)
            {
                _logger.LogWarning("A2SGoldSourceStrategy: IP address or port missing for server {ServerId}", serverEntity.GameServerId);
                return CreateFallbackDto(basicDto, "Chybí IP adresa nebo port serveru.");
            }

            IPEndPoint? targetEndpoint = null;
            try
            {
                if (IPAddress.TryParse(serverEntity.IpAddress, out IPAddress? parsedIp))
                {
                    targetEndpoint = new IPEndPoint(parsedIp, serverEntity.Port.Value);
                }
                else
                {
                    IPHostEntry hostEntry = await Dns.GetHostEntryAsync(serverEntity.IpAddress);
                    targetEndpoint = new IPEndPoint(hostEntry.AddressList[0], serverEntity.Port.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IPEndPoint for A2S query to {IpAddress}:{Port} in A2SGoldSourceStrategy.", serverEntity.IpAddress, serverEntity.Port.Value);
                return CreateFallbackDto(basicDto, $"Chyba připojení: {ex.Message}");
            }

            if (targetEndpoint == null) return CreateFallbackDto(basicDto, "Nepodařilo se vytvořit koncový bod pro dotaz.");


            byte[]? responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, A2S_INFO_REQUEST_PAYLOAD);
            if (responseBytes == null || responseBytes.Length < 5)
            {
                return CreateFallbackDto(basicDto, "Server neodpověděl na A2S_INFO nebo odpověď byla příliš krátká.");
            }

            if (responseBytes[4] == A2S_CHALLENGE_RESPONSE_HEADER)
            {
                _logger.LogInformation("A2SGoldSourceStrategy: Received A2S_CHALLENGE from {TargetEndpoint}.", targetEndpoint);
                if (responseBytes.Length < 9)
                {
                    return CreateFallbackDto(basicDto, "Neplatná A2S_CHALLENGE odpověď (příliš krátká).");
                }
                List<byte> challengedRequestList = new List<byte>(A2S_INFO_REQUEST_PAYLOAD);
                challengedRequestList.AddRange(responseBytes.Skip(5).Take(4));
                responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, challengedRequestList.ToArray());

                if (responseBytes == null || responseBytes.Length < 5)
                {
                    return CreateFallbackDto(basicDto, "Server neodpověděl na A2S_INFO s challenge nebo odpověď byla příliš krátká.");
                }
            }

            GameServerDetailDto? parsedInfo = null;
            if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD) // 'I'
            {
                parsedInfo = ParseA2SInfo_GoldSource_TypeI_Variant(responseBytes, basicDto);
            }
            // Add handling for 0x6D ('m') if needed, potentially calling a different parser
            // else if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_NEW) { ... }
            else
            {
                _logger.LogWarning("A2SGoldSourceStrategy: Received unexpected response header 0x{HeaderByte:X2} from {TargetEndpoint} instead of A2S_INFO (0x49 or 0x6D).", responseBytes[4], targetEndpoint);
                return CreateFallbackDto(basicDto, $"Neočekávaná A2S odpověď: 0x{responseBytes[4]:X2}.");
            }

            if (parsedInfo == null)
            {
                return CreateFallbackDto(basicDto, $"Nepodařilo se parsovat A2S_INFO odpověď (Hlavička: 0x{responseBytes[4]:X2}).");
            }

            var players = await GetA2SPlayerInfoAsync(targetEndpoint, serverEntity.GameType);
            parsedInfo.Players = players.OrderByDescending(p => p.Score).ToList();
            if (players.Any())
            {
                parsedInfo.CurrentPlayers = players.Count;
            }

            return parsedInfo;
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

        private GameServerDetailDto CreateFallbackDto(GameServerDto basicInfo, string a2sStatusDetail)
        {
            _logger.LogWarning("A2SGoldSourceStrategy Fallback pro {ServerName}. Detail: {A2SStatus}", basicInfo.Name, a2sStatusDetail);
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
                MapName = "N/A",
                CurrentPlayers = 0,
                MaxPlayers = 0,
                GameName = basicInfo.Name
            };
        }
    }
}
