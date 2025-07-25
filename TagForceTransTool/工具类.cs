﻿using Newtonsoft.Json;
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
        string searchPattern = "*.ehp";

        foreach (string 源文件路径 in Directory.EnumerateFiles(输入目录, searchPattern, SearchOption.AllDirectories))
        {
            string 相对路径 = 获取相对路径(源文件路径, 输入目录);
            string 输出路径 = Path.Combine(解包目录, 相对路径);
            if (!Directory.Exists(输出路径)) Directory.CreateDirectory(输出路径);
            // 第一个参数是源文件位置 第二个参数是输出位置（在此程序的OUTPUT文件夹）
            string 参数 = $"\"{源文件路径}\" \"{输出路径}\"";
            //Console.WriteLine($"{ehppack目录} {参数}");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ehppack目录,
                Arguments = 参数,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();
        }
    }

    public static IEnumerable<string> 获取Lj文件()
    {
        return Directory.EnumerateFiles(解包目录, "*.*", SearchOption.AllDirectories)
            .Where(file =>
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                return (file.EndsWith(".bin") || file.EndsWith(".gz"))
                    && (fileNameWithoutExtension.Contains("Lj") || fileNameWithoutExtension.Contains("bl"))
                    // 筛掉声音
                    && !fileNameWithoutExtension.Contains("voice")
                    // 筛掉其它5门语言，只保留日语
                    && !fileNameWithoutExtension.EndsWith("bl_e")
                    && !fileNameWithoutExtension.EndsWith("bl_f")
                    && !fileNameWithoutExtension.EndsWith("bl_g")
                    && !fileNameWithoutExtension.EndsWith("bl_i")
                    && !fileNameWithoutExtension.EndsWith("bl_s");
            });
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
        string 扩展名 = Path.GetExtension(当前文件路径);
        byte[] 源文本;
        if (扩展名 == ".gz")
        {
            using (var file = new GZipStream(File.OpenRead(当前文件路径), CompressionMode.Decompress))
            {
                var ms = new MemoryStream();
                file.CopyTo(ms);
                源文本 = ms.ToArray();
            }
        }
        else
        {
            源文本 = File.ReadAllBytes(当前文件路径);
        }
        string 相对路径 = 获取相对路径(当前文件路径, 解包目录);
        // 保留原扩展名，直接在后面加txt，方便将来转回bin和gz
        string 输出路径 = Path.Combine(TXT目录, 相对路径) + ".txt";
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        bool 有日语字符 = false;
        StringBuilder sb = new StringBuilder();
        int index = 0;
        int bias = 当前文件路径.Contains("bl") ? BitConverter.ToInt32(源文本, 8) : 0;
        index += bias;
        while (index < 源文本.Length)
        {
            int count = index + 2 <= 源文本.Length ? 2 : 源文本.Length - index;
            string 双字节 = Encoding.Unicode.GetString(源文本, index, count);
            if (双字节 == "\n")
                sb.Append("\n-----\n");
            else if (双字节 == "\0")
                sb.Append("\n*****\n");
            else
            {
                sb.Append(双字节);
                if (有日语字符 ||
                    (双字节.CompareTo("\u3040") >= 0 && 双字节.CompareTo("\u309F") <= 0) || // Hiragana
                    (双字节.CompareTo("\u30A0") >= 0 && 双字节.CompareTo("\u30FF") <= 0))   // Katakana
                {
                    有日语字符 = true;
                }
            }
            index += 2;
        }

        if (有日语字符)
        {
            using (var 输出文本 = new StreamWriter(输出路径, false, Encoding.Unicode))
            {
                输出文本.Write(sb.ToString());
            }
        }
    }

    public static string? 返回搜索结果文件名(string 当前文件路径, string 关键字)
    {
        string 扩展名 = Path.GetExtension(当前文件路径);
        byte[] 源文本;
        if (扩展名 == ".gz")
        {
            using (var file = new GZipStream(File.OpenRead(当前文件路径), CompressionMode.Decompress))
            {
                var ms = new MemoryStream();
                file.CopyTo(ms);
                源文本 = ms.ToArray();
            }
        }
        else
        {
            源文本 = File.ReadAllBytes(当前文件路径);
        }

        StringBuilder sb = new();
        int index = 0;
        int bias = 当前文件路径.Contains("bl") ? BitConverter.ToInt32(源文本, 8) : 0;
        index += bias;
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
        string 扩展名 = Path.GetExtension(当前文件路径);
        byte[] 源文本;
        if (扩展名 == ".gz")
        {
            using (var file = new GZipStream(File.OpenRead(当前文件路径), CompressionMode.Decompress))
            {
                var ms = new MemoryStream();
                file.CopyTo(ms);
                源文本 = ms.ToArray();
            }
        }
        else
        {
            源文本 = File.ReadAllBytes(当前文件路径);
        }
        string 相对路径 = 获取相对路径(当前文件路径, 解包目录);
        // 保留原扩展名，直接在后面加txt，方便将来转回bin和gz
        string 输出路径 = Path.Combine(JSON目录, 相对路径) + ".json";
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        List<string> 原文条目 = new List<string>();
        bool 有日语字符 = false;
        StringBuilder sb = new();
        int index = 0;
        int bias = 当前文件路径.Contains("bl") ? BitConverter.ToInt32(源文本, 8) : 0;
        index += bias;
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
                if (有日语字符 ||
                    (双字节.CompareTo("\u3040") >= 0 && 双字节.CompareTo("\u309F") <= 0) || // Hiragana
                    (双字节.CompareTo("\u30A0") >= 0 && 双字节.CompareTo("\u30FF") <= 0))   // Katakana
                {
                    有日语字符 = true;
                }
            }
            index += 2;
        }

        if (有日语字符)
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
        bool 是bl = 短文件名.Contains("bl");
        bool 是gz = 短文件名.EndsWith(".gz");
        bool 是LJ = 短文件名.Contains("Lj");

        string 相对路径 = 获取相对路径(当前文件路径, JSON目录);
        string 输出路径 = Path.Combine(BIN目录, 相对路径);
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
        int number = 0;
        if (!是LJ)
        {
            已写入字节.AddRange(new byte[] { 0xFF, 0xFE });
            number = 1;
        }
        List<byte> 文本偏移 = new();

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
            string path = Console.ReadLine().Trim('"');  // 拖入文件的路径
            Console.WriteLine("开始解包……");
            解包ehp(path);
            Console.WriteLine("解包完毕，请按任意键继续\n（如果解包时间少于1秒，请过1秒再按，不然电脑反应不过来）");
            Console.ReadKey();
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
        foreach (string EHP原路径 in 获取EHP目录(解包目录))
        {
            string 相对路径 = 获取相对路径(EHP原路径, 解包目录);
            string 输出路径 = Path.Combine(EHP目录, Path.GetDirectoryName(相对路径));
            string 文件名 = Path.GetFileName(相对路径);
            string 输出EHP路径 = Path.Combine(输出路径, 文件名);
            if (!Directory.Exists(输出路径)) Directory.CreateDirectory(输出路径);
            // 第一个参数是源文件位置 第二个参数是输出位置（在此程序的OUTPUT文件夹）
            string 参数 = $"-p \"{EHP原路径}\" \"{输出EHP路径}\"";
            //Console.WriteLine($"{ehppack目录} {参数}");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ehppack目录,
                Arguments = 参数,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();
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
            if (fileInfo.Exists && fileInfo.Length == 0)
            {
                还有空文件 = true;
                string 参数 = $"-p \"{EHP原路径}\" \"{输出EHP路径}\"";
                Console.WriteLine($"ehppack -p \"{EHP原路径}\" \"{输出EHP路径}\"");
                //Console.WriteLine($"{ehppack目录} {参数}");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ehppack目录,
                    Arguments = 参数,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();
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
            JSON目录 = Console.ReadLine().Trim('"');
        } 
        return Directory.EnumerateFiles(JSON目录, "*.json", SearchOption.AllDirectories);
    }


    public static string 获取相对路径(string 绝对路径, string 根目录)
    {
        return 绝对路径.Substring(根目录.Length + 1);
    }
}

