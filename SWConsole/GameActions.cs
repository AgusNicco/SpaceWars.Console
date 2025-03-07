﻿using SpaceWarsServices;

namespace SWConsole;
public enum Direction { Right, Left }

public class GameActions
{
    private readonly JoinGameResponse joinGameResponse;
    private readonly ApiService apiService;
    private int heading;

    public GameActions(string playerName, JoinGameResponse joinGameResponse, ApiService apiService)
    {
        this.joinGameResponse = joinGameResponse;
        this.apiService = apiService;
        heading = joinGameResponse.Heading;
        PlayerName = playerName;
    }
    public async Task RotateLeftAsync(bool quickTurn) => await rotate(Direction.Left, quickTurn);

    public async Task RotateRightAsync(bool quickTurn) => await rotate(Direction.Right, quickTurn);

    public async Task Rotate30LeftAsync() => await rotate30(Direction.Left);
    public async Task Rotate30RightAsync() => await rotate30(Direction.Right);

    private async Task rotate(Direction direction, bool quickTurn)
    {
        heading = (direction, quickTurn) switch
        {
            (Direction.Right, true) => heading + 10,
            (Direction.Right, false) => heading + 1,
            (Direction.Left, true) => heading - 10,
            (Direction.Left, false) => heading - 1,
            _ => 0,//turn north if someone calls this with a bogus Direction
        };
        heading = ClampRotation(heading);
        Program.myHeading = heading;
        await apiService.QueueAction([new("changeHeading", heading.ToString())]);
    }

    private async Task rotate30(Direction direction)
    {
        heading = (direction) switch
        {
            (Direction.Right) => heading + 30,
            (Direction.Left) => heading - 30,
            _ => 0,//turn north if someone calls this with a bogus Direction
        };

        heading = ClampRotation(heading);
        Program.myHeading = heading;
        await apiService.QueueAction([new("changeHeading", heading.ToString())]);
    }

    public async Task AimAtClosestPlayerAsync(ApiService service)
    {
        var nearbyPlayers = await service.GetNearestPlayers();
        if (!nearbyPlayers.Any()) return;

        var closestPlayer = nearbyPlayers[0];

        int targetHeading = CalculateHeadingToTarget(Program.myLocation, closestPlayer);
        Program.myHeading = targetHeading;

        await apiService.QueueAction([new QueueActionRequest("changeHeading", targetHeading.ToString())]);
    }

    private int CalculateHeadingToTarget(Location myLocation, Location targetLocation)
    {
        int my_x = myLocation.Y;
        int my_y = myLocation.X;
        int target_x = targetLocation.Y;
        int target_y = targetLocation.X;

        double angle = Math.Atan2(target_y - my_y, target_x - my_x);
        int degrees = (int)(angle * (180 / Math.PI));
        return ClampRotation(degrees);
        
        double deltaX = targetLocation.X - myLocation.X;
        double deltaY = targetLocation.Y - myLocation.Y;
        double targetAngleRadians = Math.Atan2(-deltaX, deltaY);
        int targetAngleDegrees = (int)(targetAngleRadians * (180 / Math.PI));
        targetAngleDegrees = (targetAngleDegrees + 360) % 360;
        return targetAngleDegrees;
    }


    public async Task MoveForwardAsync(bool lightSpeed)
    {
        double distance = lightSpeed ? 10 : 1;
        double radians = Math.PI * Program.myHeading / 180.0;

        int deltaX = (int)Math.Round(distance * Math.Sin(radians));
        int deltaY = (int)Math.Round(distance * Math.Cos(radians));

        Program.myLocation = new Location(Program.myLocation.X + deltaX, Program.myLocation.Y + deltaY);

        heading = Program.myHeading;
        heading = ClampRotation(heading);
        var actions = Enumerable.Range(0, lightSpeed ? 10 : 1)
                .Select(n => new QueueActionRequest("move", heading.ToString()));
        await apiService.QueueAction(actions);
    }


    public async Task FastForwardAsync()
    {
        heading = Program.myHeading;
        var fastForwardDistance = 5;
        var actions = Enumerable.Range(0, fastForwardDistance)
                .Select(n => new QueueActionRequest("move", heading.ToString()));
        await apiService.QueueAction(actions);
    }

    public async Task FireWeaponAsync(string? weapon = null) => await apiService.QueueAction([new("fire", weapon ?? CurrentWeapon)]);

    public async Task RepairShipAsync() => await apiService.QueueAction([new("repair", null)]);

    public async Task ClearQueueAsync() => await apiService.ClearAction();

    public async Task PurchaseItemAsync(string item) => await apiService.QueueAction([new("purchase", item)]);

    private static int ClampRotation(int degrees)
    {
        degrees %= 360;
        if (degrees < 0)
            degrees += 360;
        return degrees;
    }

    internal async Task ReadAndEmptyMessagesAsync()
    {
        var messages = await apiService.ReadAndEmptyMessages();
        GameMessages.AddRange(messages);

        foreach (var weaponPurchaseMessage in messages.Where(m => m.Type == "SuccessfulWeaponPurchase"))
        {
            Weapons.Add(weaponPurchaseMessage.Message);
        }
    }

    internal void SelectWeapon(ConsoleKey key)
    {
        char c = (char)key;
        int index = c - '1';
        if (Weapons.Count > index)
        {
            CurrentWeapon = Weapons[index];
        }
    }

    public List<string> Weapons { get; set; } = new();
    public string CurrentWeapon { get; set; }
    public List<GameMessage> GameMessages { get; set; } = new();
    public string PlayerName { get; set; }
    public string Token => joinGameResponse.Token;
}

public static class IEnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }
}

