// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.TestDoubles;

/// <summary>
/// Helper class to create a Kernel with a mock ITextGenerationService for testing.
/// </summary>

internal static class MockKernelFactory
{ /// <summary>
  /// Creates a minimal kernel with a mock text generation service that returns a predefined response.
  /// </summary>
  /// <param name="response">The response text that the mock should return.</param>
  /// <returns>A kernel instance with the mock service.</returns>
    public static Kernel CreateKernelWithMockService(string response = "Mock AI response")
    {
        // Create our mock text generation service
        SimpleTextGenerationService mockService = new()
        {
            Response = response,
        };

        // Create a real kernel with our mock service
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ITextGenerationService>(mockService);

        return builder.Build();
    }

    /// <summary>
    /// Creates a kernel that handles chunking well in tests, returning predictable responses for function invocations.
    /// </summary>
    /// <param name="response">The response text that the mock should return.</param>
    /// <returns>A kernel instance with the mock service and function handling.</returns>
    public static Kernel CreateKernelForChunkingTests(string response = "Mock AI summary for chunks")
    {
        Kernel kernel = CreateKernelWithMockService(response);

        // Return a special kernel that can handle chunking invocations
        return kernel;
    }
}
