using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ParserAutoFromAvito {
    class Program {

        static List<string> links = new List<string>();

        static void Main(string[] args) {

            while (true) {
                string url = Properties.Settings.Default.Url;
                string content = getRequest(url);
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(content);

                HtmlNodeCollection c = doc.DocumentNode.SelectNodes("//div[@class='description']");

                if (c != null) {

                    Console.WriteLine("");
                    Console.WriteLine(string.Format("******************"));
                    Console.WriteLine(string.Format("Найдено {0} машин", c.Count));
                    foreach (HtmlNode n in c) {

                        if (n.InnerText.Contains("Битый")) {

                            var href = n.ChildNodes["h3"].ChildNodes["a"].Attributes["href"].Value;
                            if (links.Contains(href)) {
                                continue;
                            } else {
                                links.Add(href);
                            }

                            var subject = Regex.Replace(n.ChildNodes["h3"].InnerText, @"\t|\n|\r", String.Empty);

                            Console.WriteLine(string.Format("Найдена Битая машина : {0}", subject));

                            var innerHtml = n.ChildNodes["div"].InnerHtml;
                            innerHtml = Regex.Replace(innerHtml, @"\t|\n|\r", String.Empty);
                            //var innerText = Regex.Replace(innerHtml, "<span class=\"params\">.*<\\/span>", String.Empty);
                            var innerText = String.Empty;
                            innerText = Regex.Replace(innerHtml, @"<.*<\/.*>", String.Empty);
                            innerText = Regex.Replace(innerText, "руб.", String.Empty);
                            innerText = Regex.Replace(innerText, " ", String.Empty);

                            double price = 0;
                            if (string.IsNullOrEmpty(innerText)) {
                                Console.WriteLine(string.Format("У машины : {0} не указана цена", subject));
                                continue;
                            }

                            price = double.Parse(innerText);
                            var maxPrice = Properties.Settings.Default.MaxPrice;

                            if (price > maxPrice) {
                                Console.WriteLine(string.Format("Цена {0} выше чем была задана {1}", price, maxPrice));
                                continue;
                            }

                            Console.WriteLine(string.Format("Цена : {0}", price));

                            string body = string.Format("Ссылка: <a href='https://www.avito.ru{0}'>{1}</a> <br /> Цена: {2}", href, subject, price);

                            Console.WriteLine(string.Format("Отправка на почту"));

                            SendMail(subject + " Цена: " + price, body);

                            Console.WriteLine(string.Format("--------"));
                        }

                    }
                }

                int long_time = Properties.Settings.Default.long_time;
                for (int i = long_time; i >= 0; i--) {
                    Console.WriteLine(string.Format("Ожидание до нового старта {0} сек", i));
                    Thread.Sleep(1000);
                }
            }
        }

        static void SendMail(string _subject, string _body) {
            var fromAddress = Properties.Settings.Default.fromAddress;
            var toAddress = Properties.Settings.Default.toAddress;
            string fromPassword = Properties.Settings.Default.fromPassword;
            string subject = "Нашел: " + _subject;
            string body = _body;

            var toMail = toAddress.Split(';');

            foreach (var mail in toMail) {
                var smtp = new SmtpClient {
                    Host = "smtp.mail.ru",
                    Port = 25,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress, fromPassword)
                };
                using (
                    var message = new MailMessage(fromAddress, mail) {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    }) {
                    try {
                        smtp.Send(message);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
            }           
        }

        static string getRequest(string url) {
            try {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.AllowAutoRedirect = false;//Запрещаем автоматический редирект
                httpWebRequest.Method = "GET"; //Можно не указывать, по умолчанию используется GET.
                httpWebRequest.Referer = "http://google.com"; // Реферер. Тут можно указать любой URL
                using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse()) {
                    using (var stream = httpWebResponse.GetResponseStream()) {
                        using (var reader = new StreamReader(stream, Encoding.GetEncoding(httpWebResponse.CharacterSet))) {
                            return reader.ReadToEnd();
                        }
                    }
                }
            } catch {
                return String.Empty;
            }
        }

    }
}
