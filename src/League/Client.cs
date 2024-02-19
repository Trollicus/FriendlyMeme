using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FriendlyMeme.Handlers.WebRequests;
using Microsoft.Extensions.Logging;

namespace FriendlyMeme.League;

public class Client(ILogger<Client> logger)
{
    private readonly HttpHandler _httpHandler = new();
    public async Task RunAsync()
    {
        logger.LogInformation(3, "League Of Legends Current Patch: {ReturnVersionAsync}", await ClientVersion());

         await CrashLobby();
    }

    private async Task CrashLobby()
    {
        await QuitCustomLobby();

        var checkTimerSelectedChamp = await GetCheckTimerSelectedChamp();

        if (checkTimerSelectedChamp)
        {
            var customLobby = await CreateCustomLobby();
        
           var LobbyId = customLobby["id"].ToString();
           var gameTypeConfigId = customLobby["gameTypeConfigId"].ToString();

           await StartChampionSelection(LobbyId, gameTypeConfigId);
           await SetClientReceivedGameMessage(LobbyId);
           await SelectSpells(32, 4);
           await SelectChampionV2(1, 1000);
           await ChampionSelectCompleted();
           await Task.Delay(15000);
           await QuitCustomLobby();
           await SetClientReceivedMaestroMessage(LobbyId);
        }

    }

    private async Task SetClientReceivedMaestroMessage(string id)
    {
        var argsArray = new object[] { id, "GameClientConnectedToServer" };
        var argsJson = JsonSerializer.Serialize(argsArray);

        var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryParams["destination"] = "gameService";
        queryParams["method"] = "setClientReceivedMaestroMessage";
        queryParams["args"] = argsJson;
        
        await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?{queryParams}");
    }

    private async Task QuitCustomLobby()
    {
        await ExecuteAsync(HttpMethod.Post, "/lol-login/v1/session/invoke?destination=gameService&method=quitGame&args=%5B%5D");
    }

    private async Task ChampionSelectCompleted()
    {
        await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?destination=gameService&method=championSelectCompleted&args=%5B%5D");
    }

    private async Task SelectChampionV2(int i, int i1)
    {
        await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?destination=gameService&method=selectChampionV2&args=%5B%22{i}%22%2C%22{i1}%22%5D");
    }

    private async Task SelectSpells(int i, int i1)
    {
        await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?destination=gameService&method=selectSpells&args=%5B%22{i}%22%2C%22{i1}%22%5D");
    }

    private async Task SetClientReceivedGameMessage(string lobbyId)
    {
        await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?destination=gameService&method=setClientReceivedGameMessage&args=%5B%22{lobbyId}%22%2C%22CHAMP_SELECT_CLIENT%22%5D");
    }

    private async Task<Dictionary<string,object>> CreateCustomLobby()
    {
        var objectData = new Dictionary<string, object>
        {
            { "__class", "com.riotgames.platform.game.lcds.dto.CreatePracticeGameRequestDto" }
        };

        var practiceGameConfig = new Dictionary<string, object>
        {
            { "__class", "com.riotgames.platform.game.PracticeGameConfig" },
            { "allowSpectators", "NONE" },
            { "gameMap", new Dictionary<string, object>
                {
                    { "__class", "com.riotgames.platform.game.map.GameMap" },
                    { "description", "" },
                    { "displayName", "" },
                    { "mapId", 11 },
                    { "minCustomPlayers", 1 },
                    { "name", "" },
                    { "totalPlayers", 10 }
                }
            },
            { "gameMode", "CLASSIC" },
            { "gameMutators", new List<object>() },
            { "gameName", "test" },
            { "gamePassword", "" },
            { "gameTypeConfig", 1 },
            { "gameVersion", await GetClientVersion() },
            { "maxNumPlayers", 10 },
            { "passbackDataPacket", null },
            { "passbackUrl", null },
            { "region", "" }
        };

        objectData["practiceGameConfig"] = practiceGameConfig;
        objectData["simpleInventoryJwt"] = await GetSimpleInventoryJwt();

        var tokens = new Dictionary<string, object>
        {
            { "__class", "com.riotgames.platform.util.tokens.PlayerGcoTokens" },
            { "idToken", await GetIdToken() },
            { "userInfoJwt", await GetUserInfoJwt() },
            { "summonerToken", await GetSummonerToken() }
        };

        objectData["playerGcoTokens"] = tokens;

        var queryParams = new Dictionary<string, string>
        {
            { "destination", "gameService" },
            { "method", "createPracticeGameV4" },
            { "args", JsonSerializer.Serialize(new object[] { objectData }) }
        };

        var queryString = new StringBuilder();
        foreach (var kvp in queryParams)
        {
            queryString.Append($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}&");
        }
        queryString.Length--;
        
        
        var request = await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?{queryString}");
        
        var gameDTO = new Dictionary<string, object>
        {
            { "__class", "com.riotgames.platform.game.GameDTO" }
        };
        
        var jsonData = JsonDocument.Parse(request).RootElement.GetProperty("body").ToString();
        var bodyData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);

