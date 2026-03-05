using HackerrankCSharp.Exercises;
using HackerrankCSharp.Resources;

namespace HackerrankCSharp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await RunMainMenuAsync();
    }

    private static async Task RunMainMenuAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== Hackerrank C# Menu ===");
            Console.WriteLine("1. Basic");
            Console.WriteLine("2. Collections");
            Console.WriteLine("3. OOP");
            Console.WriteLine("4. Formatting");
            Console.WriteLine("5. Disposable demos");
            Console.WriteLine("0. Exit");
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    RunBasicMenu();
                    break;
                case "2":
                    RunCollectionsMenu();
                    break;
                case "3":
                    RunOopMenu();
                    break;
                case "4":
                    RunFormattingMenu();
                    break;
                case "5":
                    await RunDisposableDemoAsync();
                    break;
                case "0":
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private static void RunBasicMenu()
    {
        while (true)
        {
            Console.WriteLine("--- Basic ---");
            Console.WriteLine("1. Arrays");
            Console.WriteLine("2. BasicDataTypes");
            Console.WriteLine("3. Functions");
            Console.WriteLine("4. Strings");
            Console.WriteLine("0. Back");
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    var result = Arrays.Solve("4 1 4 3 2");
                    Console.WriteLine("Arrays.Solve(\"4 1 4 3 2\") => " + result);
                    break;
                case "2":
                    var formatted = BasicDataTypes.FormatOutput("3 123456789123 123456789123456789 A 334.23 14049.30493");
                    Console.WriteLine("BasicDataTypes output:");
                    Console.WriteLine(formatted);
                    break;
                case "3":
                    Console.WriteLine("Functions.MaxOfFour(3, 4, 6, 5) => " + Functions.MaxOfFour(3, 4, 6, 5));
                    break;
                case "4":
                    var (aLength, bLength, concat, swapped) = Strings.Solve("abcd", "ef");
                    Console.WriteLine($"Strings.Solve(\"abcd\", \"ef\") => lengths: {aLength}, {bLength}; concat: {concat}; swapped: {swapped}");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void RunCollectionsMenu()
    {
        while (true)
        {
            Console.WriteLine("--- Collections ---");
            Console.WriteLine("1. VectorSort");
            Console.WriteLine("2. LowerBoundStl");
            Console.WriteLine("0. Back");
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    var sorted = VectorSort.Sort([4, 1, 3, 9, 7]);
                    Console.WriteLine("VectorSort.Sort([4, 1, 3, 9, 7]) => " + string.Join(' ', sorted));
                    break;
                case "2":
                    var (found, idx) = LowerBoundStl.LowerBound([1, 4, 6, 8, 9], 6);
                    Console.WriteLine($"LowerBoundStl.LowerBound([1, 4, 6, 8, 9], 6) => {(found ? "Yes" : "No")} {idx}");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void RunOopMenu()
    {
        while (true)
        {
            Console.WriteLine("--- OOP ---");
            Console.WriteLine("1. RectangleArea");
            Console.WriteLine("2. Box");
            Console.WriteLine("0. Back");
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    var rectangle = new RectangleArea { Width = 3, Height = 5 };
                    Console.WriteLine("RectangleArea.Display() => " + rectangle.Display());
                    break;
                case "2":
                    var box = new Box(1, 2, 3);
                    Console.WriteLine($"Box(1, 2, 3) => {box}; volume: {box.CalculateVolume()}");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void RunFormattingMenu()
    {
        while (true)
        {
            Console.WriteLine("--- Formatting ---");
            Console.WriteLine("1. PrintPretty");
            Console.WriteLine("0. Back");
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    var (a, b, c) = PrintPretty.Format(100, -12345.6789, 12345.6789);
                    Console.WriteLine("PrintPretty.Format(100, -12345.6789, 12345.6789):");
                    Console.WriteLine(a);
                    Console.WriteLine(b);
                    Console.WriteLine(c);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static async Task RunDisposableDemoAsync()
    {
        Console.WriteLine("Running disposable demos...");

        using (var fileLogger = new FileLogger("demo.log"))
        {
            fileLogger.Log("using statement disposed at scope end");
        }

        using var counter = new ResourceCounter();
        counter.Increment();
        Console.WriteLine($"ResourceCounter.Count => {counter.Count}");

        await using var asyncTimer = new AsyncTimer();
        await asyncTimer.DelayAndPrintAsync(25);

        await DisposableExamples.RunAsync();

        Console.WriteLine("Disposable demos complete.");
    }
}
