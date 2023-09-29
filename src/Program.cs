using System.Text.Json;
using System.Diagnostics;
using System.CommandLine;
using System.Text;

internal enum OutputFormat
{
  None,
  Markdown
}

internal class Program
{
  private static async Task<int> Main(string[] args)
  {
    var serveOption = new Option<bool>(aliases: new[] { "--serve" }, description: "Serve the generated documentation on localhost");
    var sortOption = new Option<bool>(aliases: new[] { "--sort" }, description: "Sort members alphabetically");
    var outputFormatOption = new Option<OutputFormat>(aliases: new[] { "--output-format" }, description: $"The format to use for the output, valid options are ({string.Join(" | ", Enum.GetNames(typeof(OutputFormat)))})");
    var bicepFileOption = new Option<FileInfo>(aliases: new[] { "--bicep-file" }, description: "Bicep file to generate documentation for");

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

  private static void ValidateOptions(bool serve, bool sort, OutputFormat format, FileInfo file)
  {
    if (format == OutputFormat.None)
    {
      throw new ArgumentException("output format cannot be none");
    }

    if (file == null)
    {
      throw new ArgumentException("The bicep file option is required");
    }
  }

  private static void GenerateDocumentation(bool sort, OutputFormat format, FileInfo file)
  {
    switch (format)
    {
      case OutputFormat.Markdown:
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

      var stream = File.Create(file.FullName.Replace(".bicep", ".md"));

      stream.Write(Encoding.UTF8.GetBytes($"# {file.Name}\n\n"));

      if (root.metadata != null)
      {
        if (root.metadata._EXPERIMENTAL_FEATURES_ENABLED.Any())
        {
          stream.Write(Encoding.UTF8.GetBytes($"> {root.metadata._EXPERIMENTAL_WARNING}\n\n"));
          stream.Write(Encoding.UTF8.GetBytes($"_Enabled experimental features:_\n\n"));
          foreach (var item in root.metadata._EXPERIMENTAL_FEATURES_ENABLED)
          {
            stream.Write(Encoding.UTF8.GetBytes($"- {item}\n"));
          }
          stream.Write(Encoding.UTF8.GetBytes($"\n"));
        }
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Table of Contents\n\n"));

      stream.Write(Encoding.UTF8.GetBytes($"- [Variables](#variables)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Parameters](#parameters)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Resources](#resources)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Outputs](#outputs)\n"));

      stream.Write(Encoding.UTF8.GetBytes($"\n"));

      var resourceTypes = new List<(string, string)>();
      foreach (var item in root.resources.EnumerateObject())
      {
        var propertyType = item.Value.GetProperty("type").GetString();
        var apiVersion = item.Value.GetProperty("apiVersion").GetString();

        resourceTypes.Add((propertyType, apiVersion));
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Resource Types\n\n"));
      stream.Write(Encoding.UTF8.GetBytes($"Resource Types used in the bicep file\n\n"));
      stream.Write(Encoding.UTF8.GetBytes($"|Resource Type|API Version|\n"));
      stream.Write(Encoding.UTF8.GetBytes($"|---|---|\n"));
      foreach (var item in resourceTypes.Distinct())
      {
        var propertyType = item.Item1;
        var apiVersion = item.Item2;

        var resourceType = propertyType.Split("/").First();
        var subType = propertyType.Split("/").Last();

        if (resourceType == "Mock.Rp")
        {
          continue;
        }

        stream.Write(Encoding.UTF8.GetBytes($"|{propertyType}|[{apiVersion}](https://learn.microsoft.com/en-us/azure/templates/{resourceType}/{apiVersion}/{subType})|\n"));
      }
      stream.Write(Encoding.UTF8.GetBytes($"\n\n"));

      stream.Write(Encoding.UTF8.GetBytes($"## Variables\n\n"));
      var variables = root.variables.EnumerateObject().ToList();
      if (sort)
      {
        variables = variables.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in variables)
      {
        var name = item.Name;

        stream.Write(Encoding.UTF8.GetBytes($"- {name}\n"));
      }
      stream.Write(Encoding.UTF8.GetBytes($"\n"));

      stream.Write(Encoding.UTF8.GetBytes($"## Parameters\n\n"));
      var parameters = root.parameters.EnumerateObject().ToList();
      if (sort)
      {
        parameters = parameters.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in parameters)
      {
        var name = item.Name;
        var propertyType = item.Value.GetProperty("type").GetString();

        stream.Write(Encoding.UTF8.GetBytes($"### {name}\n\n- _Type:_ {propertyType}\n\n"));

        var property = item.Value.EnumerateObject().Where(p => p.Name == "metadata");
        if (property.Any())
        {
          var description = property.First().Value.GetProperty("description").GetString();

          stream.Write(Encoding.UTF8.GetBytes($"> {description}\n\n"));
        }
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Resources\n\n"));
      var resources = root.resources.EnumerateObject().ToList();
      if (sort)
      {
        resources = resources.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in resources)
      {
        var name = item.Name;
        var propertyType = item.Value.GetProperty("type").GetString();
        var propertyName = item.Value.GetProperty("name").GetString();

        stream.Write(Encoding.UTF8.GetBytes($"### {name}\n\n- _Type:_ {propertyType}\n- _Name:_ {propertyName}\n\n"));

        var property = item.Value.EnumerateObject().Where(p => p.Name == "metadata");
        if (property.Any())
        {
          var description = property.First().Value.GetProperty("description").GetString();

          stream.Write(Encoding.UTF8.GetBytes($"> {description}\n\n"));
        }

        if (item.Value.EnumerateObject().Where(p => p.Name == "dependsOn").Select(e => e.Value).Any())
        {
          stream.Write(Encoding.UTF8.GetBytes($"#### Dependencies\n\n"));
          foreach (var dependsOn in item.Value.EnumerateObject().Where(p => p.Name == "dependsOn").Select(e => e.Value))
          {
            dependsOn.EnumerateArray().ToList().ForEach(dependency =>
            {
              var dependencyContent = dependency.GetString() ?? "";
              var hashLink = dependencyContent.Replace(".", "").Replace("[", "").Replace("]", "").Replace(" ", "-").ToLower();
              stream.Write(Encoding.UTF8.GetBytes($"- [{dependency}](#{hashLink})\n"));
            });
          }
          stream.Write(Encoding.UTF8.GetBytes($"\n"));
        }
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Outputs\n\n"));
      var outputs = root.outputs.EnumerateObject().ToList();
      if (sort)
      {
        outputs = outputs.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in outputs)
      {
        var name = item.Name;
        var propertyType = item.Value.GetProperty("type").GetString();

        stream.Write(Encoding.UTF8.GetBytes($"### {name}\n\n- _Type:_ {propertyType}\n\n"));

        var property = item.Value.EnumerateObject().Where(p => p.Name == "metadata");
        if (property.Any())
        {
          var description = property.First().Value.GetProperty("description").GetString();
          stream.Write(Encoding.UTF8.GetBytes($"> {description}\n\n"));
        }
      }

      stream.Close();
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
