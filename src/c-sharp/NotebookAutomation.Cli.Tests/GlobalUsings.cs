// Licensed under the MIT License. See LICENSE file in the project root for full license information.
global using System;
global using System.CommandLine;
global using System.CommandLine.Parsing;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// Microsoft Testing Framework
global using Microsoft.VisualStudio.TestTools.UnitTesting;

// Mocking Framework
global using Moq;

// NotebookAutomation CLI namespaces
global using NotebookAutomation.Cli.Commands;

// NotebookAutomation CLI Tools (commonly used)
global using NotebookAutomation.Cli.Utilities;

// NotebookAutomation Core namespaces (commonly used in CLI tests)
global using NotebookAutomation.Core.Configuration;
