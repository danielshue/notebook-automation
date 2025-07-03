using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NotebookAutomation.Cli.Utilities;

/*
Notebook Automation version string format
=========================================

Readable pattern
----------------
    <major>.<minor>.<patch>-<branch>.<YYDDD>.<build> (<commit>)

Break-down of each field
------------------------

+---------+-------------+-------------------------+-----------------------------------------------+
| Part    | Raw value   | Typical role            | Notes / example interpretation                |
+---------+-------------+-------------------------+-----------------------------------------------+
| Major   | 3           | Breaking-change release | Backwards-incompatible changes since 2.x.x    |
| Minor   | 9           | Feature release         | New functionality added in the 3.x series     |
| Patch   | 0           | Bug-fix level           | No patch-level updates yet for 3.9            |
| Branch  | 6           | Build stream / branch   | Internal branch or CI pipeline identifier     |
| YYDDD   | 21124       | Build date code         | YY = 21 → 2021; DDD = 124 → 4 May 2021         |
| Build   | 20          | Build iteration         | 20th build produced on that date / branch     |
| Commit  | db94f4cc    | Source-control hash     | Pinpoints the exact changeset                 |
+---------+-------------+-------------------------+-----------------------------------------------+

Example interpretation
----------------------
“3.9.0-6.21124.20 (db94f4cc)” →
Version **3.9.0**, built from branch **6** on **2021-05-04**, 20th build that day, commit **db94f4cc**.

This file contains
------------------
* **Version** record (with full XML docs) to store every piece
* Static **Parse** method that converts the raw string to the record
* Helper **BuildDateUtc** that resolves YYDDD to a real <c>DateTime</c>
* Simple demo **Program** that prints the parsed parts

NOTE: Wrapped in <c>NotebookAutomation.Cli.Utilities</c> so it won’t collide with <c>System.Version</c>.
*/


