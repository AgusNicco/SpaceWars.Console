﻿using SWConsole;
using System.Diagnostics;

namespace SpaceWarsServices;

class Program
{
    public static Location myLocation { get; set; }
    public static int myHeading { get; set; }
    static async Task Main(string[] args)
    {
        //**************************************************************************************
        //***  |    |    |    |                                            |    |    |    |    |
        //***  |    |    |    |       Change your key mappings here        |    |    |    |    |
        //***  V    V    V    V                                            V    V    V    V    V
        //**************************************************************************************
        const ConsoleKey forwardKey = ConsoleKey.UpArrow;
        const ConsoleKey leftKey = ConsoleKey.LeftArrow;
        const ConsoleKey rightKey = ConsoleKey.RightArrow;
        const ConsoleKey fireKey = ConsoleKey.Spacebar;
        const ConsoleKey clearQueueKey = ConsoleKey.C;
        const ConsoleKey infoKey = ConsoleKey.I;
        const ConsoleKey shopKey = ConsoleKey.S;
        const ConsoleKey repairKey = ConsoleKey.R;
        const ConsoleKey readAndEmptyMessagesKey = ConsoleKey.M;

        Uri baseAddress = getApiBaseAddress(args);
        using HttpClient httpClient = new HttpClient() { BaseAddress = baseAddress };
        bool exitGame = false;
        var currentHeading = 0;
        var token = "";
        var service = new ApiService(httpClient);
        List<PurchasableItem> Shop = new List<PurchasableItem>();
        JoinGameResponse joinGameResponse = null;


        Console.WriteLine("Please enter your name");
        var username = Console.ReadLine();
        try
        {
            joinGameResponse = await service.JoinGameAsync(username);
            token = joinGameResponse.Token;

            Shop = joinGameResponse.Shop.Select(item => new PurchasableItem(item.Cost, item.Name, item.Prerequisites)).ToList();

            myLocation = joinGameResponse.StartingLocation;
            myHeading = joinGameResponse.Heading;
            var response2 = service.GetNearestPlayers();
            Console.WriteLine(response2);

            Console.WriteLine($"Token:{joinGameResponse.Token}, Heading: {joinGameResponse.Heading}");
            Console.WriteLine($"Ship located at: {joinGameResponse.StartingLocation}, Game State is: {joinGameResponse.GameState}, Board Dimensions: {joinGameResponse.BoardWidth}, {joinGameResponse.BoardHeight}");

            OpenUrlInBrowser($"{baseAddress.AbsoluteUri}hud?token={token}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        var gameActions = new GameActions(username, joinGameResponse, service);
        gameActions.Weapons.Add("Basic Cannon");
        gameActions.CurrentWeapon = "Basic Cannon";

        while (!exitGame)
        {
            printStatus();
            ConsoleKeyInfo keyInfo = Console.ReadKey(true); // Read key without displaying it
            bool shiftPressed = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift);

            switch (keyInfo.Key)
            {
                case var key when key == forwardKey:
                    await gameActions.MoveForwardAsync(shiftPressed);
                    break;
                case var key when key == leftKey:
                    await gameActions.RotateLeftAsync(shiftPressed);
                    break;
                case var key when key == rightKey:
                    await gameActions.RotateRightAsync(shiftPressed);
                    break;
                case var key when key == fireKey:
                    await gameActions.FireWeaponAsync();
                    break;
                case var key when key == clearQueueKey:
                    await gameActions.ClearQueueAsync();
                    break;
                case var key when key == repairKey:
                    await gameActions.RepairShipAsync();
                    Console.WriteLine("Ship repair requested.");
                    break;
                case var key when key == infoKey:
                    foreach (var item in Shop)
                    {
                        Console.WriteLine($"upgrade: {item.Name}, cost: {item.Cost}");
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                    break;
                case var key when key == shopKey:

                    Console.WriteLine("please enter what you'd like to purchase from the shop, (if you've changed your mind enter x)");
                    var response = Console.ReadLine();
                    if (response == "x")
                    {
                        continue;
                    }

                    if (Shop.Any(item => item.Name.Equals(response, StringComparison.OrdinalIgnoreCase)))
                    {
                        await gameActions.PurchaseItemAsync(response);
                        Console.WriteLine($"Purchase of {response} requested.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid item. Please choose a valid item from the shop.");
                    }
                    break;
                case var key when key == readAndEmptyMessagesKey:
                    await gameActions.ReadAndEmptyMessagesAsync();
                    Console.WriteLine("Message queue read.");
                    break;
                case var key when key >= ConsoleKey.D0 && key <= ConsoleKey.D9:
                    gameActions.SelectWeapon(key);
                    Console.WriteLine($"Selected weapon {((char)key) - '1'} ({gameActions.CurrentWeapon}");
                    break;
                //**************************************************************************************
                //***  |    |    |    |                                            |    |    |    |    |
                //***  |    |    |    |       Add any other custom keys here       |    |    |    |    |
                //***  V    V    V    V                                            V    V    V    V    V
                //**************************************************************************************
                case var key when key == ConsoleKey.W:
                    await gameActions.MoveForwardAsync(shiftPressed);
                    break;
                case var key when key == ConsoleKey.A:
                    await gameActions.RotateLeftAsync(shiftPressed);
                    break;
                case var key when key == ConsoleKey.D:
                    await gameActions.RotateRightAsync(shiftPressed);
                    break;
                case var key when key == ConsoleKey.Q:
                    await gameActions.Rotate30LeftAsync();
                    break;
                case var key when key == ConsoleKey.E:
                    await gameActions.Rotate30RightAsync();
                    break;
                case var key when key == ConsoleKey.X:
                    await gameActions.FastForwardAsync();
                    break;
                case ConsoleKey.Z:
                    await gameActions.AimAtClosestPlayerAsync(service);
                    break;
            }
        }

        async void printStatus()
        {
            Console.Clear();
            Console.WriteLine($"Name: {username,-34} Token: {gameActions.Token}");
            Console.WriteLine($"Left: {leftKey,-12} Right: {rightKey,-12} Forward: {forwardKey,-12} Fire: {fireKey,-12} Clear Queue: {clearQueueKey,-12}");
            Console.WriteLine($"Info: {infoKey,-12}  Shop: {shopKey,-12}  Repair: {repairKey,-12} Read & Empty Messages: {readAndEmptyMessagesKey,-12}");
            Console.WriteLine($"Aimbot: Z");

            for (int i = 0; i < gameActions.Weapons.Count; i++)
            {
                string? weapon = gameActions.Weapons[i];
                if (weapon == gameActions.CurrentWeapon)
                {
                    weapon = $"**{weapon}**";
                }
                Console.Write($"{i + 1}: {weapon}   ");
            }
            Console.WriteLine();


            if (gameActions.GameMessages.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Last 10 messages:");
                Console.WriteLine(new string('-', Console.WindowWidth));
                foreach (var msg in gameActions.GameMessages.TakeLast(10))
                {
                    Console.WriteLine($"{msg.Type,-30} {msg.Message}");
                }
            }

            Console.WriteLine(new string('=', Console.WindowWidth));

            var nearestPlayers = await service.GetNearestPlayers();
            var safestLocation = service.FindSafestLocation(nearestPlayers);
            var mostDangerousLocation = service.FindLargestPlayerClusterCenter(nearestPlayers);

            Console.WriteLine($"My location: {myLocation.X}, {myLocation.Y}, Heading: {myHeading}");
            Console.WriteLine($"Safest location: {safestLocation.X}, {safestLocation.Y}");
            Console.WriteLine($"Most dangerous location: {mostDangerousLocation.X}, {mostDangerousLocation.Y}");
            DisplayClustersMatrix(nearestPlayers);
            DisplayMap(nearestPlayers);
            Console.WriteLine("\nNearest players:");


            foreach (var location in nearestPlayers)
            {
                Console.WriteLine($"Player at  {location.X}, {location.Y}");
            }
        }
    }

    public static void DisplayClustersMatrix(IEnumerable<Location> playerLocations)
    {
        int mapSize = 500;
        int quadrantSize = mapSize / 3;
        int[,] quadrantCounts = new int[3, 3];


        foreach (var location in playerLocations)
        {
            int xQuadrant = Math.Min(location.X / quadrantSize, 2);
            int yQuadrant = Math.Min(location.Y / quadrantSize, 2);
            quadrantCounts[xQuadrant, yQuadrant]++;
        }


        Console.WriteLine("Clusters Matrix:");
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Console.Write(quadrantCounts[x, y] + "\t");
            }
            Console.WriteLine(); // New line for each row
        }
    }
    public static void DisplayMap(IEnumerable<Location> playerLocations)
{
    int gridSize = 25;
    int mapSize = 500;
    char[,] map = new char[gridSize, gridSize];

    // Initialize the map
    for (int y = 0; y < gridSize; y++)
    {
        for (int x = 0; x < gridSize; x++)
        {
            map[x, y] = '.';
        }
    }

    // Calculate scale factors to fit the map into the 25x25 grid
    double scaleX = mapSize / (double)gridSize;
    double scaleY = mapSize / (double)gridSize;

    // Mark player locations on the map
    foreach (var location in playerLocations)
    {
        int gridX = (int)(location.X / scaleX);
        int gridY = (int)(location.Y / scaleY);

        // Ensure the position is within the bounds of the map grid
        if (gridX >= 0 && gridX < gridSize && gridY >= 0 && gridY < gridSize)
        {
            map[gridX, gridY] = '*'; // Mark player location
        }
    }

    // Mark your location with an X
    int myGridX = (int)(myLocation.X / scaleX);
    int myGridY = (int)(myLocation.Y / scaleY);

    if (myGridX >= 0 && myGridX < gridSize && myGridY >= 0 && myGridY < gridSize)
    {
        map[myGridX, myGridY] = 'X'; // Mark your location
    }

    // Display the map
    for (int y = 0; y < gridSize; y++)
    {
        for (int x = 0; x < gridSize; x++)
        {
            Console.Write(map[x, y] + " ");
        }
        Console.WriteLine();
    }
}



    private static Uri getApiBaseAddress(string[] args)
    {
        Uri baseAddress;
        if (args.Length == 0)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Please enter the URL to access Space Wars");
                    baseAddress = new Uri(Console.ReadLine());
                    break;
                }
                catch { }
            }
        }
        else
        {
            baseAddress = new Uri(args[0]);
        }
        return baseAddress;
    }

    static void OpenUrlInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL in browser: {ex.Message}");
        }
    }
}
