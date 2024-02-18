using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace SpaceWarsServices;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string token;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<JoinGameResponse> JoinGameAsync(string name)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/game/join?name={Uri.EscapeDataString(name)}");

            response.EnsureSuccessStatusCode(); // Throw an exception if the status code is not a success code

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<JoinGameResponse>(content);
            token = result.Token;

            return result;
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    public async Task<GameStateResponse> GetGameState()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/game/state");

            response.EnsureSuccessStatusCode(); // Throw an exception if the status code is not a success code

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GameStateResponse>(content);
            var locations = result.PlayerLocations;


            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }
    //

public async Task<List<Location>> GetNearestPlayers()
{
    try
    {
        var response = await _httpClient.GetAsync($"/game/state");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var gameState = JsonConvert.DeserializeObject<GameStateResponse>(content);

        var closestToMe = gameState.PlayerLocations
            .OrderBy(p => Math.Pow(p.X - Program.myLocation.X, 2) + Math.Pow(p.Y - Program.myLocation.Y, 2))
            .First();

        Program.myLocation = closestToMe;

        var otherPlayers = gameState.PlayerLocations
            .Where(p => p.X != Program.myLocation.X || p.Y != Program.myLocation.Y)
            .ToList();

        var sortedLocations = otherPlayers
            .OrderBy(p => Math.Sqrt(Math.Pow(p.X - Program.myLocation.X, 2) + Math.Pow(p.Y - Program.myLocation.Y, 2)))
            .Take(20) 
            .ToList();

        return sortedLocations;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        throw;
    }
}




    public async Task QueueAction(IEnumerable<QueueActionRequest> action)
    {
        try
        {
            string url = $"/game/{token}/queue";
            var response = await _httpClient.PostAsJsonAsync(url, action);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task ClearAction()
    {
        try
        {
            string url = $"/game/{token}/queue/clear";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task<IEnumerable<GameMessage>> ReadAndEmptyMessages()
    {
        try
        {
            string url = $"/game/playermessages?token={token}";
            return await _httpClient.GetFromJsonAsync<IEnumerable<GameMessage>>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}
