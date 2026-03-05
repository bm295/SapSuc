namespace HackerrankCSharp.Exercises;

public static class Arrays
{
    public static int[] Reverse(int[] values) => values.Reverse().ToArray();

    public static string Solve(string input)
    {
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var n = int.Parse(tokens[0]);
        var values = tokens.Skip(1).Take(n).Select(int.Parse).ToArray();
        return string.Join(' ', Reverse(values));
    }
}
