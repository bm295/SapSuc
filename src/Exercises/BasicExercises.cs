using System.Globalization;

namespace HackerrankCSharp.Exercises;

public static class InputOutput
{
    public static int SumOfThree(int a, int b, int c) => a + b + c;
}

public static class BasicDataTypes
{
    public static string FormatOutput(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var a = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var b = long.Parse(parts[1], CultureInfo.InvariantCulture);
        var c = long.Parse(parts[2], CultureInfo.InvariantCulture);
        var d = parts[3];
        var e = float.Parse(parts[4], CultureInfo.InvariantCulture);
        var f = double.Parse(parts[5], CultureInfo.InvariantCulture);

        return string.Join('\n',
            a,
            b,
            c,
            d,
            e.ToString("F3", CultureInfo.InvariantCulture),
            f.ToString("F9", CultureInfo.InvariantCulture));
    }
}

public static class ConditionalStatements
{
    private static readonly string[] Words =
        ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"];

    public static string NumberToWord(int n) => n is > 0 and < 10 ? Words[n] : "Greater than 9";
}

public static class ForLoop
{
    public static IEnumerable<string> DescribeRange(int a, int b)
    {
        for (var i = a; i <= b; i++)
        {
            yield return i is > 9 ? (i % 2 == 0 ? "even" : "odd") : ConditionalStatements.NumberToWord(i);
        }
    }
}

public static class Functions
{
    public static int MaxOfFour(int a, int b, int c, int d) => new[] { a, b, c, d }.Max();
}

public static class Pointer
{
    public static (int sum, int absDiff) Update(int a, int b) => (a + b, Math.Abs(a - b));
}

public static class Strings
{
    public static (int aLength, int bLength, string concat, string swapped) Solve(string a, string b)
    {
        var swapped = $"{b[0]}{a[1..]} {a[0]}{b[1..]}";
        return (a.Length, b.Length, a + b, swapped);
    }
}

public static class StringStream
{
    public static List<int> ParseInts(string str) => str.Split(',').Select(int.Parse).ToList();
}
