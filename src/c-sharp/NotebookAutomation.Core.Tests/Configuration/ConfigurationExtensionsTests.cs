// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Configuration;


/// <summary>
/// Unit tests for ConfigurationExtensions static class.
/// Tests configuration extension methods for adding objects as configuration sources.
/// </summary>
[TestClass]
public class ConfigurationExtensionsTests
{
    /// <summary>
    /// Test class for simple configuration object testing.
    /// </summary>

    private class SimpleTestConfig
    {
        public string? StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
    }


    /// <summary>
    /// Test class for complex nested configuration object testing.
    /// </summary>

    private class ComplexTestConfig
    {
        public string? Name { get; set; }
        public SimpleTestConfig? NestedConfig { get; set; }
        public int Age { get; set; }
    }


    /// <summary>
    /// Test enum for enum property testing.
    /// </summary>

    private enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }


    /// <summary>
    /// Tests AddObject extension method with simple object.
    /// </summary>
    [TestMethod]
    public void AddObject_WithSimpleObject_AddsPropertiesToConfiguration()
    {
        // Arrange
        var testConfig = new SimpleTestConfig
        {
            StringProperty = "TestString",
            IntProperty = 42,
            BoolProperty = true,
            DateTimeProperty = new DateTime(2023, 1, 1),
            EnumProperty = TestEnum.Option2
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("TestString", configuration["StringProperty"]);
        Assert.AreEqual("42", configuration["IntProperty"]);
        Assert.AreEqual("True", configuration["BoolProperty"]);
        Assert.AreEqual("1/1/2023 12:00:00 AM", configuration["DateTimeProperty"]);
        Assert.AreEqual("Option2", configuration["EnumProperty"]);
    }


    /// <summary>
    /// Tests AddObject extension method with complex nested object.
    /// </summary>
    [TestMethod]
    public void AddObject_WithComplexObject_AddsNestedPropertiesToConfiguration()
    {
        // Arrange
        var testConfig = new ComplexTestConfig
        {
            Name = "TestName",
            Age = 25,
            NestedConfig = new SimpleTestConfig
            {
                StringProperty = "NestedString",
                IntProperty = 100,
                BoolProperty = false,
                DateTimeProperty = new DateTime(2023, 12, 31),
                EnumProperty = TestEnum.Option3
            }
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("TestName", configuration["Name"]);
        Assert.AreEqual("25", configuration["Age"]);
        Assert.AreEqual("NestedString", configuration["NestedConfig:StringProperty"]);
        Assert.AreEqual("100", configuration["NestedConfig:IntProperty"]);
        Assert.AreEqual("False", configuration["NestedConfig:BoolProperty"]);
        Assert.AreEqual("12/31/2023 12:00:00 AM", configuration["NestedConfig:DateTimeProperty"]);
        Assert.AreEqual("Option3", configuration["NestedConfig:EnumProperty"]);
    }


    /// <summary>
    /// Tests AddObject extension method with object containing null properties.
    /// </summary>
    [TestMethod]
    public void AddObject_WithNullProperties_SkipsNullValues()
    {
        // Arrange
        var testConfig = new ComplexTestConfig
        {
            Name = "TestName",
            Age = 30,
            NestedConfig = null
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("TestName", configuration["Name"]);
        Assert.AreEqual("30", configuration["Age"]);
        Assert.IsNull(configuration["NestedConfig:StringProperty"]);
    }


    /// <summary>
    /// Tests AddObject extension method throws exception when object is null.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void AddObject_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert
        builder.AddObject(null!);
    }


    /// <summary>
    /// Tests AddObject extension method with empty object.
    /// </summary>
    [TestMethod]
    public void AddObject_WithEmptyObject_CreatesEmptyConfiguration()
    {
        // Arrange
        var testConfig = new { };
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.IsNotNull(configuration);
        // Empty object should have no configuration keys
        var allKeys = configuration.AsEnumerable().ToList();
        Assert.AreEqual(0, allKeys.Count);
    }


    /// <summary>
    /// Tests AddObject extension method with anonymous object.
    /// </summary>
    [TestMethod]
    public void AddObject_WithAnonymousObject_AddsPropertiesToConfiguration()
    {
        // Arrange
        var testConfig = new
        {
            DatabaseConnection = "Server=localhost;Database=Test;",
            MaxRetries = 3,
            EnableLogging = true
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("Server=localhost;Database=Test;", configuration["DatabaseConnection"]);
        Assert.AreEqual("3", configuration["MaxRetries"]);
        Assert.AreEqual("True", configuration["EnableLogging"]);
    }


    /// <summary>
    /// Tests AddObject extension method with multiple objects (last one wins).
    /// </summary>
    [TestMethod]
    public void AddObject_WithMultipleObjects_LastObjectWins()
    {
        // Arrange
        var firstConfig = new { Setting = "FirstValue" };
        var secondConfig = new { Setting = "SecondValue" };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(firstConfig);
        builder.AddObject(secondConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("SecondValue", configuration["Setting"]);
    }


    /// <summary>
    /// Tests AddObject extension method maintains configuration builder fluent interface.
    /// </summary>
    [TestMethod]
    public void AddObject_ReturnsConfigurationBuilder_ForFluentInterface()
    {
        // Arrange
        var testConfig = new { TestSetting = "TestValue" };
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddObject(testConfig);

        // Assert
        Assert.AreSame(builder, result);
    }


    /// <summary>
    /// Tests AddObject with complex nested hierarchy.
    /// </summary>
    [TestMethod]
    public void AddObject_WithDeepNesting_CreatesCorrectKeys()
    {
        // Arrange
        var testConfig = new
        {
            Level1 = new
            {
                Level2 = new
                {
                    Level3 = new
                    {
                        DeepValue = "Found"
                    }
                }
            }
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("Found", configuration["Level1:Level2:Level3:DeepValue"]);
    }


    /// <summary>
    /// Tests AddObject with object containing array or collection properties (should be ignored).
    /// </summary>
    [TestMethod]
    public void AddObject_WithCollectionProperties_HandlesCorrectly()
    {
        // Arrange
        var testConfig = new
        {
            StringValue = "Test",
            IntValue = 42
        };

        var builder = new ConfigurationBuilder();

        // Act
        builder.AddObject(testConfig);
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("Test", configuration["StringValue"]);
        Assert.AreEqual("42", configuration["IntValue"]);
    }
}