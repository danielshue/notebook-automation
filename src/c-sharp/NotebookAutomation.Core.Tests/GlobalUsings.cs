// <copyright file="GlobalUsings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/GlobalUsings.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
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

// Test-specific namespaces
global using NotebookAutomation.Core.Tests.Helpers;
global using NotebookAutomation.Core.Tools.MarkdownGeneration;
global using NotebookAutomation.Core.Tools.PdfProcessing;

// NotebookAutomation Core Tools (commonly used)
global using NotebookAutomation.Core.Tools.Shared;
global using NotebookAutomation.Core.Tools.TagManagement;
global using NotebookAutomation.Core.Tools.VideoProcessing;
global using NotebookAutomation.Core.Utils;
