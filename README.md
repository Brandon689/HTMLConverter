# HtmlToJsonParser 🔄

HtmlToJsonParser is a versatile C# library that converts HTML to JSON using various parsing modes and customizable options. It leverages AngleSharp for HTML parsing and provides flexible output formatting.

## ✨ Features

- Multiple parsing modes:
  - Generic: Converts all HTML nodes to JSON
  - Table: Converts HTML tables to structured JSON
  - JSON-LD: Extracts JSON-LD data from HTML
- Customizable options:
  - New line conversion in values
  - Attribute prefix customization
  - Text property name customization
  - Output indentation control
  - JSON unescaping
  - Inside word trimming
  - Multiple table conversion

## 🚀 Installation

To use HtmlToJsonParser in your project, you need to install the following NuGet packages:
- Install-Package AngleSharp
- Install-Package Newtonsoft.Json

## 🔍 Parsing Modes

### Generic Mode

Converts all HTML nodes to JSON objects and properties.

### Table Mode

Converts HTML tables into a structured JSON format. Each row becomes a JSON object with column headers as keys.

### JSON-LD Mode

Extracts and parses JSON-LD data from HTML documents.

## ⚙️ Options

- `ValueNewLineConversion`: Specifies how to handle new lines in text values.
- `AttributePrefix`: Sets the prefix for HTML attributes in the JSON output.
- `TextPropertyName`: Defines the property name for text nodes.
- `Indent`: Controls whether the output JSON is indented.
- `UnescapeJson`: Attempts to unescape the input if it appears to be HTML wrapped in a JSON string.
- `TrimInsideWords`: Trims multiple consecutive spaces inside words to a single space.
- `ConvertAllTables`: Controls whether all tables or just the first one should be converted in Table mode.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
