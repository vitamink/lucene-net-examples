using System;
using System.IO;
using System.Reflection;

namespace SpatialExamples
{
    public static class AssemblyExtensions
    {
        public static string GetDirectoryName(this Assembly assembly)
        {
            var codeBaseUri = new UriBuilder(assembly.CodeBase);
            var path = Uri.EscapeUriString(codeBaseUri.Path);
            var directoryName = Path.GetDirectoryName(path);

            return directoryName;
        }

        /// <summary>
        /// Resolves a path relative to the specified assembly.
        /// </summary>
        /// <param name="assembly">The specified assembly.</param>
        /// <param name="relativePath">The path to resolve.</param>
        /// <returns></returns>
        public static string GetPathRelativeToAssembly(this Assembly assembly, string relativePath)
        {
            var assemblyDirectory = assembly.GetDirectoryName();
            var path = Path.GetFullPath(Path.Combine(assemblyDirectory, relativePath));

            return path;
        }
    }
}