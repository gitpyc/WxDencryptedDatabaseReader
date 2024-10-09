using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    internal class FolderFileSearcher
    {

        /// <summary>
        /// 搜索指定文件夹内的所有文件，并返回文件列表
        /// </summary>
        /// <param name="folderPath">要搜索的文件夹路径</param>
        /// <returns>文件列表</returns>
        public List<GloableVars.fileinfo> SearchFiles(string folderPath)
        {
            List<GloableVars.fileinfo> fileList = new List<GloableVars.fileinfo>();
            

            try
            {
                // 检查文件夹是否存在
                if (Directory.Exists(folderPath))
                {
                    // 获取文件夹内的所有文件,并存储文件路径
                    //string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                    //fileList.AddRange(files);

                    // 获取文件夹内的所有文件,并存储文件路径
                    foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                    {
                       if (!file.Contains("bak") &&!file.Contains("-wal") &&!file.Contains("-shm"))
                        {
                            GloableVars.fileinfo fileinfo = new GloableVars.fileinfo();
                            fileinfo.filename = Path.GetFileName(file);
                            fileinfo.filepath = file;
                            fileList.Add(fileinfo);
                        }
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"指定的文件夹路径不存在: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索文件时发生错误: {ex.Message}");
            }

            return fileList;
        }

        //检测列表中是否存在指定的文件，并返回存在的文件列表
        public List<string> CheckFiles(List<string> fileList, string fileName)
        {
            List<string> existFileList = new List<string>();

            foreach (string file in fileList)
            {
                if (file.Contains(fileName))
                {
                    existFileList.Add(file);
                }
            }

            return existFileList;
        }

        /// <summary>
        /// 检测指定文件是否被占用
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // 文件未被占用，可以正常打开
                    fileStream.Close();
                }
            }
            catch (IOException)
            {
                // 文件被占用，无法打开
                return true;
            }
            return false;
        }

        /// <summary>
        /// 查找指定文件的进程
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Process FindProcess(string filePath)
        {
            var processList = Process.GetProcesses();
            foreach (var process in processList)
            {
                try
                {
                    // 尝试获取进程的主模块路径，如果失败则捕获异常
                    string mainModulePath = process.MainModule.FileName;
                    if (mainModulePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return process;
                    }
                }
                catch
                {
                    // 无法访问进程的主模块路径
                    MessageBox.Show("无法访问进程的主模块路径");
                }
            }
            return null;
        }

        /// <summary>
        /// 释放sqlite文件
        /// </summary>
        /// <param name="sqliteFilePath"></param>
        public static void ReleaseSqliteFile(string sqliteFilePath)
        {
            var processList = Process.GetProcessesByName("sqlite");
            foreach (var process in processList)
            {
                try
                {
                    // 尝试关闭占用文件的进程
                    if (process.MainModule != null && process.MainModule.FileName.Contains("sqlite3"))
                    {
                        process.Kill();
                        process.WaitForExit(); // 等待进程退出
                    }
                }
                catch
                {
                    // 如果无法关闭进程，可以选择忽略或记录日志
                    MessageBox.Show("无法关闭占用文件的进程");
                }
            }
        }

        //给定一个文件列表，检测该类中是否踩在列表中的文件，并返回存在的文件列表
        public List<string> CheckFileList(List<string> fileList, List<string> checkFileList)
        {
            List<string> existFileList = new List<string>();

            foreach (string file in fileList)
            {
                foreach (string checkFile in checkFileList)
                {
                    if (file.Contains(checkFile))
                    {
                        existFileList.Add(file);
                    }
                }
            }

            return existFileList;
        }
        /// <summary>
        /// 给定一个文件列表，检测该类中是否踩在列表中的文件，并返回存在的文件列表
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="fileNameList"></param>
        /// <returns></returns>
        public List<GloableVars.fileinfo> CheckFileInfoFromFilnameList(List<GloableVars.fileinfo> sourceList, List<string> fileNameList)
        {
            List<GloableVars.fileinfo> existFileList = new List<GloableVars.fileinfo>();

            foreach (GloableVars.fileinfo file in sourceList)
            {
                foreach (string fileName in fileNameList)
                {
                    if (file.filename.Contains(fileName) && Path.GetExtension(file.filename).ToLower() == ".db")//只检测sqlite文件
                    {
                        existFileList.Add(file);
                    }
                }
            }

            return existFileList;
        }

        internal void SearchFolder(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}
