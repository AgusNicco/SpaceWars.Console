using SWConsole;
using System.Diagnostics;

namespace SpaceWarsServices;

class Program
{
    public static Location myLocation { get; set; }
    public static int myHeading { get; set; }

    public static int mapSize { get; set; }
    public static int nearestNeighbors { get; set; }
    public static int nearestClust { get; set; }
    
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
        Console.WriteLine($"Enter the number of nearest neighbors you want displayed:");
        nearestNeighbors = int.Parse(Console.ReadLine());
        Console.WriteLine($"Enter the number of nearest neighbors used for calculation of centroid and clusters");
        nearestClust = int.Parse(Console.ReadLine());
        Console.WriteLine($"Enter the size of the map (10-100)");
        mapSize = int.Parse(Console.ReadLine());

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
            DisplayMap(nearestPlayers, myLocation, safestLocation);
            Console.WriteLine("\nNearest players:");
            int n = nearestNeighbors;
            foreach (var location in nearestPlayers)
            {
                if (n-- == 0)
                {
                    break;
                }
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
            Console.WriteLine(); 
        }
    }


public static void DisplayMap(IEnumerable<Location> playerLocations, Location myLocation, Location safestLocation)
{
    int gridSize = Program.mapSize;
    int mapSize = 500;
    char[,] map = new char[gridSize, gridSize];

    for (int y = 0; y < gridSize; y++)
    {
        for (int x = 0; x < gridSize; x++)
        {
            map[x, y] = '.';
        }
    }

    double scaleX = mapSize / (double)gridSize;
    double scaleY = mapSize / (double)gridSize;


    foreach (var location in playerLocations)
    {
        int gridX = (int)(location.X / scaleX);
        int gridY = (int)(location.Y / scaleY);

        if (gridX >= 0 && gridX < gridSize && gridY >= 0 && gridY < gridSize)
        {
            map[gridX, gridY] = 'X';
        }
    }

    int myGridX = (int)(myLocation.X / scaleX);
    int myGridY = (int)(myLocation.Y / scaleY);

    if (myGridX >= 0 && myGridX < gridSize && myGridY >= 0 && myGridY < gridSize)
    {
        map[myGridX, myGridY] = '*';
    }

    int safeGridX = (int)(safestLocation.X / scaleX);
    int safeGridY = (int)(safestLocation.Y / scaleY);

    if (safeGridX >= 0 && safeGridX < gridSize && safeGridY >= 0 && safeGridY < gridSize)
    {
        map[safeGridX, safeGridY] = 'S';
    }

    for (int y = 0; y < gridSize; y++)
    {
        for (int x = 0; x < gridSize; x++)
        {
            if (map[x, y] == '*')
            {
                Console.ForegroundColor = ConsoleColor.Green; // My location
            }
            else if (map[x, y] == 'X')
            {
                Console.ForegroundColor = ConsoleColor.Red; // Player locations
            }
            else if (map[x, y] == 'S')
            {
                Console.ForegroundColor = ConsoleColor.Yellow; // Safest location
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White; // Empty spaces
            }
            Console.Write(map[x, y] + " ");
        }
        Console.WriteLine();
    }
    Console.ResetColor(); 
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
