using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Application.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RespawnApi.Application.Services.Strategies
{
    /// <summary>
    /// Strategy for querying game server information using the A2S protocol for GoldSource games (e.g., Counter-Strike 1.6).
    /// </summary>
    public class A2SGoldSourceStrategy : IGameServerInfoStrategy
    {
        private readonly ILogger<A2SGoldSourceStrategy> _logger;
        private const int DefaultTimeoutMilliseconds = 3000;

        private static readonly byte[] A2S_INFO_REQUEST_PAYLOAD =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65,
            0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };

        private static readonly byte[] A2S_PLAYER_REQUEST_PAYLOAD_INITIAL =
            { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };

        private static readonly byte A2S_CHALLENGE_RESPONSE_HEADER = 0x41;
        private static readonly byte A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD = 0x49; // 'I'
        private static readonly byte A2S_PLAYER_RESPONSE_HEADER = 0x44; // 'D'

        public GameType SupportedGameType => GameType.CounterStrike;

        public A2SGoldSourceStrategy(ILogger<A2SGoldSourceStrategy> logger)
        {
            _logger = logger;
        }


        /// <summary>
        /// Sends a UDP packet to the specified target endpoint and waits asynchronously for a response.
        /// Handles timeouts and logs errors or warnings as appropriate.
        /// </summary>
        /// <param name="target">The target <see cref="IPEndPoint"/> to send the UDP packet to.</param>
        /// <param name="payload">The byte array payload to send.</param>
        /// <param name="timeoutMilliseconds">The timeout in milliseconds to wait for a response. Defaults to 3000 ms.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the response byte array if received,
        /// or <c>null</c> if a timeout or error occurs.
        /// </returns>
        private async Task<byte[]?> SendAndReceiveUdpPacketAsync(IPEndPoint target, byte[] payload,
            int timeoutMilliseconds = DefaultTimeoutMilliseconds)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = timeoutMilliseconds;
                    udpClient.Client.ReceiveTimeout = timeoutMilliseconds;
                    await udpClient.SendAsync(payload, payload.Length, target);
                    var receiveTask = udpClient.ReceiveAsync();

                    if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds)) == receiveTask &&
                        receiveTask.Result.Buffer != null)
                    {
                        return receiveTask.Result.Buffer;
                    }

                    _logger.LogWarning(
                        "Timeout or no data received from {TargetEndpoint} for A2SGoldSourceStrategy after sending {PayloadLength} bytes.",
                        target, payload.Length);
                    return null;
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex,
                    "SocketException during UDP communication with {TargetEndpoint} in A2SGoldSourceStrategy. ErrorCode: {ErrorCode}",
                    target, ex.SocketErrorCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Generic exception during UDP communication with {TargetEndpoint} in A2SGoldSourceStrategy",
                    target);
                return null;
            }
        }

        /// <summary>
        /// Parses the GoldSource A2S_INFO response of Type I (0x49) variant and extracts detailed server information.
        /// Validates the response header and buffer length, then reads server name, map, folder, game description,
        /// player counts, protocol, server type, environment, visibility, and VAC status from the buffer.
        /// Returns a <see cref="GameServerDetailDto"/> populated with the parsed data, or null if parsing fails.
        /// </summary>
        /// <param name="buffer">The byte array containing the A2S_INFO response from the server.</param>
        /// <param name="basicServerInfo">The basic server DTO used to populate common fields in the result.</param>
        /// <returns>
        /// A <see cref="GameServerDetailDto"/> with detailed server information if parsing succeeds; otherwise, null.
        /// </returns>
        private GameServerDetailDto? ParseA2SInfo_GoldSource_TypeI_Variant(byte[] buffer, GameServerDto basicServerInfo)
        {
            int parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) ||
                buffer[4] != A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD)
            {
                _logger.LogError(
                    "Invalid A2S_INFO GoldSource Type I (0x49) header or insufficient data. Header: {HeaderByte}",
                    buffer.Length > 4 ? buffer[4] : (byte)0);
                return null;
            }

            parserOffset = 5;

            try
            {
                Encoding gameEncoding = Encoding.UTF8;
                try
                {
                    gameEncoding = Encoding.GetEncoding("Windows-1250");
                }
                catch
                {
                    /* Fallback to UTF8 if not supported */
                }


                // Get server details from the buffer
                string serverName =
                    BinaryDataParser.ReadNullTerminatedString(buffer, ref parserOffset, gameEncoding, _logger);
                string mapName =
                    BinaryDataParser.ReadNullTerminatedString(buffer, ref parserOffset, gameEncoding, _logger);
                string folder =
                    BinaryDataParser.ReadNullTerminatedString(buffer, ref parserOffset, gameEncoding, _logger);
                string gameDescription =
                    BinaryDataParser.ReadNullTerminatedString(buffer, ref parserOffset, gameEncoding, _logger);

                byte playerCount = BinaryDataParser.ReadByte(buffer, ref parserOffset);
                BinaryDataParser.ReadByte(buffer, ref parserOffset); // Skip unknownByte1 (0x00)
                BinaryDataParser.ReadByte(buffer, ref parserOffset); // Skip unknownByte2 (0x00)
                byte maxPlayers = BinaryDataParser.ReadByte(buffer, ref parserOffset);
                byte protocol = BinaryDataParser.ReadByte(buffer, ref parserOffset);

                char serverTypeChar = (char)BinaryDataParser.ReadByte(buffer, ref parserOffset);
                char environmentChar = (char)BinaryDataParser.ReadByte(buffer, ref parserOffset);
                byte visibility = BinaryDataParser.ReadByte(buffer, ref parserOffset);
                byte vacEnabled = BinaryDataParser.ReadByte(buffer, ref parserOffset);

                _logger.LogDebug(
                    "Parsed A2S_INFO_OLD: Name='{sName}', Map='{mName}', Folder='{fld}', Game='{gDesc}', Players={pc}/{mp}, Proto={prot}, Type='{st}', Env='{env}', Vis={vis}, VAC={vac}",
                    serverName, mapName, folder, gameDescription, playerCount, maxPlayers, protocol, serverTypeChar,
                    environmentChar, visibility, vacEnabled);

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
                _logger.LogError(ex,
                    "Error parsing A2S_INFO GoldSource Type I (0x49) response: Index out of range. Buffer length: {BufferLength}, CurrentOffset: {Offset}",
                    buffer.Length, parserOffset);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error parsing A2S_INFO GoldSource Type I (0x49) response. Buffer length: {BufferLength}",
                    buffer.Length);
                return null;
            }
        }

        /// <summary>
        /// Parses the A2S_PLAYER response from a GoldSource game server and extracts player details.
        /// Validates the response header and buffer length, then iterates through each player entry,
        /// reading the player's index, name, score, and duration. Handles encoding for player names
        /// (preferring Windows-1250 for Central European characters, falling back to UTF8).
        /// Returns a list of <see cref="PlayerDetailDto"/> objects representing the players on the server.
        /// Logs errors and warnings for malformed or incomplete responses.
        /// </summary>
        /// <param name="buffer">The byte array containing the A2S_PLAYER response from the server.</param>
        /// <returns>
        /// A list of <see cref="PlayerDetailDto"/> with player information if parsing succeeds; otherwise, an empty list.
        /// </returns>
        private List<PlayerDetailDto> ParseA2SPlayer_GoldSource(byte[] buffer)
        {
            var players = new List<PlayerDetailDto>();
            int parserOffset = 0;

            if (buffer.Length < 6 || !buffer.Take(4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }) ||
                buffer[4] != A2S_PLAYER_RESPONSE_HEADER)
            {
                _logger.LogError("Invalid A2S_PLAYER GoldSource header or insufficient data. Header: {HeaderByte}",
                    buffer.Length > 4 ? buffer[4] : (byte)0);
                return players;
            }

            parserOffset = 5;

            try
            {
                byte playerCount = BinaryDataParser.ReadByte(buffer, ref parserOffset);
                _logger.LogDebug("A2S_PLAYER: Reported player count: {PlayerCount}", playerCount);

                Encoding gameEncoding = Encoding.UTF8;
                try
                {
                    gameEncoding = Encoding.GetEncoding("Windows-1250");
                }
                catch
                {
                    /* Fallback to UTF8 */
                }


                for (int i = 0; i < playerCount; i++)
                {
                    if (parserOffset + 9 > buffer.Length && i < playerCount)
                    {
                        _logger.LogWarning(
                            "A2S_PLAYER: Buffer potentially too short for full player entry {PlayerNum}/{TotalPlayers}. Offset: {Offset}, Remaining: {Remaining}",
                            i + 1, playerCount, parserOffset, buffer.Length - parserOffset);
                        break;
                    }

                    // Get player details
                    byte index = BinaryDataParser.ReadByte(buffer, ref parserOffset);
                    string name =
                        BinaryDataParser.ReadNullTerminatedString(buffer, ref parserOffset, gameEncoding, _logger);
                    int score = BinaryDataParser.ReadInt32LittleEndian(buffer, ref parserOffset);
                    float duration =
                        BinaryDataParser.ReadFloatLittleEndian(buffer, ref parserOffset);

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

                _logger.LogInformation(
                    "A2S_PLAYER: Successfully parsed {ParsedCount} players from {ReportedCount} reported.",
                    players.Count, playerCount);
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex,
                    "Error parsing A2S_PLAYER GoldSource response: Index out of range. Buffer length: {BufferLength}, CurrentOffset: {Offset}",
                    buffer.Length, parserOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing A2S_PLAYER GoldSource response. Buffer length: {BufferLength}",
                    buffer.Length);
            }

            return players;
        }

        /// <summary>
        /// Asynchronously gets player information from the server using A2S_PLAYER protocol.
        /// Handles challenge-response mechanism.
        /// </summary>
        /// <param name="targetEndpoint">The IPEndPoint of the game server.</param>
        /// <param name="gameType">The type of the game (used for logging/context, not directly for A2S_PLAYER logic here).</param>
        /// <returns>A list of PlayerDetailDto, or an empty list if query fails or no players.</returns>
        private async Task<List<PlayerDetailDto>> GetA2SPlayerInfoAsync(IPEndPoint targetEndpoint, GameType gameType)
        {
            // checked wireshark udp connection for A2S_PLAYER query
            var players = new List<PlayerDetailDto>();
            _logger.LogInformation(
                "A2SGoldSourceStrategy: Attempting A2S_PLAYER query for {TargetEndpoint}, GameType: {GameType}",
                targetEndpoint, gameType);

            // Step 1: Send initial A2S_PLAYER request to get challenge
            // Payload: 0xFFFFFFFF 0x55 0xFFFFFFFF (0x55 is A2S_PLAYER, last 4 bytes are challenge, initially -1)
            byte[]? playerResponseBytes =
                await SendAndReceiveUdpPacketAsync(targetEndpoint, A2S_PLAYER_REQUEST_PAYLOAD_INITIAL);

            if (playerResponseBytes == null || playerResponseBytes.Length < 5)
            {
                _logger.LogWarning(
                    "A2SGoldSourceStrategy: No/short response for initial A2S_PLAYER from {TargetEndpoint}",
                    targetEndpoint);
                return players; // Return empty list
            }

            // Step 2: Check if server responded with a challenge
            if (playerResponseBytes[4] == A2S_CHALLENGE_RESPONSE_HEADER) // 'A'
            {
                _logger.LogInformation(
                    "A2SGoldSourceStrategy: Received A2S_CHALLENGE for A2S_PLAYER from {TargetEndpoint}",
                    targetEndpoint);
                if (playerResponseBytes.Length < 9) // Header (5 bytes) + Challenge (4 bytes)
                {
                    _logger.LogWarning(
                        "A2SGoldSourceStrategy: A2S_CHALLENGE response for players from {TargetEndpoint} is too short (length {Length}) to contain a challenge number.",
                        targetEndpoint, playerResponseBytes.Length);
                    return players; // Return empty list
                }

                // Construct new request with the challenge
                // Payload: 0xFFFFFFFF 0x55 <challenge_bytes>
                byte[] challenge = playerResponseBytes.Skip(5).Take(4).ToArray();
                List<byte> playerRequestWithChallengeList = new List<byte> { 0xFF, 0xFF, 0xFF, 0xFF, 0x55 };
                playerRequestWithChallengeList.AddRange(challenge);

                _logger.LogDebug("A2SGoldSourceStrategy: Sending A2S_PLAYER request with challenge to {TargetEndpoint}",
                    targetEndpoint);
                playerResponseBytes =
                    await SendAndReceiveUdpPacketAsync(targetEndpoint, playerRequestWithChallengeList.ToArray());

                if (playerResponseBytes == null || playerResponseBytes.Length < 5)
                {
                    _logger.LogWarning(
                        "A2SGoldSourceStrategy: No/short response for A2S_PLAYER with challenge from {TargetEndpoint}",
                        targetEndpoint);
                    return players; // Return empty list
                }
            }
            // If the first response was not a challenge, but also not a player list, it's an issue.
            // However, GoldSource servers typically send a challenge for A2S_PLAYER if they support it.

            // Step 3: Parse the A2S_PLAYER response
            if (playerResponseBytes[4] == A2S_PLAYER_RESPONSE_HEADER) // 'D'
            {
                _logger.LogInformation(
                    "A2SGoldSourceStrategy: Received A2S_PLAYER response (Header 0x44) from {TargetEndpoint}. Parsing...",
                    targetEndpoint);
                return ParseA2SPlayer_GoldSource(playerResponseBytes);
            }
            else
            {
                _logger.LogWarning(
                    "A2SGoldSourceStrategy: Received unexpected header 0x{HeaderByte:X2} for A2S_PLAYER response from {TargetEndpoint}. Expected 0x44 (or 0x41 for challenge).",
                    playerResponseBytes[4], targetEndpoint);
            }

            return players; // Return empty list if parsing fails or unexpected header
        }

        public async Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto)
        {
            if (serverEntity.IpAddress == null || !serverEntity.Port.HasValue)
            {
                _logger.LogWarning("A2SGoldSourceStrategy: IP address or port missing for server {ServerId}",
                    serverEntity.GameServerId);
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
                _logger.LogError(ex,
                    "Error creating IPEndPoint for A2S query to {IpAddress}:{Port} in A2SGoldSourceStrategy.",
                    serverEntity.IpAddress, serverEntity.Port.Value);
                return CreateFallbackDto(basicDto, $"Chyba připojení: {ex.Message}");
            }

            if (targetEndpoint == null)
                return CreateFallbackDto(basicDto, "Nepodařilo se vytvořit koncový bod pro dotaz.");

            byte[]? responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, A2S_INFO_REQUEST_PAYLOAD);
            if (responseBytes == null || responseBytes.Length < 5)
            {
                return CreateFallbackDto(basicDto, "Server neodpověděl na A2S_INFO nebo odpověď byla příliš krátká.");
            }

            if (responseBytes[4] == A2S_CHALLENGE_RESPONSE_HEADER)
            {
                _logger.LogInformation("A2SGoldSourceStrategy: Received A2S_CHALLENGE from {TargetEndpoint}.",
                    targetEndpoint);
                if (responseBytes.Length < 9)
                {
                    return CreateFallbackDto(basicDto, "Neplatná A2S_CHALLENGE odpověď (příliš krátká).");
                }

                List<byte> challengedRequestList = new List<byte>(A2S_INFO_REQUEST_PAYLOAD);
                challengedRequestList.AddRange(responseBytes.Skip(5).Take(4));
                responseBytes = await SendAndReceiveUdpPacketAsync(targetEndpoint, challengedRequestList.ToArray());

                if (responseBytes == null || responseBytes.Length < 5)
                {
                    return CreateFallbackDto(basicDto,
                        "Server neodpověděl na A2S_INFO s challenge nebo odpověď byla příliš krátká.");
                }
            }

            GameServerDetailDto? parsedInfo = null;
            if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_OLD) // 'I'
            {
                parsedInfo = ParseA2SInfo_GoldSource_TypeI_Variant(responseBytes, basicDto);
            }
            // else if (responseBytes[4] == A2S_INFO_RESPONSE_HEADER_GOLDSOURCE_NEW) { ... }  - here can be more logic
            else
            {
                _logger.LogWarning(
                    "A2SGoldSourceStrategy: Received unexpected response header 0x{HeaderByte:X2} from {TargetEndpoint} instead of A2S_INFO (0x49).",
                    responseBytes[4], targetEndpoint);
                return CreateFallbackDto(basicDto, $"Neočekávaná A2S odpověď: 0x{responseBytes[4]:X2}.");
            }

            if (parsedInfo == null)
            {
                return CreateFallbackDto(basicDto,
                    $"Nepodařilo se parsovat A2S_INFO odpověď (Hlavička: 0x{responseBytes[4]:X2}).");
            }

            var players = await GetA2SPlayerInfoAsync(targetEndpoint, serverEntity.GameType);
            parsedInfo.Players = players.OrderByDescending(p => p.Score).ToList();
            if (players.Any())
            {
                parsedInfo.CurrentPlayers = players.Count;
            }

            return parsedInfo;
        }

        /// <summary>
        /// Creates a fallback <see cref="GameServerDetailDto"/> when A2S querying fails or returns invalid data.
        /// Populates the DTO with basic server information and a status detail message describing the A2S error.
        /// Sets default values for map name, player counts, and game name.
        /// </summary>
        /// <param name="basicInfo">The basic server DTO containing initial server data.</param>
        /// <param name="a2sStatusDetail">A string describing the reason for the fallback, typically an error or status message from the A2S query.</param>
        /// <returns>
        /// A <see cref="GameServerDetailDto"/> with fallback values and error details, suitable for returning to the caller when A2S fails.
        /// </returns>
        private GameServerDetailDto CreateFallbackDto(GameServerDto basicInfo, string a2sStatusDetail)
        {
            _logger.LogWarning("A2SGoldSourceStrategy Fallback pro {ServerName}. Detail: {A2SStatus}", basicInfo.Name,
                a2sStatusDetail);
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
                StatusDetails =
                    $"{basicInfo.StatusDetails ?? ""}{(string.IsNullOrEmpty(basicInfo.StatusDetails) ? "" : "; ")}A2S: {a2sStatusDetail}",
                Players = new List<PlayerDetailDto>(),
                MapName = "N/A",
                CurrentPlayers = 0,
                MaxPlayers = 0,
                GameName = basicInfo.Name
            };
        }
    }
}