// <copyright file="FileSizeFormatterTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Utils/FileSizeFormatterTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Contains unit tests for the <see cref="FileSizeFormatter"/> utility class.
/// </summary>
/// <remarks>
/// Verifies correct formatting of file sizes for various byte values and units (B, KB, MB, GB, TB).
/// Ensures precision and rounding rules are applied as specified in the formatter implementation.
/// </remarks>
[TestClass]
internal class FileSizeFormatterTests
{
    /// <summary>
    /// Tests formatting of a byte value less than 1 KB.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Bytes() => Assert.AreEqual("512.00 B", FileSizeFormatter.FormatFileSizeToString(512));

    /// <summary>
    /// Tests formatting of a value exactly 1 KB.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Kilobytes() => Assert.AreEqual("1.00 KB", FileSizeFormatter.FormatFileSizeToString(1024));

    /// <summary>
    /// Tests formatting of a value exactly 1 MB.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Megabytes() => Assert.AreEqual("1.00 MB", FileSizeFormatter.FormatFileSizeToString(1024 * 1024));

    /// <summary>
    /// Tests formatting of a value exactly 1 GB.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Gigabytes() => Assert.AreEqual("1.00 GB", FileSizeFormatter.FormatFileSizeToString(1024L * 1024 * 1024));

    /// <summary>
    /// Tests formatting of a value exactly 1 TB.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Terabytes() => Assert.AreEqual("1.00 TB", FileSizeFormatter.FormatFileSizeToString(1024L * 1024 * 1024 * 1024));

    /// <summary>
    /// Tests formatting precision and rounding for values near unit boundaries.
    /// </summary>
    [TestMethod]
    public void FormatFileSizeToString_Precision()
    {
        Assert.AreEqual("9.99 MB", FileSizeFormatter.FormatFileSizeToString(10485760 - 10486)); // Just under 10 MB
        Assert.AreEqual("10.0 MB", FileSizeFormatter.FormatFileSizeToString(10485760)); // Exactly 10 MB
        Assert.AreEqual("99.9 MB", FileSizeFormatter.FormatFileSizeToString(104857600 - 104858)); // Just under 100 MB
        Assert.AreEqual("100 MB", FileSizeFormatter.FormatFileSizeToString(104857600)); // Exactly 100 MB
    }
}
