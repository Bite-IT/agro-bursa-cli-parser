using AgroBursaParser.Tables;
using HtmlAgilityPack;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AgroBursaParser {



    class ZernoParser {

        static System.Net.WebClient web = new System.Net.WebClient { Encoding = Encoding.UTF8 };

        private static List<string> UrlList;
        private static List<InfoNote> infoNotesList;
        private static List<string> keywordList;
        private static List<Results> EndList;

        public static Results[] MainMethod(List<string> keywords, string Url) {

            EndList = new List<Results>();
            keywordList = keywords.ToList();
            DateTime days = new DateTime();
            string[] date = new string[8];
            string[] month = new string[12];

            days = DateTime.Now;

            month[0] = "01"; month[1] = "02"; month[2] = "03"; month[3] = "04";
            month[4] = "05"; month[5] = "06"; month[6] = "07"; month[7] = "08";
            month[8] = "09"; month[9] = "10"; month[10] = "11"; month[11] = "12";
            string workingDirectory = Environment.CurrentDirectory;
            for(int j = 0; j < 8; j++) {
                date[j] = days.AddDays(-j).Date.ToString("dd.MM");
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(web.DownloadString(Url));

            IEnumerable<HtmlNode> SectionsNodes = doc.DocumentNode.SelectNodes("//div[@class='view-content']");
            List<string> ListNewsNodes = new List<string>();
            int k = 0;
            
            foreach(HtmlNode sectionNode in SectionsNodes) {
                k++;
                IEnumerable<HtmlNode> ListTmpNodes = sectionNode.SelectNodes(sectionNode.XPath + "//span[@class='field-content']");
                if(k != 6) {
                    foreach(HtmlNode siteNode in ListTmpNodes) {

                        if(Regex.Match(siteNode.InnerHtml, "/node/\\S*\"").Value.Length != 0) {
                            ListNewsNodes.Add(Url + Regex.Match(siteNode.InnerHtml, "/node/\\S*\"").Value.Substring(0, Regex.Match(siteNode.InnerHtml, "/node/\\S*\"").Value.Length - 1));
                            //ListNewsNodes[ListNewsNodes.Count - 1].Replace("\"", " ");
                        }
                        /*string qwe = "";
                        qwe = "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd']";
                        qwe = "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd views-row-first']";
                        qwe = "//div[@class='views-row views-row-" + i.ToString() + " views-row-even']";
                        if(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd']") != null) {

                            ListNewsNodes.Add(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd']").InnerHtml);
                        } else if(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd views-row-first']") != null) {

                            ListNewsNodes.Add(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-odd views-row-first']").InnerHtml);
                        } else if(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-even']") != null) {

                            ListNewsNodes.Add(siteNode.SelectSingleNode(siteNode.XPath + "//div[@class='views-row views-row-" + i.ToString() + " views-row-even']").InnerHtml);
                        }       */
                    }
                } else{
                    break;
                }

            }

            ListNewsNodes = ListNewsNodes.GroupBy(x => x).Select(x => x.First()).ToList();

            for(int i = 0; i < ListNewsNodes.Count; i++) {
                string text = "";
                string dateNews = "";
                bool flag = false;
                doc.LoadHtml(web.DownloadString(ListNewsNodes[i]));

                HtmlNode newsNode = doc.DocumentNode.SelectSingleNode("//span[@class='time pubdate']");
                
                if(Regex.Match(newsNode.InnerText, @"\s\S*/\S*/").Value.Length != 0) {
                    dateNews = Regex.Match(newsNode.InnerText, @"\s\S*/\S*/").Value.Substring(0, Regex.Match(newsNode.InnerText, @"\s\S*/\S*/").Value.Length - 1);
                    dateNews = dateNews.Substring(4, 2) + "." + dateNews.Substring(1, 2);
                }
                for(int j = 0; j < date.Length; j++) {
                    if(dateNews.Contains(date[j])) {
                        flag = true;
                        break;
                    }
                }

                if(!flag) {
                    continue;
                }

                IEnumerable<HtmlNode> newsNodes = doc.DocumentNode.SelectNodes("//div[@class='field-item odd']");
                foreach(HtmlNode node in newsNodes) {
                    text = node.InnerText;
                    if((text != "") && (CompareText(text, keywordList.ToArray()))) {
                        EndList.Add(new Results { Text = text, Date = dateNews, Url = ListNewsNodes[i] });
                    }
                }
                
                    

                
                
                int b = 1;
            }
            return EndList.ToArray();
        }

        private static bool CompareText(string text, string[] comparedWords) {
            for(int i = 0; i < comparedWords.Length; i++) {
                if(Regex.IsMatch(text.ToLower(), @"\.*" + comparedWords[i] + @"\.*")) {
                    return true;
                }
            }
            return false;
        }


    }

    class InfoNote {
        public string Author;
        public string Date;
        public string Text;
    }
}
