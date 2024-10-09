using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    public class aesdencrpyt
    {
        private static byte[] aes_key = null; // 16字节的密钥
        private static byte[] aes_iv = null;// 16字节的向量

        //构造函数
        public aesdencrpyt(string key,string iv)
        {
            aes_key = Get16Bytes(key);
            aes_iv = Get16Bytes(iv);
        }

        /// <summary>
        /// 将长度为32个字符的字符串转换为16字节数组
        /// </summary>
        /// <param name="original32char"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] Get16Bytes(string original32char)
        {
            if (original32char.Length!= 32)
            {
                throw new ArgumentException("必须传入长度为32个字符的字符串！");
            }
            byte[] bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                bytes[i] = Convert.ToByte(original32char.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        //从文件读取加密数据
        public static byte[] ReadFile(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            return data;
        }

        
        /// <summary>
        /// AES加密图片
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        public static void EncryptImage(string inputFile, string outputFile)
        {
            using (FileStream fsIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream fsOut = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = aes_key;
                aes.IV = aes_iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] array = new byte[4096];
                int bytesRead;

                while ((bytesRead = fsIn.Read(array, 0, array.Length)) != 0)
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(array, 0, bytesRead);
                    fsOut.Write(encrypted, 0, encrypted.Length);
                }
            }
        }

        /// <summary>
        /// AES解密图片
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        public static void DecryptImage(string inputFile, string outputFile)
        {
            using (FileStream fsIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream fsOut = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = aes_key;
                aes.IV = aes_iv;
                aes.Padding = PaddingMode.PKCS7; // 确保填充模式一致

                // 跳过输入文件的头部 6 个字节
                fsIn.Seek(6, SeekOrigin.Begin);

                // 读取并解密前 1024 字节
                byte[] aesBuffer = new byte[1024];
                int aesBytesRead = fsIn.Read(aesBuffer, 0, aesBuffer.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (MemoryStream ms = new MemoryStream(aesBuffer, 0, aesBytesRead))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    byte[] decryptedAesBuffer = new byte[aesBytesRead];
                    int decryptedBytesRead = cs.Read(decryptedAesBuffer, 0, decryptedAesBuffer.Length);
                    fsOut.Write(decryptedAesBuffer, 0, decryptedBytesRead);
                }

                // 读取剩余的数据并进行异或解密
                byte[] xorKey = new byte[] {  0x02 }; // 异或密钥
                byte[] xorBuffer = new byte[4096];
                int xorBytesRead;

                while ((xorBytesRead = fsIn.Read(xorBuffer, 0, xorBuffer.Length)) > 0)
                {
                    for (int i = 0; i < xorBytesRead; i++)
                    {
                        xorBuffer[i] ^= xorKey[i % xorKey.Length];
                    }
                    fsOut.Write(xorBuffer, 0, xorBytesRead);
                }
            }
        }


    }
}
