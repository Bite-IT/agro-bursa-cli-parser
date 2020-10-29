using AgroBursaParser.Tables;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AgroBursaParser {
    class VkParse {

        static System.Net.WebClient web = new System.Net.WebClient { Encoding = Encoding.UTF8 };

        private static List<string> UrlList;
        private static List<InfoNote> infoNotesList;
        private static List<string> keywordList;
        private static List<Results> EndList;


        public VkParse() {

        }


        public static Results[] MainMethod(List<string> keywords, List<string> Url) {

            

            DateTime days = new DateTime();
            string[] date = new string[10];
            string[] month = new string[12];
            UrlList = Url.ToList();//new List<string> { "https://vk.com/zernovoz", "https://vk.com/zernovozs", "https://vk.com/gruzvoju", "https://vk.com/id618791000" };
            keywordList = keywords.ToList();
            EndList = new List<Results>();
            int e = 0;

            days = DateTime.Now;
            date[0] = "сегодня"; date[1] = "вчера"; date[8] = "мин"; date[9] = "час";

            month[0] = "янв"; month[1] = "фев"; month[2] = "мар"; month[3] = "апр";
            month[4] = "мая"; month[5] = "июн"; month[6] = "июл"; month[7] = "авг";
            month[8] = "сен"; month[9] = "окт"; month[10] = "ноя"; month[11] = "дек";
            string workingDirectory = Environment.CurrentDirectory;
            for(int j = 2; j < 8; j++) {
                date[j] = days.AddDays(-j).Date.ToString("dd");
                date[j] += " " + month[days.AddDays(-j).Month - 1];
            }           
            

            for(int i = 0; i < UrlList.Count; i++) {
                infoNotesList = new List<InfoNote>();

                //ChromeDriver driver = new ChromeDriver(@"D:\repo\agro-bursa-cli-parser\AgroBursaParser\AgroBursaParser\");
                
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--lang=ru-RU");
                options.AddArgument("headless");

                ChromeDriver driver = new ChromeDriver (workingDirectory, options);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                driver.Navigate().GoToUrl(UrlList[i]);



                for(int j = 0; j < 40; j++) {
                    js.ExecuteScript("window.scrollBy(0,10000)");
                    Thread.Sleep(200);
                }

                string title = driver.Title;//Page title
                string html = driver.PageSource;//Page Html
                driver.Close();

                //string htmlDocumentString = web.DownloadString(UrlList[i] + "/");
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                IEnumerable<HtmlNode> TextNodes = doc.DocumentNode.SelectNodes("//div[@class='_post_content']");
                List<HtmlNode> qwe = new List<HtmlNode>();
                foreach(HtmlNode node in TextNodes) {
                    qwe.Add(node);
                    infoNotesList.Add( new InfoNote {Author = qwe[e].SelectSingleNode("//a[@class='author']").InnerText, Date = "", Text = "" }) ;
                    //infoNotesList[infoNotesList.Count - 1].Date = node.SelectSingleNode("//a[@class='wi_date']").InnerText; //rel_date rel_date_needs_update

                    if(qwe[e].SelectSingleNode("//span[@class='rel_date']").ToString() != null) {
                        infoNotesList[infoNotesList.Count - 1].Date = qwe[e].SelectSingleNode("//span[@class='rel_date']").InnerText;
                    } else if(qwe[e].SelectSingleNode("//span[@class='rel_date rel_date_needs_update']").ToString() != null) {
                        infoNotesList[infoNotesList.Count - 1].Date = qwe[e].SelectSingleNode("//span[@class='rel_date rel_date_needs_update']").InnerText;
                    }

                    if(qwe[e].SelectSingleNode("//div[@class='wall_post_text']").ToString() != null) {
                        infoNotesList[infoNotesList.Count - 1].Text = qwe[e].SelectSingleNode("//div[@class='wall_post_text']").InnerText;
                    } else if(qwe[e].SelectSingleNode("//div[@class='pi_text zoom_text']").ToString() != null) {
                        infoNotesList[infoNotesList.Count - 1].Text = qwe[e].SelectSingleNode("//div[@class='wall_post_text zoom_text']").InnerText;
                    }
                    e++;
                }

                for(int j = 0; j < infoNotesList.Count; j++) {
                    bool flag = false;
                    if(CompareText(infoNotesList[j].Date, date)) {
                        if(CompareText(infoNotesList[j].Text, keywordList.ToArray())){
                            EndList.Add(new Results { Author = infoNotesList[j].Author, Date = infoNotesList[j].Date, Text = infoNotesList[j].Text, Url = UrlList[i] });
                            flag = true;
                        }
                    }

                    if (!flag) {
                        infoNotesList[i].Author = "";
                        infoNotesList[i].Date = "";
                        infoNotesList[i].Text = "";
                    }
                    
                }

            }

            return EndList.ToArray();
        }

        private static bool CompareText(string text, string[] comparedWords) {
            for(int i = 0; i < comparedWords.Length; i++) {
                if((text.ToLower().Contains(comparedWords[i]))) {
                    return true;
                }
            }
            return false;
        }


        class InfoNote {
            public string Author;
            public string Date;
            public string Text;
        }

    }
}
