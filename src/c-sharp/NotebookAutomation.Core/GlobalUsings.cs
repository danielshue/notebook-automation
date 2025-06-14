// Licensed under the MIT License. See LICENSE file in the project root for full license information.
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
// External dependencies for build error fixes
global using Microsoft.Extensions.Primitives; // IChangeToken
global using Microsoft.Graph; // GraphServiceClient
global using Microsoft.Graph.Models;
// Do NOT globally alias Serilog.ILogger as ILogger. Use Microsoft.Extensions.Logging.ILogger everywhere.
global using Microsoft.Identity.Client; // AuthenticationResult, IPublicClientApplication
global using Microsoft.SemanticKernel; // Kernel
global using Microsoft.SemanticKernel.Connectors.OpenAI; // OpenAIPromptExecutionSettings

global using NotebookAutomation.Core.Configuration;
global using NotebookAutomation.Core.Models;
global using NotebookAutomation.Core.Services;
global using NotebookAutomation.Core.Tools.MarkdownGeneration;
global using NotebookAutomation.Core.Tools.PdfProcessing;
global using NotebookAutomation.Core.Tools.Shared;
global using NotebookAutomation.Core.Tools.TagManagement;
global using NotebookAutomation.Core.Tools.Vault;
global using NotebookAutomation.Core.Tools.VideoProcessing;
global using NotebookAutomation.Core.Utils;
// Serilog namespaces
global using Serilog;
global using Serilog.Events;
global using Serilog.Extensions.Logging;
// PDF processing - will be added when we determine the correct library
// global using PdfSharpCore.Pdf;
// global using PdfSharpCore.Pdf.IO;

// PDF processing with UglyToad.PdfPig
global using UglyToad.PdfPig;
global using UglyToad.PdfPig.Content;

global using YamlDotNet.Core; // Scalar, ScalarStyle
global using YamlDotNet.Core.Events; // Scalar events
global using YamlDotNet.Serialization; // ChainedObjectGraphVisitor, IObjectGraphVisitor, IEmitter, IPropertyDescriptor, IObjectDescriptor
global using YamlDotNet.Serialization.NamingConventions; // CamelCaseNamingConvention
global using YamlDotNet.Serialization.ObjectGraphVisitors;

global using ILogger = Microsoft.Extensions.Logging.ILogger;
global using LogLevel = Microsoft.Extensions.Logging.LogLevel;
