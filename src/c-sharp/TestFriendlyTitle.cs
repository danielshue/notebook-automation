using NotebookAutomation.Core.Utils;

var testCases = new[]
{
    "04_01_process-arrangements-for-competitive-differentiatiaon",
    "06_01_supplier-selection-development-and-monitoring",
    "01_01_welcome-to-operations-management-organization-and-analysis",
    "01_03_meet-professor-gopesh-anand"
};

foreach (var testCase in testCases)
{
    var result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(testCase);
    Console.WriteLine($"Input: '{testCase}'");
    Console.WriteLine($"Output: '{result}'");
    Console.WriteLine();
}
