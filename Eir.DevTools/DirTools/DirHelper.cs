﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Eir.DevTools
{
    public static class DirHelper
    {
        /// <summary>
        /// Create a clean directory.
        /// We dont delete and recreate existing dir's because with windows this appears to sometimes fail.
        /// The CreateDir works, but the dir is not created. Appears to be a timing issue.
        /// So, instead, delete all elements of existing directories.
        /// </summary>
        /// <param name="path"></param>
        public static void CreateCleanDir(String path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                return;
            }
            CleanDir(path);
        }

        public static void CreateDirPath(String path)
        {
            if (String.IsNullOrEmpty(path))
                throw new Exception($"Invalid path {path}");
            if (Directory.Exists(path) == true)
                return;
            String? dirName = Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(dirName))
                throw new Exception($"Invalid dir name {dirName}");
            CreateDirPath(dirName);
            Directory.CreateDirectory(path);
        }

        public static void CleanDir(String path)
        {
            foreach (String file in Directory.GetFiles(path))
                File.Delete(file);

            foreach (String subDir in Directory.GetDirectories(path))
                Directory.Delete(subDir, true);
        }

        public static String FindParentDir(String dirName)
        {
            String servicePath = Path.GetFullPath(".");
            while (true)
            {
                servicePath = Path.GetFullPath(servicePath);
                String serviceDir = Path.Combine(servicePath, dirName);
                if (Directory.Exists(serviceDir))
                    return serviceDir;
                String newPath = Path.Combine(servicePath, "..");
                newPath = Path.GetFullPath(newPath);
                if (String.Compare(newPath, servicePath, StringComparison.InvariantCulture) == 0)
                    throw new Exception($"Parent directory {dirName} not found");
                servicePath = newPath;
            }
        }
    }
}
