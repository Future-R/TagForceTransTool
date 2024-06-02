using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class 测试
{
    public static void JSON转换为Lj台词()
    {

        Console.WriteLine("请拖入当前文件路径");
        string 当前文件路径 = Console.ReadLine().Trim('"');


        string 短文件名 = Path.GetFileNameWithoutExtension(当前文件路径);
        bool 是bl = 短文件名.Contains("bl");
        bool 是gz = 短文件名.EndsWith(".gz");

        string 输出路径 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(当前文件路径));
        // 去掉.json后缀，恢复原后缀
        输出路径 = 输出路径.Substring(0, 输出路径.Length - 5);
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        // 相当于一个索引ID，标记每段文字开始的位置
        List<int> 指针组 = new List<int>();
        // 接下来先读取JSON，拿到条目List
        string JSON字典文本 = File.ReadAllText(当前文件路径);
        JArray JSON数组 = JArray.Parse(JSON字典文本);

        // 如果是gz，使用GZipStream
        using var 输出文件 = 是gz ? (Stream)new GZipStream(File.Create(输出路径), CompressionMode.Compress) : File.Create(输出路径);
        List<byte> 已写入字节 = new();
        int number = 1;
        List<byte> 文本偏移 = new() { 0x00, 0x00, 0x00, 0x00 };

        // 逐条写入，每条写完都加\0
        foreach (var jobj in JSON数组.ToObject<List<JObject>>())
        {
            // -1已隐藏，0未翻译，这俩将使用原文original，否则使用译文translation
            // PSP里换行是\x0A\x00，对应\n+空字符，因为unicode固定占用两个字节，所以\n后面会自动补
            string 当前条目 = (int)jobj["stage"].ToObject(typeof(int)) > 0
                ? jobj["translation"].ToString()
                : jobj["original"].ToString();
            byte[] 待写入字节 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(当前条目.Replace("\\n", "\n")));
            已写入字节.AddRange(待写入字节);
            // 条目结束，写入\0
            已写入字节.AddRange(new byte[] { 0x00, 0x00 });
            // 暂时不知道bl代表什么，总之先偏移！
            if (是bl)
            {
                number++;
                文本偏移.AddRange(BitConverter.GetBytes(待写入字节.Length));
            }
        }

        if (短文件名.Contains("Lj"))
        {
            List<byte> 文本索引 = new() { 0x00, 0x00, 0x00, 0x00 };
            int index = 0;
            int eof = 已写入字节.Count;
            byte[] buffer = new byte[2];
            byte[] intBytes;

            while (index < eof)
            {
                buffer[0] = 已写入字节[index];
                buffer[1] = 已写入字节[index + 1];
                index += 2;

                if (buffer.SequenceEqual(new byte[] { 0x00, 0x00 }))
                {
                    //// 根据系统架构获取正确的字节顺序
                    //if (BitConverter.IsLittleEndian)
                    //{
                    //    Array.Reverse(buffer);
                    //}
                    intBytes = BitConverter.GetBytes(index / 2);
                    文本索引.AddRange(intBytes);
                }
            }
            // 写入本体LJ文件
            输出文件.Write(已写入字节.ToArray(), 0, 已写入字节.Count);
            for (int i = 0; i < 已写入字节.Count; i++)
            {
                Console.WriteLine($"{i,3}-{已写入字节[i]}");
                Console.ReadLine();
            }
            // 写入索引IJ文件
            string 索引输出路径 = 输出路径.Replace("Lj", "Ij");
            using var 索引输出文件 = 是gz ? (Stream)new GZipStream(File.Create(索引输出路径), CompressionMode.Compress) : File.Create(索引输出路径);
            索引输出文件.Write(文本索引.ToArray(), 0, 文本索引.Count);
            for (int i = 0; i < 文本索引.Count; i++)
            {
                Console.WriteLine($"{i,3}-{文本索引[i]}");
            }
        }
        else
        {
            输出文件.Write(BitConverter.GetBytes(number), 0, 4);
            输出文件.Write(new byte[] { 0x0C, 0x00, 0x00, 0x00 }, 0, 4);
            输出文件.Write(BitConverter.GetBytes(number * 4 + 12), 0, 4);
            输出文件.Write(文本偏移.ToArray(), 0, 文本偏移.Count);
            输出文件.Write(已写入字节.ToArray(), 0, 已写入字节.Count);
        }
    }
}

