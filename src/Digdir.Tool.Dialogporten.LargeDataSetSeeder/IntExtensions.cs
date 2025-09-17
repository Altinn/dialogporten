namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class IntExtensions
{
    public static bool PercentOfTheTime(this int value) => Random.Shared.Next(1, 101) <= value;
}
