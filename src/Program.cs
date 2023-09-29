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
    var serveOption = new Option<bool>(aliases: new[] { "--serve" }, description: "The serve to read and display on the console");
    var sortOption = new Option<bool>(aliases: new[] { "--sort" }, description: "Sort members alphabetically");
    return await rootCommand.InvokeAsync(args);
  }
}