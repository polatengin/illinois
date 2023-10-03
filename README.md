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

## Deploying to Nuget

Compile the project and run the following command from the root of the repository:

```bash
dotnet pack src/illinois.sln --configuration release
```

Run the following command to push the package to Nuget:

```bash
dotnet nuget push src/illinois/bin/release/illinois.0.1.0.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate --api-key <API_KEY>
```

### Clear package cache

```bash
dotnet nuget locals all --clear
```
