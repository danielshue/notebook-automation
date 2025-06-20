# Notebook Automation

A comprehensive toolkit that transforms educational content into intelligent, searchable knowledge bases. It processes PDFs, extracts annotations, generates AI-powered summaries from videos (with transcripts) and questions/answers, creates hierarchical course structures, and seamlessly integrates with popular tools like Obsidian and Anki for enhanced learning workflows.

## 💡 The Story Behind this Project

Like many students and lifelong learners, I found myself manually collecting course content from various online platforms—downloading PDFs, saving lecture notes, organizing video files, and trying to keep track of assignments across multiple courses. This tedious process consumed hours that could have been spent actually learning.

After spending countless evenings manually organizing course materials, I discovered the brilliant [coursera-dl](https://github.com/coursera-dl/coursera-dl) and [Coursera-Downloader](https://github.com/touhid314/Coursera-Downloader) projects. These tools opened my eyes to the power of automation for educational content management. The coursera-dl project, with its ability to batch download lecture resources and organize them with meaningful names, and the Coursera-Downloader's intuitive GUI for downloading entire courses, showed me what was possible when automation meets education.

Inspired by these projects but needing broader functionality beyond just downloading, I set out to create a comprehensive toolkit that could not only organize content but also analyze, tag, and enhance it with AI-powered insights. The result is Notebook Automation—a project born from the frustration of manual organization and the inspiration of seeing what thoughtful automation could achieve in the educational space.

[![Build Status](https://github.com/danielshue/notebook-automation/actions/workflows/ci-windows.yml/badge.svg)](https://github.com/danielshue/notebook-automation/actions)
[![Latest Release](https://img.shields.io/github/v/release/danielshue/notebook-automation?label=Download&color=brightgreen)](https://github.com/danielshue/notebook-automation/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

## ✨ Key Features

- **📊 Intelligent Processing** - AI-powered content analysis and summarization
- **🗂️ Obsidian Integration** - Hierarchical course structures with rich metadata
- **📈 Progress Tracking** - Real-time processing status and logging of course content
- **❓ Question Generation** - AI-powered Q&A creation for study materials
- **📚 Anki Integration** - Export flashcards for spaced repetition learning
- **🎥 Video Transcript Processing** - Generate summaries from video content
- **📄 PDF Annotation Extraction** - Preserve highlights and comments from documents
- **📝 Vocabulary Management** - Extract and organize key terms and definitions
- **📁 Batch Operations** - Process multiple content for note efficiently
- **🔧 Extensible Architecture** - Plugin system for custom processors

## 📸 Screenshots & Features

### AI-Generated Page Summaries

Each processed document receives an intelligent summary that captures key points, main themes, and actionable insights. These summaries help you quickly review and recall important content.
![Obsidian Page Summary View](docs/images/ObsidianPageSummaryView.png)

### AI-Powered Question Generation

The system automatically generates intelligent questions and answers from your course content, perfect for creating study materials and spaced repetition systems. This feature leverages AI to identify key concepts and create meaningful assessment questions.

![AI Question Generation](docs/images/AI-Question-Generation.png)

### Anki Integration for Spaced Repetition

These Questions and Answers can be used to seamlessly export the generated questions to Anki for optimized learning through spaced repetition. The tool creates properly formatted flashcards that integrate with your existing study workflow.

![Anki Review System](docs/images/AnkiReview.png)

### Obsidian Content Indexes and Class View

#### Navigation Through Indexes

Demonstrate here is the hierarchical navigation system within Obsidian. Using the Bases template system, users can easily explore their course content through structured indexes. These indexes provide a clear overview of programs, courses, modules, and lessons, enabling seamless navigation and quick access to specific sections of the educational material.

![Obsidian Content Indexes for easy navigation](docs/images/Obsidian-Hierarchical-Indexes.png)

#### Class-Level Page Tracking

Highlighting the class-level page view, which tracks individual notes and document statuses. This view allows users to monitor the progress of their course materials, ensuring that all notes and documents are properly organized and up-to-date. It provides a centralized location to manage and review class-specific content efficiently.

![Obsidian Class View Using Bases](docs/images/ObsidianClassViewUsingBases.png)

### Case Study Analysis Views

Detailed case study notes with structured analysis, key insights, and cross-references. The system automatically formats complex business cases into digestible, searchable content.

![Obsidian Notes Case Study View](docs/images/ObsidianNotesCaseStudyView.png)

### PDF Annotation Processing

Automatically extract and process annotations from PDF documents, preserving highlights, comments, and notes in your knowledge base while maintaining proper attribution and context.

![PDF Annotations Processing](docs/images/PDF-Annotations.png)

### Rich YAML Frontmatter

Comprehensive metadata extraction creates rich YAML frontmatter with course information, tags, relationships, and custom properties that enhance searchability and organization.

![Rich YAML Frontmatter](docs/images/RichYamlFrontmatter.png)

### Vocabulary and Definition Management

Automatically identify and extract key terms and definitions from course materials, creating a searchable vocabulary database with contextual usage examples that can also imported into Anki.

![Vocabulary Definitions](docs/images/VocabularyDefinitions.png)

## 📖 Documentation

| Section | Description |
|---------|-------------|
| [**Getting Started**](docs/getting-started/index.md) | Installation, setup, and first steps |
| [**User Guide**](docs/user-guide/index.md) | Comprehensive usage documentation |
| [**Configuration**](docs/configuration/index.md) | Settings and customization options |
| [**Tutorials**](docs/tutorials/index.md) | Step-by-step examples and workflows |
| [**API Reference**](docs/api/index.md) | Detailed API documentation |
| [**Developer Guide**](docs/developer-guide/index.md) | Building and contributing |
| [**Troubleshooting**](docs/troubleshooting/index.md) | Common issues and solutions |

## 🛠️ System Requirements

- **.NET 9.0 SDK** or later
- **Windows 10/11**, **Linux**, or **macOS**
- **PowerShell** (for build scripts)
- **8GB RAM** recommended for large notebook processing

## 🏗️ Project Structure

```
notebook-automation/
├── src/c-sharp/                 🎯 Core C# application
│   ├── NotebookAutomation.Core/ 📚 Main processing library
│   ├── NotebookAutomation.CLI/  💻 Command-line interface
│   └── NotebookAutomation.Tests/🧪 Unit and integration tests
├── docs/                        📖 Documentation site
├── config/                      ⚙️ Configuration templates
├── scripts/                     🔧 Build and utility scripts
└── templates/                   📄 Output templates
```

## 🎯 Use Cases

- **Academic Research** - Organize course notebooks and assignments
- **Data Science Projects** - Standardize analysis workflows
- **Educational Content** - Prepare teaching materials
- **Documentation** - Generate reports from exploratory analysis
- **Archive Management** - Organize and categorize notebook collections

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](docs/developer-guide/contributing.md) for details.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests and documentation
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.

## 🙋 Support

- **Issues**: [GitHub Issues](https://github.com/danielshue/notebook-automation/issues)
- **Discussions**: [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)
- **Documentation**: [Project Documentation](docs/index.md)

---

<div align="center">

**[📖 Read the Docs](docs/index.md)** • **[🚀 Quick Start](docs/getting-started/index.md)** • **[💡 Examples](docs/tutorials/index.md)**

</div>
