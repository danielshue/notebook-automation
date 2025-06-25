// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Configuration;


/// <summary>
/// Unit tests for the <c>ConfigurationExtensions</c> static class.
/// <para>
/// These tests verify the behavior of extension methods for adding objects as configuration sources to an <see cref="IConfigurationBuilder"/>.
/// Scenarios include handling of simple and complex objects, nulls, anonymous types, deep nesting, collections, and fluent interface support.
/// </para>
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
    /// Verifies that <c>AddObject</c> adds all simple properties of a plain object to the configuration.
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
        // Parse and compare DateTime instead of string comparison to avoid culture issues
        Assert.IsTrue(DateTime.TryParse(configuration["DateTimeProperty"], out var actualDateTime));
        Assert.AreEqual(new DateTime(2023, 1, 1), actualDateTime);
        Assert.AreEqual("Option2", configuration["EnumProperty"]);
    }


    /// <summary>
    /// Verifies that <c>AddObject</c> adds nested properties of a complex object to the configuration using colon-separated keys.
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
        // Parse and compare DateTime instead of string comparison to avoid culture issues
        Assert.IsTrue(DateTime.TryParse(configuration["NestedConfig:DateTimeProperty"], out var nestedDateTime));
        Assert.AreEqual(new DateTime(2023, 12, 31), nestedDateTime);
        Assert.AreEqual("Option3", configuration["NestedConfig:EnumProperty"]);
    }


    /// <summary>
    /// Verifies that <c>AddObject</c> skips null properties and does not add them to the configuration.
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
    /// Verifies that <c>AddObject</c> throws <see cref="ArgumentNullException"/> when the input object is null.
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
    /// Verifies that <c>AddObject</c> with an empty object results in an empty configuration (no keys).
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
    /// Verifies that <c>AddObject</c> works with anonymous objects and adds their properties to the configuration.
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
    /// Verifies that when <c>AddObject</c> is called multiple times, the last object's values overwrite previous ones for the same keys.
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
    /// Verifies that <c>AddObject</c> returns the same <see cref="IConfigurationBuilder"/> instance for fluent chaining.
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
    /// Verifies that <c>AddObject</c> correctly handles deep nested objects, producing colon-separated keys for all levels.
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
    /// Verifies that <c>AddObject</c> ignores array or collection properties and only adds scalar values to the configuration.
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
