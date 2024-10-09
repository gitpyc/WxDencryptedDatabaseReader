using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using wxreader;

public class Golangdecoder
{
    private static readonly Dictionary<string, byte[]> imagePrefixBtsMap = new Dictionary<string, byte[]>();
    

    public static void Fuckit()
    {
        InitImagePrefixBtsMap();
        string filePath = null, outputDir = null;
        GloableVars.imagesDir = new List<string>();
        GloableVars.processedDir = new List<string>();
        //打开文件选择对话框
        //OpenFileDialog openFileDialog = new OpenFileDialog();
        //openFileDialog.Filter = "WeChat Files (*.dat)|*.dat";
        //if (openFileDialog.ShowDialog() == DialogResult.OK)
        //{
        //    filePath = openFileDialog.FileName;
        //    outputDir = Path.GetDirectoryName(filePath) + "/Decode";
        //    if (!Directory.Exists(outputDir))
        //    {
        //        Directory.CreateDirectory(outputDir);
        //    }
        //    HandlerOne(new FileInfo(filePath), Path.GetDirectoryName(filePath), outputDir);
        //}


        //打开文件夹选择对话框
        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
        folderBrowserDialog.Description = "选择要处理的文件夹";
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            filePath = folderBrowserDialog.SelectedPath;
            //outputDir = filePath + "/Decode";
            //检测当前文件夹下是否有dat文件
            var testfile = Directory.GetFiles(filePath, "*.dat", SearchOption.AllDirectories);
            //更新进度条并避免阻塞主线程
            var startTime = DateTime.Now;
            //if (testfile.Length > 0)
            if (GloableVars.imagesDir.Count ==0)
            {
                foreach (var item in testfile)
                {
                    if (Directory.GetDirectories(Path.GetDirectoryName(item)).Length == 0)
                    {
                        outputDir = Path.GetDirectoryName(item) + "\\Decode";
                        if (!Directory.Exists(outputDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(outputDir);
                                string fipath = Path.GetDirectoryName(item);
                                if (!GloableVars.processedDir.Contains(outputDir))
                                {
                                    ProcessDirectory(fipath, fipath, new DirectoryInfo(fipath), outputDir, DateTime.Now);
                                    GloableVars.processedDir.Add(outputDir);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("创建文件夹失败：" + ex.Message);
                                throw ex;
                            }
                            GloableVars.imagesDir.Add(outputDir);
                        }
                    }
                } 
            }
            
            //HandlerOne(new FileInfo(filePath), Path.GetDirectoryName(filePath), outputDir);
        }

        //string dir = ".";
        //string outputDir = "./Decode";

        //foreach (var arg in args)
        //{
        //    if (arg.StartsWith("-in="))
        //        dir = arg.Substring(4);
        //    else if (arg.StartsWith("-out="))
        //        outputDir = arg.Substring(5);
        //}

        //Console.WriteLine($@"
        //    https://github.com/liuggchen/wechatDatDecode.git

        //处理目录：{dir}
        //输出目录：{outputDir}
        //");


        //遍历目录，查找dat文件
        //var allfiles = Directory.GetFiles(filePath, "*.dat", SearchOption.AllDirectories);
        //foreach (var file in allfiles)
        //{
        //    Console.WriteLine("find file: " + file);
        //    ////检测文件是否是在最后一层文件夹
        //    //if(Directory.GetDirectories(Path.GetDirectoryName(file)).Length > 0)
        //    //{

        //    //}
        //    string fipath = Path.GetDirectoryName(file);
        //    if (true)
        //    {
        //        ProcessDirectory(fipath, fipath, new DirectoryInfo(fipath), fipath + "\\Decode", startTime); 
        //    }
        //}

        //    FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);

        //if (!Directory.Exists(outputDir))
        //{
        //    Directory.CreateDirectory(outputDir);
        //}

        //var taskChan = new Queue<FileInfo>(files);
        //var tasks = new List<Task>();

        //for (int i = 0; i < 10; i++)
        //{
        //    tasks.Add(Task.Run(() =>
        //    {
        //        while (taskChan.Count > 0)
        //        {
        //            FileInfo info;
        //            lock (taskChan)
        //            {
        //                if (taskChan.Count == 0) break;
        //                info = taskChan.Dequeue();
        //            }
        //            HandlerOne(info, filePath, outputDir);
        //        }
        //    }));
        //}

        //Task.WaitAll(tasks.ToArray());
        //var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
        //Console.WriteLine($"\nfinished time= {elapsedTime} s\n");
    }

