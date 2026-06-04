using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
class 程序入口
{
    static void Main()
    {
        工具类.初始化();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("运行前请先确认路径中是否有空格或中文……");
            Console.WriteLine("请按下序号：\n[ 1 ] - 提取文本 - [ 1 ]\n[ 2 ] - 导入文本 - [ 2 ]\n[ 5 ] - 导出STRTBL图像 - [ 5 ]\n[ 6 ] - 转换行符 - [ 6 ]");
            try
            {
                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        提取文本();
                        break;
                    case '2':
                        JSON转EHP();
                        break;
                    case '5':
                        导出STRTBL图像();
                        break;
                    case '6':
                        Console.WriteLine("请拖入要转换的目录");
                        工具类.LF转CRLF((Console.ReadLine() ?? string.Empty).Trim('"'));
                        Console.WriteLine("换行符转换完毕");
                        break;
                    case '7':
                        测试.搜索文本();
                        break;
                    case '8':
                        测试.读取二进制();
                        break;
                    case '9':
                        导入已有文本();
                        break;
                    default:
                        Console.WriteLine("请按下正确的按键");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误：{ex.Message}");
            }
            Console.ReadKey();
        }
    }

    static void 提取文本()
    {
        Console.WriteLine("请拖入需要解包的目录并回车\n解包结果会生成在此程序目录下的Extraction文件夹\n不会对源文件造成损害，大可安心");
        string path = (Console.ReadLine() ?? string.Empty).Trim('"');
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("未提供有效目录");
            return;
        }
        Console.WriteLine("开始解包……");
        工具类.解包ehp(path);
        Console.WriteLine("解包完毕，开始扫描名字带有“Lj”的bin/gz");
        var Lj文件集合 = 工具类.获取Lj文件().ToList();
        if (Lj文件集合.Count == 0)
        {
            Console.WriteLine("没有找到可导出的文本文件");
            return;
        }

        Console.WriteLine($"获取完毕！共找到 {Lj文件集合.Count} 个目标文件，开始转换为JSON");
        foreach (var Lj文件 in Lj文件集合)
        {
            工具类.Lj台词转换为JSON(Lj文件);
            //工具类.Lj台词转换为TXT(Lj文件);
        }
        Console.WriteLine("JSON导出完毕！请检查此程序目录下的JSON文件夹");
    }

    static void JSON转EHP()
    {
        var JSON文件集合 = 工具类.获取JSON文件().ToList();
        if (JSON文件集合.Count == 0)
        {
            Console.WriteLine("未找到任何JSON文件");
            return;
        }

        foreach (var JSON文件 in JSON文件集合)
        {
            工具类.JSON转换为Lj台词(JSON文件);
        }
        工具类.合并BIN();
        Console.WriteLine("合并完毕！正在打包，请稍候……");
        工具类.批量打包为EHP();
        bool 还有空文件 = 工具类.二次检查EHP();
        while (还有空文件)
        {
            Thread.Sleep(2000);
            还有空文件 = 工具类.二次检查EHP();
        }
        Console.WriteLine("EHP打包完毕！请检查此程序目录下的EHP文件夹");
    }

    static void 导出STRTBL图像()
    {
        Console.WriteLine("请拖入要导出的 STRTBL10 文件");
        string path = (Console.ReadLine() ?? string.Empty).Trim('"');
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("未提供文件路径");
            return;
        }

        string 输出路径 = 工具类.导出STRTBL图像(path);
        Console.WriteLine($"导出完毕：{输出路径}");
    }

    static void 导入已有文本()
    {
        Console.WriteLine("请拖入原文TXT");
        string 原TXT = (Console.ReadLine() ?? string.Empty).Trim('"');
        if (string.IsNullOrWhiteSpace(原TXT))
        {
            Console.WriteLine("未提供原文TXT");
            return;
        }

        string 原文本 = File.ReadAllText(原TXT);
        原文本 = 原文本.Replace("\n-----\n", "\n");

        List<string> 原列表 = 原文本.Split(new string[] { "\n*****\n" }, StringSplitOptions.None).ToList();

        Console.WriteLine("请拖入译文TXT");
        string 现TXT = (Console.ReadLine() ?? string.Empty).Trim('"');
        if (string.IsNullOrWhiteSpace(现TXT))
        {
            Console.WriteLine("未提供译文TXT");
            return;
        }

        string 现文本 = File.ReadAllText(现TXT, Encoding.Unicode);
        现文本 = 现文本.Replace("\r\n", "\n").Replace("\n-----\n", "\n");

        List<string> 现列表 = 现文本.Split(new string[] { "\n*****\n" }, StringSplitOptions.None).ToList();

        if (原列表.Count != 现列表.Count)
        {
            Console.WriteLine($"原{原列表.Count}现{现列表.Count}");
            Console.ReadKey();
            return;
        }
        List<JObject> jobj = new List<JObject>();
        string 译文 = "";
        int stage = 0;
        for (int i = 0; i < 原列表.Count; i++)
        {
            if (原列表[i] != 现列表[i])
            {
                译文 = 现列表[i];
                stage = 1;
            }
            else
            {
                译文 = "";
                stage = 0;
            }
            jobj.Add(new JObject
            {
                ["key"] = i.ToString().PadLeft(6, '0'),
                ["original"] = 原列表[i],
                ["translation"] = 译文,
                ["stage"] = stage
            });
        }
        string jsonContent = JsonConvert.SerializeObject(jobj, Formatting.Indented);
        File.WriteAllText(Path.ChangeExtension(原TXT, "json"), jsonContent, Encoding.UTF8);
        Console.WriteLine("完毕！");
    }
}
