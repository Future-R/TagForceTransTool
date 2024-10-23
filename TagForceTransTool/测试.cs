using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;


class 测试
{
    public static void 搜索文本()
    {
        Console.WriteLine("请拖入需要解包的目录并回车\n解包结果会生成在此程序目录下的Extraction文件夹\n不会对源文件造成损害，大可安心");
        string path = Console.ReadLine().Trim('"');  // 拖入文件的路径
        Console.WriteLine("开始解包……");
        工具类.解包ehp(path);
        Console.WriteLine("解包完毕，开始扫描所有bin/gz");

        var 文件集合 = 工具类.获取bin与gz文件();
        Console.WriteLine("扫描完毕，请输入待搜索的关键字：");
        var 关键字 = Console.ReadLine();

        foreach (var item in 文件集合)
        {
            var 搜索结果 = 工具类.返回搜索结果文件名(item, 关键字);
            if (!string.IsNullOrEmpty(搜索结果))
            {
                Console.WriteLine(搜索结果);
            }
        }
        Console.WriteLine("搜索完毕！");
    }
    public static void JSON转换为Lj台词()
    {

        Console.WriteLine("请拖入当前文件路径");
        string 当前文件路径 = Console.ReadLine().Trim('"');


        string 短文件名 = Path.GetFileNameWithoutExtension(当前文件路径);
        bool 是bl = 短文件名.Contains("bl");
        bool 是gz = 短文件名.EndsWith(".gz");
        bool 是LJ = 短文件名.Contains("LJ");

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
        var 输出文件 = 是gz ? (Stream)new GZipStream(File.Create(输出路径), CompressionMode.Compress) : File.Create(输出路径);
        List<byte> 已写入字节 = new List<byte>();
        int number = 0;
        if (!是LJ)
        {
            已写入字节.AddRange(new byte[] { 0xFF, 0xFE });
            number = 1;
        }
        List<byte> 文本偏移 = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        int 累计字节数 = 0;

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
                累计字节数 += 待写入字节.Length;
                文本偏移.AddRange(BitConverter.GetBytes(累计字节数));
            }
        }

        if (是LJ)
        {
            List<byte> 文本索引 = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
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
            // 写入索引IJ文件
            string 索引输出路径 = 输出路径.Replace("Lj", "Ij");
            var 索引输出文件 = 是gz ? (Stream)new GZipStream(File.Create(索引输出路径), CompressionMode.Compress) : File.Create(索引输出路径);
            索引输出文件.Write(文本索引.ToArray(), 0, 文本索引.Count);
        }
        else
        {
            输出文件.Write(BitConverter.GetBytes(number),0, BitConverter.GetBytes(number).Length);
            输出文件.Write(new byte[] { 0x0C, 0x00, 0x00, 0x00 }, 0, 4);
            输出文件.Write(BitConverter.GetBytes(number * 4 + 12), 0, BitConverter.GetBytes(number * 4 + 12).Length);
            输出文件.Write(文本偏移.ToArray(), 0, 文本偏移.Count);
            输出文件.Write(已写入字节.ToArray(), 0, 已写入字节.Count());
        }
    }

    public static void 读取二进制()
    {
        Console.WriteLine("请拖入二进制文件路径");
        string filePath = Console.ReadLine().Trim('"');

        // 使用FileStream打开文件
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // 创建一个足够大的buffer来读取文件
            byte[] buffer = new byte[fs.Length];

            // 读取文件到buffer中
            fs.Read(buffer, 0, buffer.Length);

            // 遍历buffer中的每一个字节
            for (int i = 0; i < buffer.Length; i++)
            {
                // 打印每一个字节的数值
                Console.WriteLine("Byte at position {0} is: {1}", i, buffer[i]);
            }
        }
    }

}

