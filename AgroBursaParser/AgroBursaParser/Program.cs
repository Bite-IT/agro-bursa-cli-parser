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
        static List<string> keywordAllList;
        static List<Results> ResultsList;

        static void Main(string[] args)
        {
            ResultsList = new List<Results>();

            

            keywordVKList = new List<string> { 
                @"Зерноотход",
                @"Фуражн\.*пшениц",
                @"пшениц\.*2000т\.*цена",
                @"ячмен\.*2000т\.*цена",
                @"кукуруз\.*2000т\.*цена",
                @"кукуруз",
                @"куплю пшениц" };

            keywordAllList = new List<string> {
                @"кризис",
                @"снижение\.*тарифа",
                @"повышение\.*тарифа",
                @"результаты\.*будут\.*лучше",
                @"результаты\.*будут\.*хуже",
                @"погод",
                @"засух",
                @"дожд"
            };
            ResultsList.AddRange(ZernoParser.MainMethod(keywordAllList.ToList(), "http://zerno.ru/"));

            ResultsList.AddRange(VkParser.MainMethod(keywordVKList.ToList(), new List<string> { "https://vk.com/zernovoz", "https://vk.com/zernovozs", "https://vk.com/gruzvoju", "https://vk.com/id618791000" }));

            StreamWriter sw = new StreamWriter("report.txt");
            for(int i = 0; i < ResultsList.Count; i++) {
                sw.WriteLine(ResultsList[i].Url + "\n" + ResultsList[i].Date + "\n" + ResultsList[i].Author + "\n" + ResultsList[i].Text + "\n_______________________________\n");
            }
            sw.Close();

            AgroBursaParser.MainMethod();
            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
