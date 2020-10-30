using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AgroBursaParser
{
    class AgroBursaParser
    {
        // веб-клиент для скачивания страниц
        static System.Net.WebClient web = new System.Net.WebClient { Encoding = Encoding.UTF8 };

        class Production
        {
            // наименование продукции
            public string Name;
            // общий URL
            public string Url;
            // признак наличия нескольких таблиц на одной странице
            public bool ManyTablesOnPage = false;
            // заголовок, который требуется извлечь и/или удалить
            public string TitleName = null;
        }

        class OutputFile
        {
            public string Path;
            public string FileName;
            public string CsvContent;
        }

        class DownloadInfo
        {
            public Production Production;
            public string ProductionPath;
            public Dictionary<string, IEnumerable<string>> DatesToDownload;
        }

        public AgroBursaParser() {

        }

        public static void MainMethod() {

            // список продукции
            List<Production> productionList = new List<Production>
            {
                new Production { Name = "Пшеница", Url = "https://agro-bursa.ru/prices/wheat/", ManyTablesOnPage = true, TitleName = "Пшеница"},
                new Production { Name = "Кукуруза", Url = "https://agro-bursa.ru/prices/corn/", ManyTablesOnPage = false, TitleName = null},
                new Production { Name = "Ячмень", Url = "https://agro-bursa.ru/prices/barley/", ManyTablesOnPage = false, TitleName = null}
            };

            List<DownloadInfo> downloadInfoList = new List<DownloadInfo>();

            // осуществляем обход по всей продукции
            foreach (Production prod in productionList) {
                DownloadInfo downloadInfo = new DownloadInfo {
                    Production = prod,
                    DatesToDownload = new Dictionary<string, IEnumerable<string>>()
                };

                string path = Path.Combine(Environment.CurrentDirectory, prod.Name);
                downloadInfo.ProductionPath = path;
                DirectoryInfo mainDirectoryInfo = new DirectoryInfo(path);
                if (!mainDirectoryInfo.Exists) {
                    mainDirectoryInfo.Create();
                }

                List<HtmlNode> linkNodes = new List<HtmlNode>();

                List<string> url = new List<string>();
                string htmlDocumentString = web.DownloadString(prod.Url + "archive/");
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlDocumentString);
                HtmlNode agroArticleNode = doc.DocumentNode.SelectSingleNode("//div[@class='agro-article']");
                IEnumerable<HtmlNode> seasonTitleNodes = agroArticleNode.SelectNodes("//a[@class='season-title']").Where(node => node.InnerText.Replace("Сезон ", "").Split('/').Select(int.Parse).ToArray()[1] >= 2020);
                foreach (HtmlNode node in seasonTitleNodes) {
                    string seasonId = node.GetAttributeValue("id", null);
                    if (seasonId == null) {
                        continue;
                    }

                    IEnumerable<HtmlNode> seasonNodes = agroArticleNode.SelectNodes($"//ul[@id='season{seasonId}']//a")
                        .Where(htmlNode => {
                            DateTime resultDate = DateTime.ParseExact(htmlNode.InnerText, "dd.MM.yyyy",
                                CultureInfo.InvariantCulture);
                            return (DateTime.Now - resultDate).TotalDays > 30 && resultDate.Year >= 2020;
                        });

                    linkNodes.AddRange(seasonNodes);
                }

                IEnumerable<IGrouping<int, HtmlNode>> linksByYears = linkNodes.GroupBy(node => DateTime.ParseExact(node.InnerHtml, "dd.MM.yyyy",
                    CultureInfo.InvariantCulture).Year);

                foreach (IGrouping<int, HtmlNode> link in linksByYears) {
                    DirectoryInfo yearDirectoryInfo = new DirectoryInfo(Path.Combine(path, link.Key.ToString()));
                    if (!yearDirectoryInfo.Exists) {
                        yearDirectoryInfo.Create();
                    }
                    IEnumerable<string> urls = link.Select(node => node.InnerText).Except(yearDirectoryInfo.GetDirectories().Select(dir => dir.Name));

                    downloadInfo.DatesToDownload.Add(link.Key.ToString(), urls);

                }

                downloadInfoList.Add(downloadInfo);
            }

            // список файлов для вывода
            List<OutputFile> outputFileList = DownloadTables(downloadInfoList);
            Console.WriteLine($"Total files downloaded - {outputFileList.Count}");
            foreach (OutputFile outputFile in outputFileList) {
                DirectoryInfo dirInfo = new DirectoryInfo(outputFile.Path);
                if (!dirInfo.Exists) {
                    dirInfo.Create();
                }

                // запись в файл
                using(FileStream fstream = new FileStream(@$"{outputFile.Path}\{outputFile.FileName}", FileMode.Create)) {
                    // преобразуем строку в байты
                    byte[] array = System.Text.Encoding.Default.GetBytes(outputFile.CsvContent);
                    // запись массива байтов в файл
                    fstream.Write(array, 0, array.Length);
                    Console.WriteLine(@$"{outputFile.Path}\{outputFile.FileName}");
                }
            }
        }

        static List<OutputFile> DownloadTables( List<DownloadInfo> downloadInfoList ) {
            List<OutputFile> outputFileList = new List<OutputFile>();

            foreach (DownloadInfo downloadInfo in downloadInfoList) {
                foreach (KeyValuePair<string, IEnumerable<string>> yearDates in downloadInfo.DatesToDownload) {
                    foreach (string date in yearDates.Value) {
                        string url = $"{downloadInfo.Production.Url}{date.Replace('.', '-')}/";
                        Console.WriteLine(url);
                        string downloadString = web.DownloadString($"{downloadInfo.Production.Url}{date.Replace('.', '-')}/");
                        if (downloadInfo.Production.ManyTablesOnPage) {
                            outputFileList.AddRange(ParseMultipleTables(downloadString, downloadInfo.ProductionPath, yearDates.Key, date, downloadInfo.Production.TitleName));
                        } else {
                            outputFileList.Add(ParseSingleTable(downloadString, downloadInfo.ProductionPath,
                                yearDates.Key, date, downloadInfo.Production.Name));
                        }
                    }
                }
            }
            return outputFileList;
        }

        static List<OutputFile> ParseMultipleTables( string webpage, string filePath, string year, string date, string titleName ) {
            List<OutputFile> result = new List<OutputFile>();

            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(webpage);
            HtmlNodeCollection tables = doc.DocumentNode.SelectNodes("//div[@id='price']//table");

            foreach (HtmlNode table in tables) {
                string parseResult = InternalParseSingleTableWithTitle(doc.DocumentNode.SelectNodes(table.XPath + "/tr"), titleName, out string fullTitleContent);
                if (parseResult != null) {
                    fullTitleContent = fullTitleContent.Replace("&#37;", "%");
                    OutputFile outputFile = new OutputFile {
                        Path = $@"{filePath}\{year}\{date}",
                        FileName = $"{fullTitleContent}.csv",
                        CsvContent = parseResult
                    };
                    result.Add(outputFile);
                }
            }


            return result;
        }

        static OutputFile ParseSingleTable( string webpage, string filePath, string year, string date, string titleName ) {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(webpage);
            HtmlNodeCollection table = doc.DocumentNode.SelectNodes("//div[@id='price']/table/tr");
            string parseResult = InternalParseSingleTable(table);
            if (parseResult != null) {
                OutputFile outputFile = new OutputFile {
                    Path = $@"{filePath}\{year}\{date}",
                    FileName = $"{titleName}.csv",
                    CsvContent = parseResult
                };
                return outputFile;
            }

            return null;

        }
        static string InternalParseSingleTableWithTitle( HtmlNodeCollection table, string titleName, out string fullTitleContent ) {
            //var aa = table.;
            HtmlNode fullTitleContentNode = table.FirstOrDefault(node => node.InnerText.Contains(titleName))?.ChildNodes.FirstOrDefault(node => node.InnerText.Contains(titleName));
            //table.Remove()
            if (fullTitleContentNode != null) {
                fullTitleContent = fullTitleContentNode.InnerText;
                fullTitleContentNode.InnerHtml = string.Empty;
                if (fullTitleContentNode.PreviousSibling == null) {
                    table.Remove(0);
                }
            } else {
                fullTitleContent = titleName;
            }

            return InternalParseSingleTable(table);
        }

        static string InternalParseSingleTable( HtmlNodeCollection table ) {

            int headerRowsCount = table[0].ChildNodes[0].GetAttributeValue("rowspan", -1);
            if (headerRowsCount == -1) {
                Console.WriteLine("Table creation error");
                //Console.ReadKey();
                return null;
            }

            try {
                int columnsCount = table.Max(item => item.ChildNodes.Count);
                string[] headerText = new string[columnsCount];
                int[] headerColumnOffsets = new int[columnsCount];
                for (int trRowIndex = 0; trRowIndex < headerRowsCount; ++trRowIndex) {
                    HtmlNode trNode = table[trRowIndex];
                    int colunmIndex = 0;
                    foreach (HtmlNode tdNode in trNode.ChildNodes) {
                        int rowspan = tdNode.GetAttributeValue("rowspan", 1);
                        int columnspan = tdNode.GetAttributeValue("colspan", 1);

                        for (int j = 1; j <= columnspan && colunmIndex < columnsCount;) {
                            if (headerColumnOffsets[colunmIndex] <= trRowIndex) {

                                headerText[colunmIndex] += (!string.IsNullOrEmpty(headerText[colunmIndex]) && !string.IsNullOrEmpty(tdNode.InnerText)
                                    ? $", {tdNode.InnerText}"
                                    : tdNode.InnerText);
                                headerColumnOffsets[colunmIndex] += rowspan;
                                ++j;
                            }

                            ++colunmIndex;
                        }
                    }
                };

                int dataRowsCount = table.Count - headerRowsCount;
                string[,] dataText = new string[dataRowsCount, columnsCount];
                int[] dataColumnOffset = new int[columnsCount];
                for (int trRowIndex = 0; trRowIndex < dataRowsCount; ++trRowIndex) {
                    HtmlNode trNode = table[trRowIndex + headerRowsCount];
                    int currentColumn = 0;


                    foreach (HtmlNode tdNode in trNode.ChildNodes) {
                        int rowspan = tdNode.GetAttributeValue("rowspan", 1);

                        // костыль
                        // учитываем ситуацию, когда на странице rowspan выставили неверно
                        if (rowspan > (dataRowsCount - trRowIndex)) {
                            rowspan = dataRowsCount - trRowIndex;
                        }

                        for (int rowIndex = 0; rowIndex < rowspan;) {
                            if (dataColumnOffset[currentColumn] <= trRowIndex) {
                                dataText[trRowIndex + rowIndex, currentColumn] = tdNode.InnerText;
                                ++rowIndex;
                            } else {
                                ++currentColumn;
                            }
                        }
                        dataColumnOffset[currentColumn] += rowspan;


                        ++currentColumn;
                    }

                }

                // неоптимально
                // stringbuilder здесь смотрелся бы, конечно, лучше

                string output = string.Join(";", headerText) + "\r\n";

                for (int i = 0; i < dataRowsCount; ++i) {
                    string[] tmp = new string[columnsCount];
                    for (int j = 0; j < columnsCount; ++j) {
                        tmp[j] = dataText[i, j];
                    }

                    output += string.Join(";", tmp) + "\r\n";
                }

                return output;
            } catch (Exception) {
                Console.WriteLine("Parsing table error");
                return null;
            }

        }
        
    }
}
