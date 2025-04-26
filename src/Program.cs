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
    var outFileOption = new Option<FileInfo>(aliases: new[] { "--outfile" }, description: "Output file to write the generated documentation");

    var rootCommand = new RootCommand("Auto generate documentation for the given bicep file");

    rootCommand.AddOption(serveOption);
    rootCommand.AddOption(sortOption);
    rootCommand.AddOption(outputFormatOption);
    rootCommand.AddOption(bicepFileOption);
    rootCommand.AddOption(outFileOption);

    rootCommand.SetHandler((serve, sort, format, bicepFile, outFile) =>
    {
      ValidateOptions(serve, sort, format, bicepFile);

      GenerateDocumentation(sort, format, bicepFile, outFile);

      if (serve)
      {
        Serve();
      }
    },
    serveOption, sortOption, outputFormatOption, bicepFileOption, outFileOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static void ValidateOptions(bool serve, bool sort, OutputFormat format, FileInfo bicepFile)
  {
    if (format == OutputFormat.None)
    {
      throw new ArgumentException("output format cannot be none");
    }

    if (bicepFile == null)
    {
      throw new ArgumentException("The bicep file option is required");
    }
  }

  private static void GenerateDocumentation(bool sort, OutputFormat format, FileInfo bicepFile, FileInfo outFile)
  {
    switch (format)
    {
      case OutputFormat.Markdown:
        GenerateMarkdownDocumentation(sort, bicepFile, outFile);
        break;
      default:
        throw new ArgumentException($"Unsupported format: {format}");
    }
  }

  private static void GenerateMarkdownDocumentation(bool sort, FileInfo bicepFile, FileInfo outFile)
  {
    string sanitizeName(string name) {
      return name.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace("(", "").Replace(")", "").Replace(" ", "-").Trim();
    }

    var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    var process = new Process();
    process.StartInfo.FileName = "az";
    process.StartInfo.Arguments = $"bicep build --file {bicepFile} --outfile {tempFile}";
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.Start();

    process.WaitForExit();

    if (process.ExitCode == 0)
    {
      var rawContent = File.ReadAllText(tempFile);
      var root = JsonSerializer.Deserialize<Root>(rawContent) ?? throw new ArgumentException($"Failed to deserialize the generated bicep file: {tempFile}");

      if (outFile == null)
      {
        outFile = new FileInfo(bicepFile.FullName.Replace(".bicep", ".md"));
      }

      var stream = File.Create(outFile.FullName);

      stream.Write(Encoding.UTF8.GetBytes($"# {bicepFile.Name}\n\n"));

      if (root.metadata.TryGetProperty("_EXPERIMENTAL_WARNING", out var experimentalWarning))
      {
        var warningMessage = experimentalWarning.GetString() ?? "";
        stream.Write(Encoding.UTF8.GetBytes($"> {warningMessage}\n\n"));
        stream.Write(Encoding.UTF8.GetBytes($"_Enabled experimental features:_\n\n"));
        root.metadata.TryGetProperty("_EXPERIMENTAL_FEATURES_ENABLED", out var experimentalFeaturesEnabled);
        if (experimentalFeaturesEnabled.ValueKind == JsonValueKind.Array)
        {
          foreach (var item in experimentalFeaturesEnabled.EnumerateArray())
          {
            stream.Write(Encoding.UTF8.GetBytes($"- {item}\n"));
          }
        }
        stream.Write(Encoding.UTF8.GetBytes($"\n"));
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Table of Contents\n\n"));

      stream.Write(Encoding.UTF8.GetBytes($"- [Diagram](#diagram)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Metadata](#metadata)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Resource Types](#resource-types)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [User Defined Functions](#user-defined-functions)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Variables](#variables)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Required Parameters](#required-parameters)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Optional Parameters](#optional-parameters)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Resources](#resources)\n"));
      stream.Write(Encoding.UTF8.GetBytes($"- [Outputs](#outputs)\n"));

      stream.Write(Encoding.UTF8.GetBytes($"\n"));

      #region Diagram
      stream.Write(Encoding.UTF8.GetBytes($"## Diagram\n\n"));
      stream.Write(Encoding.UTF8.GetBytes($"```mermaid\n"));
      stream.Write(Encoding.UTF8.GetBytes($"graph TD;\n"));
      foreach (var item in root.resources.EnumerateObject())
      {
        var name = item.Value.GetProperty("name").GetString() ?? "";

        if (name.Contains("[format"))
        {
          name = item.Name;
        }

        item.Value.EnumerateObject().Where(p => p.Name == "dependsOn").Select(e => e.Value).ToList().ForEach(dependency =>
        {
          dependency.EnumerateArray().ToList().ForEach(dep =>
          {
            stream.Write(Encoding.UTF8.GetBytes($"  {sanitizeName(dep.GetString() ?? "")} --> {sanitizeName(name)};\n"));
          });
        });
      }
      stream.Write(Encoding.UTF8.GetBytes($"```\n\n"));
      #endregion

      #region Metadata
      stream.Write(Encoding.UTF8.GetBytes($"## Metadata\n\n"));
      var metadata = root.metadata.EnumerateObject().ToList().Where(e => e.Name != "_EXPERIMENTAL_WARNING" && e.Name != "_EXPERIMENTAL_FEATURES_ENABLED").ToList();
      if (sort)
      {
        metadata = metadata.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in metadata)
      {
        stream.Write(Encoding.UTF8.GetBytes($"<details>\n<summary>{item.Name}</summary>\nDefault value: {item.Value}\n</details>\n\n"));
      }
      #endregion

      #region Resource Types
      var resourceTypes = new List<(string, string)>();
      foreach (var item in root.resources.EnumerateObject().Select(e => e.Value))
      {
        var propertyType = item.GetProperty("type").GetString() ?? "";
        var apiVersion = item.GetProperty("apiVersion").GetString() ?? "";

        resourceTypes.Add((propertyType, apiVersion));
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Resource Types\n\n"));
      stream.Write(Encoding.UTF8.GetBytes($"Resource Types used in the bicep file\n\n"));
      stream.Write(Encoding.UTF8.GetBytes($"| Resource Type | API Version |\n"));
      stream.Write(Encoding.UTF8.GetBytes($"| :-- | :-- |\n"));
      foreach (var item in resourceTypes.Distinct())
      {
        var propertyType = item.Item1;
        var apiVersion = item.Item2;

        var propertyParts = propertyType.Split("/");

        var resourceType = propertyParts[0];
        var subType = propertyParts.Last();

        if (resourceType == "Mock.Rp")
        {
          continue;
        }

        stream.Write(Encoding.UTF8.GetBytes($"| {propertyType} | [{apiVersion}](https://learn.microsoft.com/en-us/azure/templates/{resourceType}/{apiVersion}/{subType}) |\n"));
      }
      stream.Write(Encoding.UTF8.GetBytes($"\n"));
      #endregion

      #region User Defined Functions
      stream.Write(Encoding.UTF8.GetBytes($"## User Defined Functions\n\n"));

      if (!root.functions.Any())
      {
        stream.Write(Encoding.UTF8.GetBytes($"No user defined functions found\n\n"));
      }
      else
      {
        var functions = root.functions.FirstOrDefault()!;
        var members = functions["members"];
        var functionsList = members?.AsObject().AsEnumerable() ?? [];
        foreach (var item in functionsList)
        {
          // var parameters = string.Empty;
          // foreach (var parameter in item.Value.AsObject().Where(p => p.Key == "parameters").Select(e => e.Value))
          // {
          //   var name = parameter.AsObject().FirstOrDefault(p => p.Key == "name").Value ?? "";
          //   var type = parameter.AsObject().FirstOrDefault(p => p.Key == "type").Value ?? "";
          //   var description = parameter.AsObject().FirstOrDefault(p => p.Key == "description").Value ?? "";

          //   parameters += $"- {name} ({type}) - {description}\n";
          // }
          stream.Write(Encoding.UTF8.GetBytes($"- {item.Key}()\n\n"));
        }
      }
      #endregion
      stream.Write(Encoding.UTF8.GetBytes($"## Variables\n\n"));
      var variables = root.variables.EnumerateObject().ToList();
      if (sort)
      {
        variables = variables.OrderBy(p => p.Name).ToList();
      }
      foreach (var item in variables)
      {
        stream.Write(Encoding.UTF8.GetBytes($"<details>\n<summary>{item.Name}</summary>\nDefault value: {item.Value}\n</details>\n\n"));
      }

      stream.Write(Encoding.UTF8.GetBytes($"## Required Parameters\n\n"));
      var required_parameters = root.parameters.EnumerateObject().Where(e => e.Value.TryGetProperty("defaultValue", out _) == false).ToList();
      if (sort)
      {
        required_parameters = required_parameters.OrderBy(p => p.Name).ToList();
      }
      stream.Write(Encoding.UTF8.GetBytes($"| Parameter Name | Type | Description |\n"));
      stream.Write(Encoding.UTF8.GetBytes($"| :-- | :-- | :-- |\n"));
      foreach (var item in required_parameters)
      {
        var name = item.Name;
        var propertyType = item.Value.GetProperty("type").GetString();
        var property = item.Value.EnumerateObject().Where(p => p.Name == "metadata");
        var description = "";

        if (property.Any())
        {
          description = property.First().Value.GetProperty("description").GetString();
        }

        stream.Write(Encoding.UTF8.GetBytes($"| {name} | {propertyType} | {description} |"));
      }
      stream.Write(Encoding.UTF8.GetBytes($"\n"));

      stream.Write(Encoding.UTF8.GetBytes($"## Optional Parameters\n\n"));
      var optional_parameters = root.parameters.EnumerateObject().Where(e => e.Value.TryGetProperty("defaultValue", out _) == true).ToList();
      if (sort)
      {
        optional_parameters = optional_parameters.OrderBy(p => p.Name).ToList();
      }
      stream.Write(Encoding.UTF8.GetBytes($"| Parameter Name | Type | Default Value | Allowed Values | Description |\n"));
      stream.Write(Encoding.UTF8.GetBytes($"| :-- | :-- | :-- | :-- | :-- |\n"));
      foreach (var item in optional_parameters)
      {
        var name = item.Name;
        var propertyType = item.Value.GetProperty("type").GetString();
        var property = item.Value.EnumerateObject().Where(p => p.Name == "metadata");
        var description = "";

        if (property.Any())
        {
          description = property.First().Value.GetProperty("description").GetString();
        }

        stream.Write(Encoding.UTF8.GetBytes($"| {name} | {propertyType} | {description} |\n"));
      }
      stream.Write(Encoding.UTF8.GetBytes($"\n"));

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
      throw new ArgumentException($"Failed to generate documentation for file: {bicepFile}");
    }
  }

  private static void Serve()
  {
    throw new ArgumentException("Serve feature is not implemented yet");
  }
}
