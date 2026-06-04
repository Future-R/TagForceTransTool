using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

public class 工具类
{
    static string 根目录 = string.Empty;
    static string ehppack目录 = string.Empty;
    static string 解包目录 = string.Empty;
    static string JSON目录 = string.Empty;
    static string TXT目录 = string.Empty;
    static string BIN目录 = string.Empty;
    static string EHP目录 = string.Empty;

    static bool 是Lj文本文件(string 文件路径)
    {
        string 文件名 = Path.GetFileNameWithoutExtension(文件路径);
        return 文件名.IndexOf("Lj", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool 是偏移文本表(string 文件路径)
    {
        string 文件名 = Path.GetFileNameWithoutExtension(文件路径).ToLowerInvariant();
        return 文件名.Contains("strtbl")
            || 文件名.Contains("wordstbl")
            || 文件名 == "bl"
            || 文件名.StartsWith("bl_")
            || 文件名.EndsWith("_bl")
            || 文件名.Contains("_bl_");
    }

    static bool 是文本候选文件(string 文件路径)
    {
        string 文件名 = Path.GetFileNameWithoutExtension(文件路径);
        string 小写文件名 = 文件名.ToLowerInvariant();
        string 扩展名 = Path.GetExtension(文件路径).ToLowerInvariant();

        if (扩展名 != ".bin" && 扩展名 != ".gz")
        {
            return false;
        }

        if (小写文件名.Contains("voice"))
        {
            return false;
        }

        if (小写文件名.EndsWith("bl_e")
            || 小写文件名.EndsWith("bl_f")
            || 小写文件名.EndsWith("bl_g")
            || 小写文件名.EndsWith("bl_i")
            || 小写文件名.EndsWith("bl_s"))
        {
            return false;
        }

        return 是Lj文本文件(文件路径)
            || 小写文件名.Contains("strtbl")
            || 小写文件名.Contains("wordstbl")
            || 是偏移文本表(文件路径);
    }

    static byte[] 读取文本源字节(string 当前文件路径)
    {
        if (Path.GetExtension(当前文件路径).Equals(".gz", StringComparison.OrdinalIgnoreCase))
        {
            using var file = new GZipStream(File.OpenRead(当前文件路径), CompressionMode.Decompress);
            using var ms = new MemoryStream();
            file.CopyTo(ms);
            return ms.ToArray();
        }

        return File.ReadAllBytes(当前文件路径);
    }

    static int 获取文本起始偏移(string 当前文件路径, byte[] 源文本)
    {
        if (!是偏移文本表(当前文件路径) || 源文本.Length < 12)
        {
            return 0;
        }

        int 偏移 = BitConverter.ToInt32(源文本, 8);
        return 偏移 >= 0 && 偏移 < 源文本.Length ? 偏移 : 0;
    }

    static bool 是假名(char 字符)
    {
        return (字符 >= '\u3040' && 字符 <= '\u309F')
            || (字符 >= '\u30A0' && 字符 <= '\u30FF');
    }

    static bool 是中日韩文字(char 字符)
    {
        return 字符 >= '\u4E00' && 字符 <= '\u9FFF';
    }

    static bool 是文本样式字符(char 字符)
    {
        if (char.IsControl(字符))
        {
            return false;
        }

        return char.IsLetterOrDigit(字符)
            || char.IsWhiteSpace(字符)
            || char.IsPunctuation(字符)
            || char.IsSymbol(字符)
            || (字符 >= '\u3000' && 字符 <= '\u303F')
            || (字符 >= '\uFF00' && 字符 <= '\uFFEF')
            || 是假名(字符)
            || 是中日韩文字(字符);
    }

    static bool 值得导出为文本(IReadOnlyCollection<string> 条目列表)
    {
        List<string> 非空条目 = 条目列表
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(200)
            .ToList();

        if (非空条目.Count == 0)
        {
            return false;
        }

        bool 含假名 = false;
        bool 含中日韩 = false;
        int 可见字符数 = 0;
        int 文本样式字符数 = 0;

        foreach (string 条目 in 非空条目)
        {
            foreach (char 字符 in 条目)
            {
                if (!char.IsControl(字符))
                {
                    可见字符数++;
                }

                if (是文本样式字符(字符))
                {
                    文本样式字符数++;
                }

                if (是假名(字符))
                {
                    含假名 = true;
                }

                if (是中日韩文字(字符))
                {
                    含中日韩 = true;
                }
            }
        }

        if (!(含假名 || 含中日韩) || 可见字符数 == 0)
        {
            return false;
        }

        return 文本样式字符数 * 100 >= 可见字符数 * 85;
    }

    static string 读取STRTBL数值串(byte[] 源文本, int 指针位置, int 长度)
    {
        if (长度 <= 0 || 指针位置 < 0 || 指针位置 >= 源文本.Length)
        {
            return string.Empty;
        }

        int UTF16起点 = 指针位置 % 2 == 0 ? 指针位置 : 指针位置 + 1;
        if (UTF16起点 + 长度 * 2 <= 源文本.Length)
        {
            string utf16结果 = Encoding.Unicode.GetString(源文本, UTF16起点, 长度 * 2);
            string utf16数字 = new string(utf16结果.Where(char.IsDigit).ToArray());
            if (utf16数字.Length > 0)
            {
                return utf16数字;
            }
        }

        int ASCII终点 = 指针位置;
        while (ASCII终点 < 源文本.Length && 源文本[ASCII终点] != 0)
        {
            ASCII终点++;
        }

        if (ASCII终点 > 指针位置)
        {
            string ascii结果 = Encoding.ASCII.GetString(源文本, 指针位置, ASCII终点 - 指针位置);
            string ascii数字 = new string(ascii结果.Where(char.IsDigit).ToArray());
            if (ascii数字.Length > 0)
            {
                return ascii数字;
            }
        }

        return string.Empty;
    }

    static byte[] 生成24位BMP(byte[,] 灰度数据, int 缩放倍数)
    {
        int 原高 = 灰度数据.GetLength(0);
        int 原宽 = 灰度数据.GetLength(1);
        int 宽 = 原宽 * 缩放倍数;
        int 高 = 原高 * 缩放倍数;
        int 行步长 = (宽 * 3 + 3) & ~3;
        int 像素区大小 = 行步长 * 高;
        int 文件大小 = 14 + 40 + 像素区大小;

        using var ms = new MemoryStream(文件大小);
        using var bw = new BinaryWriter(ms);

        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(文件大小);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(54);

        bw.Write(40);
        bw.Write(宽);
        bw.Write(高);
        bw.Write((short)1);
        bw.Write((short)24);
        bw.Write(0);
        bw.Write(像素区大小);
        bw.Write(2835);
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        byte[] 填充 = new byte[行步长 - 宽 * 3];
        for (int y = 高 - 1; y >= 0; y--)
        {
            int 源Y = y / 缩放倍数;
            for (int x = 0; x < 宽; x++)
            {
                int 源X = x / 缩放倍数;
                byte 灰度 = 灰度数据[源Y, 源X];
                bw.Write(灰度);
                bw.Write(灰度);
                bw.Write(灰度);
            }

            if (填充.Length > 0)
            {
                bw.Write(填充);
            }
        }

        bw.Flush();
        return ms.ToArray();
    }

    public static string 导出STRTBL图像(string 输入文件)
    {
        byte[] 源文本 = File.ReadAllBytes(输入文件);
        if (源文本.Length < 20 || Encoding.ASCII.GetString(源文本, 0, 8) != "STRTBL10")
        {
            throw new InvalidDataException("这不是受支持的 STRTBL10 文件。");
        }

        int 记录数 = BitConverter.ToInt32(源文本, 12);
        int 记录头偏移 = 16;
        if (记录数 <= 0 || 记录头偏移 + 8 > 源文本.Length)
        {
            throw new InvalidDataException("STRTBL10 头部无效。");
        }

        int 每记录字段数 = BitConverter.ToInt32(源文本, 16);
        if (每记录字段数 <= 0)
        {
            throw new InvalidDataException("字段数无效。");
        }

        List<int> 记录偏移组 = new List<int>(记录数);
        for (int i = 0; i < 记录数; i++)
        {
            记录偏移组.Add(BitConverter.ToInt32(源文本, 记录头偏移 + i * 8 + 4));
        }

        List<List<int>> 数值图 = new List<List<int>>(记录数);
        int 最大宽度 = 0;
        int 最大数值 = 0;

        foreach (int 记录偏移 in 记录偏移组)
        {
            List<int> 当前行 = new List<int>();
            for (int i = 0; i < 每记录字段数; i++)
            {
                int 项偏移 = 记录偏移 + i * 8;
                if (项偏移 + 8 > 源文本.Length)
                {
                    break;
                }

                int 长度 = BitConverter.ToInt32(源文本, 项偏移);
                int 指针 = BitConverter.ToInt32(源文本, 项偏移 + 4);
                string 数值串 = 读取STRTBL数值串(源文本, 指针, 长度);
                if (数值串.Length == 0)
                {
                    当前行.Add(0);
                    continue;
                }

                foreach (char 数字字符 in 数值串)
                {
                    int 数值 = 数字字符 - '0';
                    当前行.Add(数值);
                    if (数值 > 最大数值)
                    {
                        最大数值 = 数值;
                    }
                }
            }

            最大宽度 = Math.Max(最大宽度, 当前行.Count);
            数值图.Add(当前行);
        }

        if (最大宽度 == 0)
        {
            throw new InvalidDataException("未能从 STRTBL10 中解析出任何可视化数据。");
        }

        byte[,] 灰度数据 = new byte[数值图.Count, 最大宽度];
        for (int y = 0; y < 数值图.Count; y++)
        {
            List<int> 当前行 = 数值图[y];
            for (int x = 0; x < 最大宽度; x++)
            {
                int 数值 = x < 当前行.Count ? 当前行[x] : 0;
                灰度数据[y, x] = 数值 <= 1 || 最大数值 <= 1
                    ? (byte)255
                    : (byte)(255 - 数值 * 255 / 最大数值);
            }
        }

        string 输出路径 = Path.GetFullPath(输入文件 + ".x4.bmp");
        File.WriteAllBytes(输出路径, 生成24位BMP(灰度数据, 4));
        return 输出路径;
    }

    static void 确认ehppack可用()
    {
        if (string.IsNullOrWhiteSpace(ehppack目录))
        {
            ehppack目录 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ehppack.exe");
        }

        if (!File.Exists(ehppack目录))
        {
            throw new FileNotFoundException("未找到 ehppack.exe，请将它放到程序运行目录后再试。", ehppack目录);
        }
    }

    static int 执行ehppack(string 参数)
    {
        确认ehppack可用();

        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ehppack目录,
                Arguments = 参数,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static void 初始化()
    {
        根目录 = AppDomain.CurrentDomain.BaseDirectory;

        // 解包工具项目：https://github.com/xan1242/ehppack
        ehppack目录 = Path.Combine(根目录, "ehppack.exe");

        解包目录 = Path.Combine(根目录, "Extraction");
        if (Directory.Exists(解包目录))
        {
            Directory.Delete(解包目录, true);
        }

        JSON目录 = Path.Combine(根目录, "JSON");
        if (Directory.Exists(JSON目录))
        {
            Directory.Delete(JSON目录, true);
        }

        TXT目录 = Path.Combine(根目录, "TXT");
        if (Directory.Exists(TXT目录))
        {
            Directory.Delete(TXT目录, true);
        }

        BIN目录 = Path.Combine(Path.Combine(根目录, "Tranz"));
        if (Directory.Exists(BIN目录))
        {
            Directory.Delete(BIN目录, true);
        }

        EHP目录 = Path.Combine(Path.Combine(根目录, "EHP"));
        if (Directory.Exists(EHP目录))
        {
            Directory.Delete(EHP目录, true);
        }
    }

    public static void LF转CRLF(string 输入目录)
    {
        string targetDirectory = 输入目录;

        try
        {
            // 递归获取目标目录及其子目录中所有的 .json 文件
            var jsonFiles = Directory.EnumerateFiles(targetDirectory, "*.json", SearchOption.AllDirectories);

            int processedCount = 0;
            int convertedCount = 0;

            foreach (string filePath in jsonFiles)
            {
                Console.WriteLine($"正在处理文件: {filePath}");
                processedCount++;

                // 读取文件内容
                string originalContent;
                try
                {
                    // 使用 File.ReadAllText 读取文件，它会根据文件编码自动处理换行符
                    // 但为了安全，我们最好手动处理，确保准确性
                    originalContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"  - 错误：无法读取文件 '{filePath}'。{ex.Message}");
                    continue;
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"  - 错误：文件 '{filePath}' 过大，内存不足。{ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  - 错误：读取文件 '{filePath}' 时发生未知错误。{ex.Message}");
                    continue;
                }

                // 将所有现有换行符标准化为 LF，然后替换为 CRLF
                // 这样可以避免将 CRLF 变为 CRCRLF 的情况
                string tempContent = originalContent.Replace("\r\n", "\n"); // 先将所有 CRLF 转换为 LF
                string newContent = tempContent.Replace("\n", "\r\n");     // 再将所有 LF 转换为 CRLF

                // 只有当内容发生变化时才写回文件
                if (newContent != originalContent)
                {
                    try
                    {
                        // 使用 File.WriteAllText 写入文件，指定 UTF8 编码
                        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
                        convertedCount++;
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"  - 错误：无法写入文件 '{filePath}'。{ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  - 错误：写入文件 '{filePath}' 时发生未知错误。{ex.Message}");
                    }
                }
            }

            Console.WriteLine("\n--------------------------");
            Console.WriteLine($"所有JSON文件处理完成。共处理 {processedCount} 个文件，其中 {convertedCount} 个文件的换行符被转换。");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"错误：指定的目录 '{targetDirectory}' 不存在。");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"错误：无权访问目录 '{targetDirectory}'。请检查权限或以管理员身份运行。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生未知错误: {ex.Message}");
        }
    }

    public static void 解包ehp(string 输入目录)
    {
        if (!Directory.Exists(输入目录))
        {
            throw new DirectoryNotFoundException($"指定的目录不存在：{输入目录}");
        }

        string searchPattern = "*.ehp";
        List<string> 源文件列表 = Directory.EnumerateFiles(输入目录, searchPattern, SearchOption.AllDirectories).ToList();
        if (源文件列表.Count == 0)
        {
            throw new FileNotFoundException($"在目录“{输入目录}”下未找到任何 .ehp 文件。");
        }

        foreach (string 源文件路径 in 源文件列表)
        {
            string 相对路径 = 获取相对路径(源文件路径, 输入目录);
            string 输出路径 = Path.Combine(解包目录, 相对路径);
            if (!Directory.Exists(输出路径)) Directory.CreateDirectory(输出路径);
            // 第一个参数是源文件位置 第二个参数是输出位置（在此程序的OUTPUT文件夹）
            string 参数 = $"\"{源文件路径}\" \"{输出路径}\"";

            int exitCode = 执行ehppack(参数);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"解包失败：{源文件路径}，ehppack 退出码：{exitCode}");
            }
        }
    }

    public static IEnumerable<string> 获取Lj文件()
    {
        return Directory.EnumerateFiles(解包目录, "*.*", SearchOption.AllDirectories)
            .Where(是文本候选文件);
    }

    public static IEnumerable<string> 获取bin与gz文件()
    {
        return Directory.EnumerateFiles(解包目录, "*.*", SearchOption.AllDirectories)
            .Where(file =>
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                return (file.EndsWith(".bin") || file.EndsWith(".gz"));
            });
    }


    public static void Lj台词转换为TXT(string 当前文件路径)
    {
        byte[] 源文本 = 读取文本源字节(当前文件路径);
        string 相对路径 = 获取相对路径(当前文件路径, 解包目录);
        // 保留原扩展名，直接在后面加txt，方便将来转回bin和gz
        string 输出路径 = Path.Combine(TXT目录, 相对路径) + ".txt";
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        List<string> 原文条目 = new();
        StringBuilder sb = new StringBuilder();
        StringBuilder 当前条目 = new();
        int index = 获取文本起始偏移(当前文件路径, 源文本);
        while (index < 源文本.Length)
        {
            int count = index + 2 <= 源文本.Length ? 2 : 源文本.Length - index;
            string 双字节 = Encoding.Unicode.GetString(源文本, index, count);
            if (双字节 == "\n")
                sb.Append("\n-----\n");
            else if (双字节 == "\0")
            {
                sb.Append("\n*****\n");
                原文条目.Add(当前条目.ToString());
                当前条目.Clear();
            }
            else
            {
                sb.Append(双字节);
                当前条目.Append(双字节);
            }
            index += 2;
        }

        if (值得导出为文本(原文条目))
        {
            using (var 输出文本 = new StreamWriter(输出路径, false, Encoding.Unicode))
            {
                输出文本.Write(sb.ToString());
            }
        }
    }

    public static string? 返回搜索结果文件名(string 当前文件路径, string 关键字)
    {
        byte[] 源文本 = 读取文本源字节(当前文件路径);

        StringBuilder sb = new();
        int index = 获取文本起始偏移(当前文件路径, 源文本);
        while (index < 源文本.Length)
        {
            int count = index + 2 <= 源文本.Length ? 2 : 源文本.Length - index;
            string 双字节 = Encoding.Unicode.GetString(源文本, index, count);

            // 终止符，表示该条目结束
            if (双字节 == "\0")
            {
                if (sb.ToString().Contains(关键字))
                {
                    return 当前文件路径;
                }
                sb = new();
            }
            else
            {
                sb.Append(双字节);
            }
            index += 2;
        }
        return null;
    }

    public static void Lj台词转换为JSON(string 当前文件路径)
    {
        byte[] 源文本 = 读取文本源字节(当前文件路径);
        string 相对路径 = 获取相对路径(当前文件路径, 解包目录);
        // 保留原扩展名，直接在后面加txt，方便将来转回bin和gz
        string 输出路径 = Path.Combine(JSON目录, 相对路径) + ".json";
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        List<string> 原文条目 = new List<string>();
        StringBuilder sb = new();
        int index = 获取文本起始偏移(当前文件路径, 源文本);
        while (index < 源文本.Length)
        {
            int count = index + 2 <= 源文本.Length ? 2 : 源文本.Length - index;
            string 双字节 = Encoding.Unicode.GetString(源文本, index, count);

            // 终止符，表示该条目结束
            if (双字节 == "\0")
            {
                原文条目.Add(sb.ToString());
                sb = new();
            }
            else
            {
                sb.Append(双字节);
            }
            index += 2;
        }

        if (值得导出为文本(原文条目))
        {
            var jobj = 原文条目.Select((item, index) => new JObject
            {
                ["key"] = index.ToString().PadLeft(6, '0'),
                ["original"] = item,
                ["translation"] = "",
                ["stage"] = 0
            }).ToList();
            string jsonContent = JsonConvert.SerializeObject(jobj, Formatting.Indented);
            File.WriteAllText(输出路径, jsonContent, Encoding.UTF8);
        }
    }



    public static void JSON转换为Lj台词(string 当前文件路径)
    {
        string 短文件名 = Path.GetFileNameWithoutExtension(当前文件路径);
        bool 是bl = 是偏移文本表(当前文件路径);
        bool 是gz = 短文件名.EndsWith(".gz");
        bool 是LJ = 是Lj文本文件(当前文件路径);

        string 相对路径 = 获取相对路径(当前文件路径, JSON目录);
        string 输出路径 = Path.Combine(BIN目录, 相对路径);
        // 去掉.json后缀，恢复原后缀
        输出路径 = 输出路径.Substring(0, 输出路径.Length - 5);
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        // 接下来先读取JSON，拿到条目List
        string JSON字典文本 = File.ReadAllText(当前文件路径);
        JArray JSON数组 = JArray.Parse(JSON字典文本);

        // 如果是gz，使用GZipStream
        using var 输出文件 = 是gz ? (Stream)new GZipStream(File.Create(输出路径), CompressionMode.Compress) : File.Create(输出路径);
        List<byte> 已写入字节 = new();
        int number = 0;
        if (!是LJ)
        {
            已写入字节.AddRange(new byte[] { 0xFF, 0xFE });
            number = 1;
        }
        List<byte> 文本偏移 = new();

        // 逐条写入，每条写完都加\0
        foreach (JObject jobj in JSON数组.Children<JObject>())
        {
            // -1已隐藏，0未翻译，这俩将使用原文original，否则使用译文translation
            // PSP里换行是\x0A\x00，对应\n+空字符，因为unicode固定占用两个字节，所以\n后面会自动补
            int stage = jobj.Value<int?>("stage") ?? 0;
            string original = jobj.Value<string>("original") ?? string.Empty;
            string translation = jobj.Value<string>("translation") ?? string.Empty;
            string 当前条目 = stage > 0 ? translation : original;
            byte[] 待写入字节 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(当前条目.Replace("\\n", "\n")));
            已写入字节.AddRange(待写入字节);
            // 条目结束，写入\0
            已写入字节.AddRange(new byte[] { 0x00, 0x00 });
            // 暂时不知道bl代表什么，总之先偏移！
            if (是bl)
            {
                number++;
                文本偏移.AddRange(BitConverter.GetBytes(已写入字节.Count));
            }
        }

        if (是LJ)
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
            // 写入索引IJ文件
            string 索引输出路径 = 输出路径.Replace("Lj", "Ij");
            using var 索引输出文件 = 是gz ? (Stream)new GZipStream(File.Create(索引输出路径), CompressionMode.Compress) : File.Create(索引输出路径);
            索引输出文件.Write(文本索引.ToArray(), 0, 文本索引.Count);
        }
        else
        {
            输出文件.Write(BitConverter.GetBytes(number), 0, 4);
            输出文件.Write(new byte[] { 0x0c, 0x00, 0x00, 0x00 }, 0, 4);
            输出文件.Write(BitConverter.GetBytes(number * 4 + 12), 0, 4);
            输出文件.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0, 4);
            输出文件.Write(文本偏移.ToArray(), 0, 文本偏移.Count);
            输出文件.Write(已写入字节.ToArray(), 0, 已写入字节.Count);
        }
    }

    public static void 合并BIN()
    {
        // 如果解包目录不存在，重新生成解包目录
        if (!Directory.Exists(解包目录))
        {
            Console.WriteLine("请拖入原游戏的USRDIR目录");
            string path = (Console.ReadLine() ?? string.Empty).Trim('"');
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new DirectoryNotFoundException("未提供原游戏目录");
            }
            Console.WriteLine("开始解包……");
            解包ehp(path);
            Console.WriteLine("解包完毕！");
        }
        // 有解包目录的情况下，将BIN目录的文件覆盖到解包目录
        var 待复制文件 = Directory.EnumerateFiles(BIN目录, "*.*", SearchOption.AllDirectories);
        // 写得比较暴力，回头再优化
        foreach (var item in 待复制文件)
        {
            File.Copy(item, item.Replace(BIN目录, 解包目录), true);
        }
    }

    public static void 批量打包为EHP()
    {
        List<string> EHP原路径集合 = 获取EHP目录(解包目录);
        if (EHP原路径集合.Count == 0)
        {
            Console.WriteLine("没有找到需要重新打包的EHP目录");
            return;
        }

        foreach (string EHP原路径 in EHP原路径集合)
        {
            string 相对路径 = 获取相对路径(EHP原路径, 解包目录);
            string 输出路径 = Path.Combine(EHP目录, Path.GetDirectoryName(相对路径));
            string 文件名 = Path.GetFileName(相对路径);
            string 输出EHP路径 = Path.Combine(输出路径, 文件名);
            if (!Directory.Exists(输出路径)) Directory.CreateDirectory(输出路径);
            // 第一个参数是源文件位置 第二个参数是输出位置（在此程序的OUTPUT文件夹）
            string 参数 = $"-p \"{EHP原路径}\" \"{输出EHP路径}\"";

            int exitCode = 执行ehppack(参数);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"打包失败：{EHP原路径}，ehppack 退出码：{exitCode}");
            }
        }
    }

    public static bool 二次检查EHP()
    {
        bool 还有空文件 = false;
        foreach (string EHP原路径 in 获取EHP目录(解包目录))
        {
            string 相对路径 = 获取相对路径(EHP原路径, 解包目录);
            string 输出路径 = Path.Combine(EHP目录, Path.GetDirectoryName(相对路径));
            string 文件名 = Path.GetFileName(相对路径);
            string 输出EHP路径 = Path.Combine(输出路径, 文件名);

            FileInfo fileInfo = new FileInfo(输出EHP路径);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                还有空文件 = true;
                string 参数 = $"-p \"{EHP原路径}\" \"{输出EHP路径}\"";
                Console.WriteLine($"ehppack -p \"{EHP原路径}\" \"{输出EHP路径}\"");

                int exitCode = 执行ehppack(参数);
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"二次打包失败：{EHP原路径}，ehppack 退出码：{exitCode}");
                }
            }
        }
        return 还有空文件;
    }

    public static List<string> 获取EHP目录(string 根目录)
    {
        List<string> 返回值 = new List<string>();
        // 只需要pack被tranz的ehp，而不是所有
        foreach (var item in Directory.EnumerateDirectories(BIN目录, "*.ehp", SearchOption.AllDirectories))
        {
            返回值.Add(item.Replace(BIN目录, 解包目录));
        }
        return 返回值;
    }

    public static IEnumerable<string> 获取JSON文件()
    {
        if (!Directory.Exists(JSON目录))
        {
            Console.WriteLine("请拖入译文目录(就是那个UTF8文件夹)");
            JSON目录 = (Console.ReadLine() ?? string.Empty).Trim('"');
        }

        if (!Directory.Exists(JSON目录))
        {
            throw new DirectoryNotFoundException($"未找到译文目录：{JSON目录}");
        }

        return Directory.EnumerateFiles(JSON目录, "*.json", SearchOption.AllDirectories);
    }


    public static string 获取相对路径(string 绝对路径, string 根目录)
    {
        return 绝对路径.Substring(根目录.Length + 1);
    }
}

