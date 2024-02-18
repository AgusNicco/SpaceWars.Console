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
                .Take(Program.nearestClust)
                .ToList();

            return sortedLocations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    public Location FindSafestLocation(IEnumerable<Location> enemyLocations)
    {
        // Calculate centroid of enemy locations
        var centroidX = enemyLocations.Average(loc => loc.X);
        var centroidY = enemyLocations.Average(loc => loc.Y);

        var mapCorners = new List<Location>
        {
        new Location(0, 0),
        new Location(0, 500),
        new Location(500, 0),
        new Location(500, 500)
        };

        var furthestCorner = mapCorners
            .OrderByDescending(corner => Math.Sqrt(Math.Pow(corner.X - centroidX, 2) + Math.Pow(corner.Y - centroidY, 2)))
            .First();

        return furthestCorner;
    }

    public Location FindRecommendedVictim(IEnumerable<Location> playerLocations)
    {

        Dictionary<Location, double> closestDistances = new Dictionary<Location, double>();

        foreach (var player in playerLocations)
        {
            double closestDistance = double.MaxValue;

            foreach (var otherPlayer in playerLocations)
            {
                if (player != otherPlayer)
                {
                    double distance = CalculateDistance(player, otherPlayer);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                    }
                }
            }

            closestDistances[player] = closestDistance;
        }
        var recommendedVictim = closestDistances.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

        return recommendedVictim;
    }

    private double CalculateDistance(Location a, Location b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }


    public Location FindLargestPlayerClusterCenter(IEnumerable<Location> playerLocations)
    {
        int mapSize = 500;

        // Calculate the number of quadrants per row and column
        int quadrantsPerSide = (int)Math.Sqrt(Program.dangerousQuadrants);
        int quadrantSize = mapSize / quadrantsPerSide;

        var quadrantCounts = new int[quadrantsPerSide, quadrantsPerSide];

        foreach (var location in playerLocations)
        {
            int xQuadrant = Math.Min(location.X / quadrantSize, quadrantsPerSide - 1);
            int yQuadrant = Math.Min(location.Y / quadrantSize, quadrantsPerSide - 1);
            quadrantCounts[xQuadrant, yQuadrant]++;
        }

        // Find the quadrant with the highest player count
        int maxCount = 0;
        Location maxQuadrantCenter = new Location(0, 0);
        for (int x = 0; x < quadrantsPerSide; x++)
        {
            for (int y = 0; y < quadrantsPerSide; y++)
            {
                if (quadrantCounts[x, y] > maxCount)
                {
                    maxCount = quadrantCounts[x, y];
                    // Calculate the center of the quadrant
                    int centerX = (x * quadrantSize) + (quadrantSize / 2);
                    int centerY = (y * quadrantSize) + (quadrantSize / 2);
                    maxQuadrantCenter = new Location(centerX, centerY);
                }
            }
        }

        return maxQuadrantCenter;
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
