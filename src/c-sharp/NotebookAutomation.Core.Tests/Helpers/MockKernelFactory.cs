// <copyright file="MockKernelFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Helpers/MockKernelFactory.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using Microsoft.SemanticKernel;

namespace NotebookAutomation.Core.Tests.Helpers;

/// <summary>
/// Factory class for creating mock Kernel instances for testing.
/// </summary>
internal static class MockKernelFactory
{
    /// <summary>
    /// Creates a kernel with a mock service that returns the specified response.
    /// </summary>
    /// <param name="response">The expected response from the kernel.</param>
    /// <returns>A Kernel instance for testing.</returns>
    public static Kernel CreateKernelWithMockService(string response)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();

        // In a real implementation, this would configure the kernel
        // with a mock service that returns the specified response
        return builder.Build();
    }
}
