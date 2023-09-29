using System.CommandLine;
internal enum Format
{
  None,
  Markdown
}

internal class Program
{
  private static async Task<int> Main(string[] args)
  {
    var rootCommand = new RootCommand("Sample app for System.CommandLine");
    return await rootCommand.InvokeAsync(args);
  }
}