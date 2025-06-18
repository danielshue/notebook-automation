// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Disable parallel execution to prevent file access conflicts in tests that create temporary files
// Workers = 1 means no parallelization (single worker thread)
[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.MethodLevel)]
