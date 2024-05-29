﻿using System;
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
                    导入文本();
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
        Console.WriteLine("获取完毕！\n开始转换Lj文件为TXT");
        foreach (var Lj文件 in Lj文件集合)
        {
            工具类.Lj台词转换为TXT(Lj文件);
        }
        Console.WriteLine("TXT导出完毕！请检查此程序目录下的TXT文件夹");
    }

    static void 导入文本()
    {
        var TXT文件集合 = 工具类.获取TXT文件();
        foreach (var TXT文件 in TXT文件集合)
        {
            工具类.TXT转换为Lj台词(TXT文件);
        }
        Console.WriteLine("Lj台词导出完毕！请检查此程序目录下的Tranz文件夹");
    }
}
