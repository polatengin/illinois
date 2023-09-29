using System.Text.Json;
using System.Diagnostics;
using System.CommandLine;
using System.Text;

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
      ValidateOptions(serve, sort, format, file);

      GenerateDocumentation(sort, format, file);

      if (serve)
      {
        Serve();
      }
    },
    serveOption, sortOption, outputFormatOption, bicepFileOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static void ValidateOptions(bool serve, bool sort, Format format, FileInfo file)
  {
    if (format == Format.None)
    {
      throw new ArgumentException("output format cannot be none");
    }

    if (file == null)
    {
      throw new ArgumentException("The bicep file option is required");
    }
  }

  private static void GenerateDocumentation(bool sort, Format format, FileInfo file)
  {
    switch (format)
    {
      case Format.Markdown:
        GenerateMarkdownDocumentation(sort, file);
        break;
      default:
        throw new ArgumentException($"Unsupported format: {format}");
    }
  }

  private static void GenerateMarkdownDocumentation(bool sort, FileInfo file)
  {
    var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

    var process = new Process();
    process.StartInfo.FileName = "az";
    process.StartInfo.Arguments = $"bicep build --file {file} --outfile {tempFile}";
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = false;
    process.Start();

    process.WaitForExit();

    if (process.ExitCode == 0)
    {
      var rawContent = File.ReadAllText(tempFile);
      var root = JsonSerializer.Deserialize<Root>(rawContent);

      if (root == null)
      {
        throw new ArgumentException($"Failed to deserialize the generated bicep file: {tempFile}");
      }

    }
    else
    {
      throw new ArgumentException($"Failed to generate documentation for file: {file}");
    }
  }

  private static void Serve()
  {
      throw new ArgumentException("Serve feature is not implemented yet");
  }
}
