// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Global usings for the NotebookAutomation.Core.Tests project
// These are commonly used throughout the test codebase

// System namespaces
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

// Microsoft Testing Framework
global using Microsoft.VisualStudio.TestTools.UnitTesting;

// Mocking Framework
global using Moq;

// NotebookAutomation Core namespaces
global using NotebookAutomation.Core.Configuration;
global using NotebookAutomation.Core.Services;

// Test-specific namespaces - organized structure
global using NotebookAutomation.Core.Tests.Configuration;
global using NotebookAutomation.Core.Tests.Helpers;
global using NotebookAutomation.Core.Tests.Models;
global using NotebookAutomation.Core.Tests.Services;
global using NotebookAutomation.Core.Tests.Services.Text;
global using NotebookAutomation.Core.Tests.TestDoubles;
global using NotebookAutomation.Core.Tests.Tools.MarkdownGeneration;
global using NotebookAutomation.Core.Tests.Tools.PdfProcessing;
global using NotebookAutomation.Core.Tests.Tools.TagManagement;
global using NotebookAutomation.Core.Tests.Tools.VideoProcessing;
global using NotebookAutomation.Core.Tests.Utilities;

// NotebookAutomation Core Tools (commonly used)
global using NotebookAutomation.Core.Tools.MarkdownGeneration;
global using NotebookAutomation.Core.Tools.PdfProcessing;
global using NotebookAutomation.Core.Tools.Shared;
global using NotebookAutomation.Core.Tools.TagManagement;
global using NotebookAutomation.Core.Tools.VideoProcessing;
global using NotebookAutomation.Core.Utils;
