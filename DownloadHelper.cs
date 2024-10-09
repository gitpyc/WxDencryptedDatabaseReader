using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace wxreader
{
    public class DownloadHelper
    {
        public static async Task Download(string url, string savePath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // 发送 GET 请求并获取响应
                    HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode(); // 确保请求成功

                    // 获取响应内容的长度
                    long? contentLength = response.Content.Headers.ContentLength;

                    // 创建文件流
                    using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        // 读取响应内容并写入文件
                        await response.Content.CopyToAsync(fs);
                    }

                    Console.WriteLine($"文件下载完成，保存路径: {savePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"下载文件时发生错误: {ex.Message}");
                }
            }
        }

        //多线程下载
        public static async Task MultiDownload(string[] urls, string[] savePaths)
        {
            int count = urls.Length;
            Task[] tasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = Download(urls[i], savePaths[i]);
            }
            await Task.WhenAll(tasks); //等待所有任务完成
        }
    }
}