    private static void ProcessDirectory(string targetDirectory, string wenjianlujin, DirectoryInfo directoryInfo, string outputDir, DateTime startTime)
    {
        // 获取当前目录下的所有文件
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string fileName in fileEntries)
        {
            ProcessFile(Path.GetDirectoryName(fileName), directoryInfo, outputDir, startTime);
        }

        // 递归处理子目录
        //string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        //foreach (string subdirectory in subdirectoryEntries)
        //{
        //    ProcessDirectory(subdirectory, wenjianlujin, directoryInfo, outputDir, startTime);
        //}
    }

    private static void ProcessFile(string filePath, DirectoryInfo directoryInfo, string outputDir, DateTime startTime)
    {
        FileInfo[] files = directoryInfo.GetFiles("*.dat", SearchOption.TopDirectoryOnly);

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var taskChan = new Queue<FileInfo>(files);
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (taskChan.Count > 0)
                {
                    FileInfo info;
                    lock (taskChan)
                    {
                        if (taskChan.Count == 0) break;
                        info = taskChan.Dequeue();
                    }
                    HandlerOne(info, filePath, outputDir);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
        Console.WriteLine($"\nfinished time= {elapsedTime} s\n");
    }

    private static void HandlerOne(FileInfo info, string dir, string outputDir)
    {
        if (info.Attributes.HasFlag(FileAttributes.Directory) || info.Extension != ".dat")
        {
            return;
        }
        Console.WriteLine("find file: " + info.Name);
        string fPath = Path.Combine(dir, info.Name);
        using FileStream sourceFile = new FileStream(fPath, FileMode.Open, FileAccess.Read);

        byte[] preTenBts = new byte[10];
        sourceFile.Read(preTenBts, 0, preTenBts.Length);
        byte decodeByte;
        string ext;
        var er = FindDecodeByte(preTenBts, out decodeByte, out ext);
        if (er != null)
        {
            MessageBox.Show(er.Message);
            return;
        }

        using FileStream distFile = new FileStream(Path.Combine(outputDir, Path.GetFileNameWithoutExtension(info.Name) + ext), FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new BinaryWriter(distFile);
        sourceFile.Seek(0, SeekOrigin.Begin);
        byte[] rBts = new byte[1024];
        int n;
        while ((n = sourceFile.Read(rBts, 0, rBts.Length)) > 0)
        {
            for (int i = 0; i < n; i++)
            {
                writer.Write((byte)(rBts[i] ^ decodeByte));
            }
        }

        Console.WriteLine("output file： " + distFile.Name);
    }

    static void InitImagePrefixBtsMap()
    {
        //JPEG (jpg)，文件头：FFD8FF
        //PNG (png)，文件头：89504E47
        //GIF (gif)，文件头：47494638
        //TIFF (tif)，文件头：49492A00
        //Windows Bitmap (bmp)，文件头：424D
        const string Jpeg = "FFD8FF";
        const string Png = "89504E47";
        const string Gif = "47494638";
        const string Tif = "49492A00";
        const string Bmp = "424D";

        imagePrefixBtsMap[".jpeg"] = StringToByteArray(Jpeg);
        imagePrefixBtsMap[".png"] = StringToByteArray(Png);
        imagePrefixBtsMap[".gif"] = StringToByteArray(Gif);
        imagePrefixBtsMap[".tif"] = StringToByteArray(Tif);
        imagePrefixBtsMap[".bmp"] = StringToByteArray(Bmp);
    }

    private static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }

    private static Exception FindDecodeByte(byte[] bts, out byte decodeByte, out string ext)
    {
        foreach (var kvp in imagePrefixBtsMap)
        {
            var er = TestPrefix(kvp.Value, bts, out decodeByte);
            if (er == null)
            {
                ext = kvp.Key;
                return null;
            }
        }
        decodeByte = 0;
        ext = null;
        return new Exception("decode fail");
    }

    private static Exception TestPrefix(byte[] prefixBytes, byte[] bts, out byte decodeByte)
    {
        decodeByte = (byte)(prefixBytes[0] ^ bts[0]);
        for (int i = 0; i < prefixBytes.Length; i++)
        {
            if ((prefixBytes[i] ^ bts[i]) != decodeByte)
            {
                return new Exception("no");
            }
        }
        return null;
    }
}

