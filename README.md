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

```bash
az bicep build --file ./sample/main.bicep --stdout
```
