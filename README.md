# Bicep Documentation Generator

This is a tool to generate documentation for given Bicep files/modules

## Installation

Run the following command to install the tool from nuget as a global tool:

```bash
dotnet tool install --global illinois
```

> If the `illinois` tool is already installed, you can update it with the following command:
>
> ```bash
> dotnet tool update --global illinois
> ```

## _Running the tool from source code_

```bash
dotnet run --project src/illinois.csproj -- --output-format markdown --bicep-file ./sample/main.bicep --sort
```

> _Output of the command can be found in [https://gist.github.com/polatengin/df3583b4f6b82432d5e926874e43bf89](https://gist.github.com/polatengin/df3583b4f6b82432d5e926874e43bf89)_

## Usage

```text
Usage:
  illinois [options]

Description:
  Auto generate documentation for the given bicep file

Options:
  --serve                          Serve the generated documentation on localhost
  --sort                           Sort members alphabetically
  --output-format <Markdown|None>  The format to use for the output, valid options are (None | Markdown)
  --bicep-file <bicep-file>        Bicep file to generate documentation for
  --outfile <outfile>              Output file to write the generated documentation
  --version                        Show version information
  -?, -h, --help                   Show help and usage information
```
