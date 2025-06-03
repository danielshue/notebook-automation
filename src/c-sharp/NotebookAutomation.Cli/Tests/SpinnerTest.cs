using NotebookAutomation.Cli.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NotebookAutomation.Cli.Tests
{
    public class SpinnerTest
    {
        public static async Task RunTest()
        {
            try
            {
                Console.WriteLine("Testing Spectre.Console integration with AnsiConsoleHelper...");
                
                // Start the spinner
                AnsiConsoleHelper.StartSpinner("Initial spinner message");
                
                // Wait a moment to see the spinner
                await Task.Delay(2000);
                
                // Update the message
                AnsiConsoleHelper.UpdateSpinnerMessage("First message update - should see a delay");
                
                // Update again quickly - should see the pause mechanism in action
                await Task.Delay(1000);
                AnsiConsoleHelper.UpdateSpinnerMessage("Second update - should be delayed due to pause mechanism");
                
                // Wait for the pause to elapse
                await Task.Delay(6000);
                
                // Update again after pause elapsed
                AnsiConsoleHelper.UpdateSpinnerMessage("Third update - should happen immediately");
                
                // Final delay
                await Task.Delay(3000);
                
                // Stop the spinner
                AnsiConsoleHelper.StopSpinner();
                
                Console.WriteLine("Spinner test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during spinner test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
