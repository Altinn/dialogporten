namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public static class Utils
{
    public static string? GetSolutionRootFolder()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var solutionFolder = currentDirectory;
        while (solutionFolder != null && Directory.GetFiles(solutionFolder, "*.sln").Length == 0)
        {
            solutionFolder = Directory.GetParent(solutionFolder)?.FullName;
        }
        return solutionFolder;
    }
}
