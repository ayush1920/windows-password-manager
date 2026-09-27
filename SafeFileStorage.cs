using System;
using System.IO;
using System.Text;

namespace PasswordGui
{
    /// <summary>
    /// Enterprise-grade atomic file I/O and backup engine.
    /// Prevents data loss during sudden power outages, application crashes, or disk write interruptions
    /// by writing to temporary files first, flushing to physical media, maintaining rotating backups (.bak),
    /// and executing atomic file replacements.
    /// </summary>
    public static class SafeFileStorage
    {
        private static readonly object FileSyncLock = new object();

        /// <summary>
        /// Atomically writes content to a target file with full disk flushing and backup generation.
        /// </summary>
        public static void WriteAllTextAtomic(string targetPath, string content, bool makeBackup = true)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            lock (FileSyncLock)
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string tempPath = targetPath + ".tmp." + Guid.NewGuid().ToString("N");
                string backupPath = targetPath + ".bak";

                try
                {
                    // 1. Write completely to unique temporary file with disk buffer flush
                    using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.Write(content);
                        sw.Flush();
                        fs.Flush(true); // Force physical disk sync
                    }

                    // 2. Manage rotating backup if target file already exists
                    if (File.Exists(targetPath) && makeBackup)
                    {
                        try
                        {
                            File.Copy(targetPath, backupPath, true);
                        }
                        catch { }
                    }

                    // 3. Atomic replacement
                    if (File.Exists(targetPath))
                    {
                        try
                        {
                            File.Replace(tempPath, targetPath, backupPath, true);
                        }
                        catch
                        {
                            // Fallback if filesystem does not support File.Replace
                            string oldPath = targetPath + ".old." + Guid.NewGuid().ToString("N");
                            File.Move(targetPath, oldPath);
                            File.Move(tempPath, targetPath);
                            try { File.Delete(oldPath); } catch { }
                        }
                    }
                    else
                    {
                        File.Move(tempPath, targetPath);
                    }
                }
                finally
                {
                    // Clean up temporary file if lingering after exception
                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Safely reads all text from a target file, falling back to backup if original is missing.
        /// </summary>
        public static string ReadAllTextSafe(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath))
                return string.Empty;

            lock (FileSyncLock)
            {
                if (File.Exists(targetPath))
                {
                    return File.ReadAllText(targetPath, Encoding.UTF8);
                }

                string backupPath = targetPath + ".bak";
                if (File.Exists(backupPath))
                {
                    return File.ReadAllText(backupPath, Encoding.UTF8);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Checks whether a valid backup file exists for the given file path.
        /// </summary>
        public static bool BackupExists(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath)) return false;
            return File.Exists(targetPath + ".bak");
        }

        /// <summary>
        /// Restores a target file from its .bak backup.
        /// </summary>
        public static bool RestoreFromBackup(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath)) return false;

            lock (FileSyncLock)
            {
                string backupPath = targetPath + ".bak";
                if (!File.Exists(backupPath)) return false;

                try
                {
                    File.Copy(backupPath, targetPath, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
