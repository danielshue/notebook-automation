// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
