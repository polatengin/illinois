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
    var outputFormatOption = new Option<Format>(aliases: new[] { "--output-format" }, description: "The format to use for the output");
    var bicepFileOption = new Option<FileInfo>(aliases: new[] { "--bicep-file" }, description: "The format to use for the output");

    var rootCommand = new RootCommand("Auto generate documentation for the given bicep file");

    rootCommand.AddOption(serveOption);
    rootCommand.AddOption(sortOption);
    rootCommand.AddOption(outputFormatOption);
    rootCommand.AddOption(bicepFileOption);

    rootCommand.SetHandler((serve, sort, format, file) =>
    {
      if (serve)
      {
        Serve();
      }
    },
    serveOption, sortOption, outputFormatOption, bicepFileOption);

    return await rootCommand.InvokeAsync(args);
  }
  private static void Serve()
  {
      throw new ArgumentException("Serve feature is not implemented yet");
  }
}