/// <summary>
/// Immutable representation of a Notebook Automation build string
/// of the form <c>&lt;major&gt;.&lt;minor&gt;.&lt;patch&gt;-&lt;branch&gt;.&lt;YYDDD&gt;.&lt;build&gt; (&lt;commit&gt;)</c>.
/// </summary>
/// <param name="Major">Semantic-version major (breaking-change indicator).</param>
/// <param name="Minor">Semantic-version minor (feature release indicator).</param>
/// <param name="Patch">Semantic-version patch (bug-fix level).</param>
/// <param name="Branch">Internal branch / CI pipeline identifier.</param>
/// <param name="DateCode">
/// Encoded build date in <c>YYDDD</c> (Julian day of year) format,
/// e.g. <c>21124</c> → 4 May 2021.
/// </param>
/// <param name="Build">Incrementing build counter for the given branch/date.</param>
/// <param name="Commit">Short source-control hash that produced this build.</param>
public record AppVersion
(
    int Major,
    int Minor,
    int Patch,
    int Branch,
    int DateCode,
    int Build,
    string Commit
)
{
    /// <summary>
    /// Gets the build date derived from <see cref="DateCode"/> in coordinated universal time (midnight).
    /// </summary>

    public DateTime BuildDateUtc => JulianToDate(DateCode);

    /// <summary>
    /// Parses a raw version string such as
    /// <c>3.9.0-6.21124.20 (db94f4cc)</c> or <c>1.0.0-0.25174.1 (unknown)+e594dd4a</c>
    /// into a strongly-typed <see cref="AppVersion"/> record.
    /// </summary>
    /// <param name="input">The raw version text to parse.</param>
    /// <returns>A populated <see cref="AppVersion"/> instance.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the string does not match the expected pattern.
    /// </exception>
    public static AppVersion Parse(string input)
    {
        // Handle .NET's automatic Git hash suffix (e.g., "+e594dd4ada99034df479c1318734d8eeb4051592")
        string cleanInput = input;
        string? gitSuffix = null;

        if (input.Contains('+'))
        {
            var plusIndex = input.IndexOf('+');
            gitSuffix = input[(plusIndex + 1)..];
            cleanInput = input[..plusIndex];
        }

        // 1) Commit hash in parentheses
        var commitMatch = Regex.Match(
            cleanInput, @"\((?<commit>[0-9a-f]{7,}|unknown)\)", RegexOptions.IgnoreCase);

        string commit;
        if (!commitMatch.Success)
        {
            throw new FormatException("Commit hash not found.");
        }
        else
        {
            commit = commitMatch.Groups["commit"].Value;

            // If commit is "unknown" but we have a Git suffix, use the first 8 chars of that
            if (commit == "unknown" && !string.IsNullOrEmpty(gitSuffix) && gitSuffix.Length >= 8)
            {
                commit = gitSuffix[..8];
            }
        }

        // 2) Strip "(hash)" and split the remaining string
        string main = cleanInput[..cleanInput.IndexOf('(')].Trim();   // e.g. "3.9.0-6.21124.20"
        var parts = main.Split('-', 2);
        if (parts.Length != 2)
            throw new FormatException("Missing '-' separator between SemVer and meta.");

        // -- SemVer: major.minor.patch
        string[] semVer = parts[0].Split('.');
        if (semVer.Length != 3)
            throw new FormatException("SemVer segment must have three dot-separated parts.");

        int major = int.Parse(semVer[0], CultureInfo.InvariantCulture);
        int minor = int.Parse(semVer[1], CultureInfo.InvariantCulture);
        int patch = int.Parse(semVer[2], CultureInfo.InvariantCulture);

        // -- Meta: branch.dateCode.build
        string[] meta = parts[1].Split('.');
        if (meta.Length != 3)
            throw new FormatException("Meta segment must have three dot-separated parts.");

        int branch = int.Parse(meta[0], CultureInfo.InvariantCulture);
        int dateCode = int.Parse(meta[1], CultureInfo.InvariantCulture);
        int build = int.Parse(meta[2], CultureInfo.InvariantCulture);

        return new AppVersion(major, minor, patch, branch, dateCode, build, commit);
    }

    /// <summary>
    /// Converts a <c>YYDDD</c> Julian date code into a <see cref="DateTime"/> (UTC, midnight).
    /// </summary>
    /// <param name="yyDdd">Two-digit year followed by three-digit day-of-year (e.g., 21124).</param>
    /// <returns>A <see cref="DateTime"/> corresponding to the encoded day.</returns>
    private static DateTime JulianToDate(int yyDdd)
    {
        int yy = yyDdd / 1000;      // first 2 digits = short year
        int ddd = yyDdd % 1000;      // last 3 digits  = day of year
        int year = 2000 + yy;         // assumes 2000-2099 range

        return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(ddd - 1);
    }

    /// <summary>
    /// Creates a Version instance from the current application assembly.
    /// </summary>
    /// <returns>A Version instance representing the current application version.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the assembly version string cannot be parsed.
    /// </exception>
    public static AppVersion FromCurrentAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        // Use the path to the main module (exe or dll) for FileVersionInfo
        string? mainModulePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(mainModulePath))
        {
            // Fallback: try to find the first .exe (Windows) or .dll (Unix) in the base directory
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                string? candidate = null;
                // On Windows, look for .exe first, then .dll. On Mac/Linux, only .dll is produced.
#if WINDOWS
                var exeFiles = Directory.GetFiles(baseDir, "*.exe");
                candidate = exeFiles.FirstOrDefault();
                if (candidate == null)
                {
                    var dllFiles = Directory.GetFiles(baseDir, "*.dll");
                    candidate = dllFiles.FirstOrDefault();
                }
#else
                // On Mac/Linux, only .dll is produced by .NET publish
                var dllFiles = Directory.GetFiles(baseDir, "*.dll");
                candidate = dllFiles.FirstOrDefault();
#endif
                mainModulePath = candidate ?? baseDir;
            }
            else
            {
                mainModulePath = string.Empty;
            }
        }
        var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(mainModulePath!);

        if (string.IsNullOrEmpty(versionInfo.FileVersion))
        {
            // Fallback to assembly version if file version is not available
            var assemblyVersion = assembly.GetName().Version;
            if (assemblyVersion != null)
            {
                return new AppVersion(
                    assemblyVersion.Major,
                    assemblyVersion.Minor,
                    assemblyVersion.Build >= 0 ? assemblyVersion.Build : 0,
                    0, // Branch - not available from assembly version
                    GetCurrentDateCode(),
                    assemblyVersion.Revision >= 0 ? assemblyVersion.Revision : 0,
                    "unknown"
                );
            }
        }

        // Try to parse the full version string if available
        string fullVersion = versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "1.0.0-0.25001.0 (unknown)";

        // If it matches our expected format, parse it
        if (fullVersion.Contains('-') && fullVersion.Contains('('))
        {
            try
            {
                return Parse(fullVersion);
            }
            catch (FormatException)
            {
                // Fallback to default version if parsing fails
            }
        }

        // Defensive: try to parse as dotted version, else fallback to 1.0.0.0
        var fileVersion = versionInfo.FileVersion ?? "1.0.0.0";
        var parts = fileVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int major = 1, minor = 0, patch = 0, build = 0;
        if (parts.Length >= 1 && int.TryParse(parts[0], out var m)) major = m;
        if (parts.Length >= 2 && int.TryParse(parts[1], out var n)) minor = n;
        if (parts.Length >= 3 && int.TryParse(parts[2], out var p)) patch = p;
        if (parts.Length >= 4 && int.TryParse(parts[3], out var b)) build = b;
        return new AppVersion(
            major,
            minor,
            patch,
            0, // Branch - not available
            GetCurrentDateCode(),
            build,
            "current"
        );
    }


    /// <summary>
    /// Gets the current date code in YYDDD format.
    /// </summary>
    /// <returns>Current date encoded as YYDDD.</returns>
    private static int GetCurrentDateCode()
    {
        var now = DateTime.Now;
        int yy = now.Year % 100;
        int ddd = now.DayOfYear;
        return yy * 1000 + ddd;
    }


    /// <summary>
    /// Converts the version to a display-friendly string format.
    /// </summary>
    /// <returns>A formatted version string for display purposes.</returns>
    public string ToDisplayString()
    {
        return $"{Major}.{Minor}.{Patch}-{Branch}.{DateCode}.{Build} ({Commit})";
    }


    /// <summary>
    /// Converts the version to a simple semantic version string (major.minor.patch).
    /// </summary>
    /// <returns>A semantic version string.</returns>
    public string ToSemanticVersionString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }


    /// <summary>
    /// Gets a summary of version information as a dictionary for compatibility with existing systems.
    /// </summary>
    /// <returns>A dictionary containing version information.</returns>
    public Dictionary<string, string> ToInfoDictionary()
    {
        return new Dictionary<string, string>
        {
            ["SemanticVersion"] = ToSemanticVersionString(),
            ["FullVersion"] = ToDisplayString(),
            ["Major"] = Major.ToString(CultureInfo.InvariantCulture),
            ["Minor"] = Minor.ToString(CultureInfo.InvariantCulture),
            ["Patch"] = Patch.ToString(CultureInfo.InvariantCulture),
            ["Branch"] = Branch.ToString(CultureInfo.InvariantCulture),
            ["DateCode"] = DateCode.ToString(CultureInfo.InvariantCulture),
            ["Build"] = Build.ToString(CultureInfo.InvariantCulture),
            ["Commit"] = Commit,
            ["BuildDate"] = BuildDateUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ["BuildDateUtc"] = BuildDateUtc.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)
        };
    }
}
