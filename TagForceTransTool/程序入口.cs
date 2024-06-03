using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

class 程序入口
{
    static void Main()
    {
        工具类.初始化();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("请按下序号：\n[ 1 ] - 提取文本 - [ 1 ]\n[ 2 ] - 导入文本 - [ 2 ]");
            switch (Console.ReadKey(true).KeyChar)
            {
                case '1':
                    提取文本();
                    break;
                case '2':
                    JSON转EHP();
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
            Console.ReadKey();
        }
    }

    static void 提取文本()
    {
        Console.WriteLine("请拖入需要解包的目录并回车\n解包结果会生成在此程序目录下的Extraction文件夹\n不会对源文件造成损害，大可安心");
        string path = Console.ReadLine().Trim('"');  // 拖入文件的路径
        Console.WriteLine("开始解包……");
        工具类.解包ehp(path);
        Console.WriteLine("解包完毕，请按任意键继续\n（如果解包时间少于1秒，请过1秒再按，不然电脑反应不过来）");
        Console.ReadKey();
        Console.WriteLine("开始扫描名字带有“Lj”的bin/gz");
        var Lj文件集合 = 工具类.获取Lj文件();
        Console.WriteLine("获取完毕！\n开始转换Lj文件为JSON");
        foreach (var Lj文件 in Lj文件集合)
        {
            工具类.Lj台词转换为JSON(Lj文件);
        }
        Console.WriteLine("JSON导出完毕！请检查此程序目录下的JSON文件夹");
    }

    static void JSON转EHP()
    {
        var JSON文件集合 = 工具类.获取JSON文件();
        foreach (var JSON文件 in JSON文件集合)
        {
            工具类.JSON转换为Lj台词(JSON文件);
        }
        Console.WriteLine("Lj台词导出到Tranz文件夹，接下来将Tranz复制到Extraction，没有则会重新生成");
        工具类.合并BIN();
        工具类.批量打包为EHP();
        Console.WriteLine("EHP打包完毕！请检查此程序目录下的EHP文件夹");
    }

    static void 导入已有文本()
    {
        Console.WriteLine("请拖入原文TXT");
        string 原TXT = Console.ReadLine().Trim('"');

        string 原文本 = File.ReadAllText(原TXT);
        原文本 = 原文本.Replace("\n-----\n", "\n");

        List<string> 原列表 = 原文本.Split(new string[] { "\n*****\n" }, StringSplitOptions.None).ToList();

        Console.WriteLine("请拖入译文TXT");
        string 现TXT = Console.ReadLine().Trim('"');

        string 现文本 = File.ReadAllText(现TXT, Encoding.Unicode);
        现文本 = 现文本.Replace("\r\n", "\n").Replace("\n-----\n", "\n");

        List<string> 现列表 = 现文本.Split(new string[] { "\n*****\n" }, StringSplitOptions.None).ToList();

        if (原列表.Count != 现列表.Count)
        {
            Console.WriteLine($"原{原列表.Count}现{现列表.Count}");
            Console.ReadKey();
            return;
        }
        List<JObject> jobj = new();
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
                ["stage"] = 0
            });
        }
        string jsonContent = JsonConvert.SerializeObject(jobj, Formatting.Indented);
        File.WriteAllText(Path.ChangeExtension(原TXT, "json"), jsonContent, Encoding.UTF8);
        Console.WriteLine("完毕！");
        Console.ReadLine();
    }
}
