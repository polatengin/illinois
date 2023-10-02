# Bicep Documentation Generator

This is a tool to generate documentation for given Bicep files/modules

## Installation

Run the following command to install the tool from nuget as a global tool:

```bash
dotnet tool install -g illinois
```

## Running the tool from source code

```bash
dotnet run --project src/illinois.csproj -- --output-format markdown --bicep-file ./sample/main.bicep --sort
```

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

```bash
az bicep build --file ./sample/main.bicep --stdout
```
