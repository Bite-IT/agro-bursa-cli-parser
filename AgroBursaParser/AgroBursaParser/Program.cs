using AgroBursaParser.Tables;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AgroBursaParser
{
    class Program
    {

        static List<string> keywordVKList;
        static List<Results> ResultsList;

        static void Main(string[] args)
        {
            ResultsList = new List<Results>();
            keywordVKList = new List<string> { @"Зерноотход", @"Фуражн\.* пшениц", @"пшениц \.*2000т\.* цена", @"ячмен \.*2000т\.* цена", @"кукуруз \.*2000т\.* цена", @"куплю пшениц" };
            ResultsList.AddRange(VkParse.MainMethod(keywordVKList.ToList(), new List<string> { "https://vk.com/zernovoz", "https://vk.com/zernovozs", "https://vk.com/gruzvoju", "https://vk.com/id618791000" }));
            AgroBursaParser.MainMethod();
            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
