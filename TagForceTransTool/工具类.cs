using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

public class 工具类
{
    static string 根目录 = string.Empty;
    static string ehppack目录 = string.Empty;
    static string 解包目录 = string.Empty;
    static string JSON目录 = string.Empty;
    static string 导入目录 = string.Empty;

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

        导入目录 = Path.Combine(Path.Combine(根目录, "Tranz"));
        if (Directory.Exists(导入目录))
        {
            Directory.Delete(导入目录, true);
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
                    && !fileNameWithoutExtension.Contains("bl_e")
                    && !fileNameWithoutExtension.Contains("bl_f")
                    && !fileNameWithoutExtension.Contains("bl_g")
                    && !fileNameWithoutExtension.Contains("bl_i")
                    && !fileNameWithoutExtension.Contains("bl_s");
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
        string 输出路径 = Path.Combine(JSON目录, 相对路径) + ".txt";
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



    public static void TXT转换为Lj台词(string 当前文件路径)
    {
        string 短文件名 = Path.GetFileNameWithoutExtension(当前文件路径);
        bool 是gz = 短文件名.EndsWith(".gz") ? true : false;

        string 相对路径 = 获取相对路径(当前文件路径, JSON目录);
        string 输出路径 = Path.Combine(导入目录, 相对路径);
        // 去掉txt后缀，恢复原后缀
        输出路径 = 输出路径.Substring(0, 输出路径.Length - 4);
        if (!Directory.Exists(Path.GetDirectoryName(输出路径))) Directory.CreateDirectory(Path.GetDirectoryName(输出路径));

        List<int> indices = new List<int>();
        using (var 输入文本 = new StreamReader(当前文件路径, Encoding.Unicode))
        {
            using (var 输出文件 = 是gz ? (Stream)new GZipStream(File.Create(输出路径), CompressionMode.Compress) : File.Create(输出路径))
            {
                string line;
                int index = 0;
                while ((line = 输入文本.ReadLine()) != null)
                {
                    byte[] bytes;
                    if (line == "-----")
                        bytes = Encoding.Unicode.GetBytes("\n");
                    else if (line == "*****")
                    {
                        bytes = Encoding.Unicode.GetBytes("\0");
                        indices.Add(index);
                    }
                    else
                        bytes = Encoding.Unicode.GetBytes(line);

                    输出文件.Write(bytes, 0, bytes.Length);
                    index += bytes.Length;
                }
            }
        }

        if (短文件名.Contains("Lj"))
        {
            string 索引文件路径 = 输出路径.Replace("Lj", "Ij");
            using (var 输出文件 = 是gz ? (Stream)new GZipStream(File.Create(索引文件路径), CompressionMode.Compress) : File.Create(索引文件路径))
            {
                foreach (int index in indices)
                {
                    byte[] bytes = BitConverter.GetBytes(index);
                    输出文件.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }


    public static IEnumerable<string> 获取TXT文件()
    {
        return Directory.EnumerateFiles(JSON目录, "*.txt", SearchOption.AllDirectories);
    }


    public static string 获取相对路径(string 绝对路径, string 根目录)
    {
        return 绝对路径.Substring(根目录.Length + 1);
    }
}