        foreach (var kvp in bodyData)
        {
            gameDTO[kvp.Key] = kvp.Value;
            Console.WriteLine(kvp);
        }

        return gameDTO;
    }

    private async Task StartChampionSelection(string gameId, string gameTypeConfigId)
    {
         await ExecuteAsync(HttpMethod.Post, $"/lol-login/v1/session/invoke?destination=gameService&method=startChampionSelection&args=%5B%22{gameId}%22%2C%22{gameTypeConfigId}%22%5D");
    }

    private async Task<string> GetSimpleInventoryJwt()
    {
        var request = await ExecuteAsync(HttpMethod.Get, "/lol-inventory/v1/champSelectInventory");
        
        using JsonDocument document = JsonDocument.Parse(request);
        var root = document.RootElement;

        return root.ToString();
    }

    private async Task<string> GetClientVersion()
    {
        var request = await ExecuteAsync(HttpMethod.Get, "/lol-patch/v1/game-version");
        
        using JsonDocument document = JsonDocument.Parse(request);
        var root = document.RootElement;

        return root.ToString();
    }

    private async Task<string> GetIdToken()
    {
        var request = await ExecuteAsync(HttpMethod.Get, "/lol-rso-auth/v1/authorization/id-token");
        using JsonDocument document = JsonDocument.Parse(request);
        var root = document.RootElement;

     //   Console.WriteLine(root);
        
        var token = root.GetProperty("token").ToString();

        //Console.WriteLine(token);
        
        return token;
    }
    
    private async Task<string> GetUserInfoJwt()
    {
        var request = await ExecuteAsync(HttpMethod.Get, "/lol-rso-auth/v1/authorization/access-token");
        using JsonDocument document = JsonDocument.Parse(request);
        var root = document.RootElement;

        var token = root.GetProperty("token").ToString();

        return token;
    }
    
    private async Task<string> GetSummonerToken()
    {
        var request = await ExecuteAsync(HttpMethod.Get, "/lol-league-session/v1/league-session-token");
     
        using JsonDocument document = JsonDocument.Parse(request);
        return document.RootElement.ToString();
    }
    
    private async Task<bool> GetCheckTimerSelectedChamp()
    {
        var session = await ExecuteAsync(HttpMethod.Get, "/lol-champ-select/v1/session");

        using JsonDocument document = JsonDocument.Parse(session);
        var root = document.RootElement;
        
        if (root.TryGetProperty("isCustomGame", out JsonElement isCustomGameElement) && isCustomGameElement.GetBoolean())
        {
            logger.LogInformation(3,"Can't be used on custom game!");
            return false;
        }
        
        var champSelect = await ExecuteAsync(HttpMethod.Get, "/lol-champ-select/v1/session/my-selection");
        using JsonDocument document2 = JsonDocument.Parse(champSelect);
        JsonElement root2 = document2.RootElement;
        
        if (root2.TryGetProperty("championId", out JsonElement championIdElement) && championIdElement.GetInt32() == 0)
        {
            logger.LogInformation(3,"You need to select champion!");
            return false;
        }
        
        if (root.TryGetProperty("timer", out JsonElement timerElement) && timerElement.TryGetProperty("phase", out JsonElement phaseElement) && phaseElement.GetString() == "FINALIZATION")
        {
            var time = DateTime.Now.Ticks;
            if (timerElement.TryGetProperty("internalNowInEpochMs", out JsonElement internalNowInEpochMsElement) &&
                timerElement.TryGetProperty("adjustedTimeLeftInPhase", out JsonElement adjustedTimeLeftInPhaseElement))
            {
                var remaining = (internalNowInEpochMsElement.GetInt64() + adjustedTimeLeftInPhaseElement.GetInt64()) - time;
                if (remaining > 11000)
                {
                    logger.LogInformation(3,"Need at least 11 seconds to activate it!");
                    return false;
                }
            }
        }

        return true;
    }
    
    private async Task<string?> ClientVersion()
    {
        var request = await _httpHandler.SendAsync("https://ddragon.leagueoflegends.com/api/versions.json", HttpMethod.Get);
        var deserialize = JsonNode.Parse(await request.Content.ReadAsStringAsync());

        return deserialize != null ? deserialize[0]?.GetValue<string>() : "";
    }

    private async Task<string> ExecuteAsync(HttpMethod method, string url, string? arguments = "{}")
    {
        var (appPort, encodedAuthToken) = await GetClientInfoAsync();
        
        var request = await _httpHandler.SendAsync($"https://127.0.0.1:{appPort}{url}", method, arguments, [
            new HttpHandler.RequestHeadersEx("Authorization", $"Basic {encodedAuthToken}"),
            new HttpHandler.RequestHeadersEx("User-Agent", "LeagueOfLegendsClient")

        ]);

        var response = await request.Content.ReadAsStringAsync();
        
        Console.WriteLine(response);
        
        return response;
    }

    private async Task<(string? appPort, string encodedAuthToken)> GetClientInfoAsync()
    {
        var process = Process.GetProcessesByName("LeagueClientUx").FirstOrDefault();

        if (process == null)
        {
            logger.LogError("LeagueClientUx was not found");
        }

        var managementObjectSearcher =
            new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

        var commandLine = await GetCommandLineAsync(managementObjectSearcher);

        Regex appPortRegex = new Regex("--app-port=(\\d+)");
        Regex remotingAuthTokenRegex = new Regex("--remoting-auth-token=(\\w+)");

        Match appPortMatch = appPortRegex.Match(commandLine);
        Match remotingAuthTokenMatch = remotingAuthTokenRegex.Match(commandLine);

        string? appPort = appPortMatch.Success ? appPortMatch.Groups[1].Value : null;
        string? remotingAuthToken = remotingAuthTokenMatch.Success ? remotingAuthTokenMatch.Groups[1].Value : null;

        if (string.IsNullOrEmpty(appPort) || string.IsNullOrEmpty(remotingAuthToken))
        {
            logger.LogError("Failed to extract app port or remoting auth token from command line");
        }

        string encodedAuthToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{remotingAuthToken}"));

        return (appPort, encodedAuthToken);
    }

    private async Task<string?> GetCommandLineAsync(ManagementObjectSearcher managementObjectSearcher)
    {
        string? commandLine = string.Empty;
        ManagementObject first = null;

        foreach (ManagementBaseObject o in managementObjectSearcher.Get())
        {
            ManagementObject managementObject = o as ManagementObject;
            if (managementObject != null)
            {
                first = managementObject;
                break;
            }
        }

        if (first != null)
        {
            await Task.Run(() =>
            {
                commandLine = first["CommandLine"].ToString();
            });
        }

        return commandLine;
    }
    
    //dotnet publish -r win-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
}
