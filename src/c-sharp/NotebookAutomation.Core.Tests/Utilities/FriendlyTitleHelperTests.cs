// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Tests for the FriendlyTitleHelper class.
/// </summary>
[TestClass]
public class FriendlyTitleHelperTests
{
    [TestMethod]
    public void GetFriendlyTitleFromFileName_RemovesLeadingNumbers()
    {
        // Arrange
        string fileName = "01_Introduction_to_Finance";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Introduction Finance", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_PreservesNumbersAfterWords()
    {
        // Arrange
        string fileName = "module-4-ROI-Analysis-part-2";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("4 ROI Analysis Part 2", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_HandlesMultipleLeadingNumbers()
    {
        // Arrange
        string fileName = "1_1_Introduction_Concept_Overview";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("Introduction Concept Overview", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_PreservesAcronyms()
    {
        // Arrange
        string fileName = "02_ROI_Analysis_for_MBA_students";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("ROI Analysis For MBA Students", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_HandlesRomanNumerals()
    {
        // Arrange
        string fileName = "Part_ii_Advanced_Topics";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Part II Advanced Topics", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_HandlesEmptyInput()
    {
        // Arrange
        string fileName = string.Empty;

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Title", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_HandlesDashesAndUnderscores()
    {
        // Arrange
        string fileName = "financial_planning-basics_101";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Financial Planning Basics 101", result);
    } // Real-world MBA-Resources filename test cases

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_OperationsManagementTitle()
    {
        // Arrange
        string fileName = "1.3 - Operations Management BADM 567 Live Session";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("1.3 Operations Management BADM 567 Live Session", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_CourseOrientationTitle()
    {
        // Arrange
        string fileName = "01_01_welcome-to-operations-management-organization-and-analysis";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("Welcome Operations Management Organization Analysis", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_ProfessorIntroduction()
    {
        // Arrange
        string fileName = "01_03_meet-professor-gopesh-anand";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Meet Professor Gopesh Anand", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_ProcessArrangements()
    {
        // Arrange
        string fileName = "02_01_process-arrangements-and-operations-strategy";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("Process Arrangements Operations Strategy", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_InventoryManagement()
    {
        // Arrange
        string fileName = "05_01_inventory-process-cash-cycle-and-inventory-metrics";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("Inventory Process Cash Cycle Inventory Metrics", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_QualityManagement()
    {
        // Arrange
        string fileName = "04_01_one-shot-inventory-decisions-newsvendor-model";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("One Shot Inventory Decisions Newsvendor Model", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_SupplyChainManagement()
    {
        // Arrange
        string fileName = "06_01_supplier-selection-development-and-monitoring";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert - Let's see what the actual output is
        Assert.AreEqual("Supplier Selection Development Monitoring", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_StatisticalProcessControl()
    {
        // Arrange
        string fileName = "04_01_x-bar-r-charts-for-measurement-data";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("X Bar R Charts For Measurement Data", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_TechFilenames()
    {
        // Arrange
        string fileName = "the-importance-of-market-research";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("The Importance Market Research", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_CostFrameworkExample()
    {
        // Arrange
        string fileName = "cost-framework-2-behavior-example";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Cost Framework 2 Behavior Example", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_MarketResearchParts()
    {
        // Arrange
        string fileName = "conducting-market-research-part-1";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);

        // Assert
        Assert.AreEqual("Conducting Market Research Part 1", result);
    }

    [TestMethod]
    public void GetFriendlyTitleFromFileName_RealWorld_ComplexHyphenatedTitle()
    {
        // Arrange
        string fileName = "04_01_process-arrangements-for-competitive-differentiatiaon";

        // Act
        string result = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);            // Assert
        Assert.AreEqual("Process Arrangements For Competitive Differentiatiaon", result);
    }
}
