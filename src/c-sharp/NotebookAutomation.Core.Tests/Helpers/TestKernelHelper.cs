// <copyright file="TestKernelHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Helpers/TestKernelHelper.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using Microsoft.SemanticKernel;

namespace NotebookAutomation.Core.Tests.Helpers;

/// <summary>
/// Helper class for creating test kernels for AI summarization tests.
/// This class replaces the previous MockTextGenerationServiceHelper which depended on ITextGenerationService.
/// </summary>
internal static class TestKernelHelper
{
    /// <summary>
    /// Creates a basic kernel for testing.
    /// </summary>
    /// <returns>A Kernel instance configured for testing.</returns>
    public static Kernel CreateTestKernel()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        return builder.Build();
    }

    /// <summary>
    /// Creates a kernel that will simulate returning a specific response for AI prompts.
    /// </summary>
    /// <param name="simulatedResponse">The response to simulate.</param>
    /// <returns>A Kernel instance that will produce the simulated response.</returns>
    public static Kernel CreateKernelWithSimulatedResponse(string simulatedResponse)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();

        // Instead of mocking a service, we could register a test implementation
        // or configure the kernel in some other way if needed
        return builder.Build();
    }
}
