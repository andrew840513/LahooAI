using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LahooAI
{
    class Program
    {
        // 设置APPID/AK/SK
        static string APP_ID = "";
        static string API_KEY = "";//

        static string SECRET_KEY = "";//
        static string FilePath = AppDomain.CurrentDomain.BaseDirectory + "Images\\";
        static string OutFilePath = AppDomain.CurrentDomain.BaseDirectory + "OutImages\\";
        static string MultiOutFilePath = AppDomain.CurrentDomain.BaseDirectory + "MultiImages\\";
        static string NoFilePath = AppDomain.CurrentDomain.BaseDirectory + "NoImages\\";
        static string OldFilePath = AppDomain.CurrentDomain.BaseDirectory + "OldImages\\";
        static void Main(string[] args)
        {
            var files = Directory.GetFiles(FilePath);
            Console.WriteLine("图片数量:" + files);
            var logoImage = File.ReadAllBytes("logo.png");
            foreach (string imagePath in files)
            {
                var imageNameArray = imagePath.Split('\\');
                var imageName = imageNameArray[imageNameArray.Length - 1];
                if (CheckIsProcessed(imageName))
                {
                    var image = File.ReadAllBytes(imagePath);
                    //var image = File.ReadAllBytes(FilePath + "WeChat Image_20210323183000.png");
                    int error_code = GeneralDemo(image, logoImage, imageName);
                    InsertProcessedImage(imageName, error_code);
                }
                Thread.Sleep(100);
            }
        }

        public static int GeneralDemo(byte[] image, byte[] logoImage, string imageName)
        {
            try
            {
                var error_code = 0;
                var client = new Baidu.Aip.Ocr.Ocr(API_KEY, SECRET_KEY)
                {
                    Timeout = 60000  // 修改超时时间
                };

                // 调用通用文字识别（含位置信息版）, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
                // 如果有可选参数
                var options = new Dictionary<string, object>{
        {"recognize_granularity", "big"},
        {"language_type", "CHN_ENG"},
        {"detect_direction", "false"},
        {"detect_language", "false"},
        {"vertexes_location", "false"},
        {"probability", "false"}
    };
                // 带参数调用通用文字识别（含位置信息版）, 图片参数为本地图片
                //var result = client.General(image, null);//, options);
                var result = client.Accurate(image, options);
                //RecResult rr = JsonConvert.DeserializeObject<RecResult>(result.ToString());
                
                var res = result.ToObject<RecResult>();
                Console.WriteLine(result);
                var hasWaterMark = false;
                if (res.words_result == null)
                {
                    Console.WriteLine("API eorror");
                    return (int)ErrorCode.API_error;
                }
                var multiWordCountCHN = 0;
                var multiWordCountENG = 0;
                foreach (WordResult wordResult in res.words_result)
                {
                    if (Levenshtein(wordResult.Words, "人在温哥华") > 0.35)
                        multiWordCountCHN++;
                    if (Levenshtein(wordResult.Words, "VanPeople com") > 0.5)
                        multiWordCountENG++;
                }
                if (multiWordCountCHN >= 2 || multiWordCountENG >= 2)
                {
                    error_code = (int)ErrorCode.multi_result;
                }
                foreach (WordResult wordResult in res.words_result)
                {
                    var similarity = Levenshtein(wordResult.Words, "人在温哥华");
                    if (similarity >= 0.35)
                    {
                        //wordResult.location.Width = wordResult.location.Width * (5 / wordResult.Words.Length);
                        //var diffCharNumber = CharDiff("人在温哥华", wordResult.Words);
                        //wordResult.location.Left = wordResult.location.Left - (wordResult.location.Width * diffCharNumber / 5);
                        //var imageLb = wordResult.location.Width * 164 / 134; //覆盖图长度
                        var Hb = wordResult.location.Height * 50 / 35 * 120 / 100; //覆盖图宽度
                        var Lb = Hb * 728 / 221 * 120 / 100; //覆盖图长度
                        //var iamgeLeft = wordResult.location.Left - (Lb - imageLb);
                        Image templeImage = Image.Load(image);
                        Image logoImg = Image.Load(logoImage);
                        logoImg.Mutate(x => x.Resize(Lb, Hb)); //设置覆盖图尺寸
                        var left = wordResult.location.Left - (Lb - wordResult.location.Width); //计算覆盖X位置
                        var outImage = MergeImage(templeImage, logoImg, left, wordResult.location.Top);
                        if (error_code == 1)
                            outImage.Save(MultiOutFilePath + imageName);
                        else
                            outImage.Save(OutFilePath + imageName);
                        hasWaterMark = true;
                        break;
                    }
                    similarity = Levenshtein(wordResult.Words.Trim(), "VanPeople com");
                    if (similarity > 0.5 && hasWaterMark == false)
                    {
                        var Hb = wordResult.location.Height * 50 / 35 * 2 * 120 / 100; //覆盖图宽度
                        var Lb = Hb * 728 / 221 * 120 / 100; //覆盖图长度
                        Image templeImage = Image.Load(image);
                        Image logoImg = Image.Load(logoImage);
                        logoImg.Mutate(x => x.Resize(Lb, Hb)); //设置覆盖图尺寸
                        var left = wordResult.location.Left - (Lb - wordResult.location.Width); //计算覆盖X位置
                        var top = wordResult.location.Top - wordResult.location.Height * 2;
                        var outImage = MergeImage(templeImage, logoImg, left, top);
                        if (error_code == 1)
                            outImage.Save(MultiOutFilePath + imageName);
                        else
                            outImage.Save(OutFilePath + imageName);
                        hasWaterMark = true;
                        break;
                    }                    
                }
                if (hasWaterMark == false)
                {
                    foreach (WordResult wordResult in res.words_result)
                    {
                        var similarity1 = Levenshtein(wordResult.Words.Trim(), "http://wwww.vanpeople.com/c/viewinfo.aspx?id=226658");
                        var similarity2 = Levenshtein(wordResult.Words.Trim(), "http://www.vanpeople.com/c/42926.html");
                        if (similarity2 > similarity1)
                            similarity1 = similarity2;
                        if (similarity1 > 0.3 && hasWaterMark == false)
                        {
                            Image templeImage = Image.Load(image);
                            float diffW = (float)(templeImage.Height - wordResult.location.Top) / templeImage.Height;
                            if (diffW < 0.25)
                            {
                                var Hb = wordResult.location.Height * 50 / 35 * 2 * 120 / 100; //覆盖图宽度
                                var Lb = Hb * 728 / 221 * 120 / 100; //覆盖图长度

                                templeImage.Mutate(x => x.Crop(new Rectangle(0, 0, templeImage.Width, wordResult.location.Top)));
                                templeImage.Save(OldFilePath + imageName);
                                hasWaterMark = true;
                                break;
                            }
                        }
                    }
                }
                if (hasWaterMark == false)
                {
                    Image orgImage = Image.Load(image);
                    orgImage.Save(NoFilePath + imageName);
                    error_code = (int)ErrorCode.no_result;
                }
                return error_code;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return (int)ErrorCode.unhandle_error;
            }

        }

        public static int CharDiff(string templeString, string resultString)
        {
            for (int i = 1; i <= resultString.Length; i++)
                for (int j = 1; j <= templeString.Length; j++)
                {
                    var tempChar = templeString.Substring(j - 1, 1);
                    var resultChar = resultString.Substring(i - 1, 1);
                    if (tempChar == resultChar)
                    {
                        Console.WriteLine("location:" + j);
                        Console.WriteLine("char:" + resultChar);
                        Console.WriteLine("diff:" + (j - i));
                        return j - i;
                    }
                }
            return 5;
        }

        /// <summary>
        /// compute similarity 
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static float Levenshtein(string str1, string str2)
        {
            //计算两个字符串的长度。 
            int len1 = str1.Length;
            int len2 = str2.Length;
            //建立上面说的数组，比字符长度大一个空间 
            int[,] dif = new int[len1 + 1, len2 + 1];
            //赋初值，步骤B。 
            for (int a = 0; a <= len1; a++)
            {
                dif[a, 0] = a;
            }
            for (int a = 0; a <= len2; a++)
            {
                dif[0, a] = a;
            }
            //计算两个字符是否一样，计算左上的值 
            int temp;
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = 1;
                    }
                    //取三个值中最小的 
                    dif[i, j] = Math.Min(Math.Min(dif[i - 1, j - 1] + temp, dif[i, j - 1] + 1), dif[i - 1, j] + 1);
                }
            }
            Console.WriteLine("字符串\"" + str1 + "\"与\"" + str2 + "\"的比较");

            //取数组右下角的值，同样不同位置代表不同字符串的比较 
            Console.WriteLine("差异步骤：" + dif[len1, len2]);
            //计算相似度 
            float similarity = 1 - (float)dif[len1, len2] / Math.Max(str1.Length, str2.Length);
            Console.WriteLine("相似度：" + similarity);
            return similarity;
        }

        /// <summary>
        /// 合并图片
        /// </summary>
        /// <param name="templateImage"></param>
        /// <param name="mergeImagePath">合并图片</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">y坐标</param>
        /// <returns></returns>
        public static Image MergeImage(Image templateImage, Image mergeImage, int x, int y)
        {
            templateImage.Mutate(o =>
            {
                o.DrawImage(mergeImage, new Point(x, y), 1);
            });

            return templateImage;
        }

        /// <summary>
        /// 查找图片是否被处理过
        /// </summary>
        /// <returns>sring</returns>
        public static bool CheckIsProcessed(string imageName)
        {
            // 实例化连接对象
            DecryptAndEncryptionHelper decryptAndEncryptionHelper = new DecryptAndEncryptionHelper("27e167e9-2660-4bc1-bea0-c8781a9wer34", "8280d587-f9bf-4127-bbfa-5e0b4b643243");
            MysqlConnector mc = new MysqlConnector();
            // 设置数据库连接参数
            mc.SetServer(Config.DatabaseServer)
                  .SetDataBase(Config.Database)
                  .SetUserID(Config.UserID)
                  .SetPassword(decryptAndEncryptionHelper.Decrypto(Config.Password))
                  .SetPort(Config.Port)
                  .SetCharset(Config.Charset);
            string sql_select_image_process = string.Format(
                                        "SELECT * FROM image_process where image_name = '{0}'",
                                        imageName);
            Console.WriteLine("check isCrawledSQL: " + sql_select_image_process);
            var resultReader = mc.ExeQuery(sql_select_image_process);
            if (resultReader.HasRows)
            {
                resultReader.Close();
                return false;
            }
            resultReader.Close();
            return true;
        }

        /// <summary>
        /// 写入被处理过的图片
        /// </summary>
        /// <returns>sring</returns>
        public static int InsertProcessedImage(string imageName, int error_code)
        {
            // 实例化连接对象
            DecryptAndEncryptionHelper decryptAndEncryptionHelper = new DecryptAndEncryptionHelper("27e167e9-2660-4bc1-bea0-c8781a9wer34", "8280d587-f9bf-4127-bbfa-5e0b4b643243");
            MysqlConnector mc = new MysqlConnector();
            // 设置数据库连接参数
            mc.SetServer(Config.DatabaseServer)
                  .SetDataBase(Config.Database)
                  .SetUserID(Config.UserID)
                  .SetPassword(decryptAndEncryptionHelper.Decrypto(Config.Password))
                  .SetPort(Config.Port)
                  .SetCharset(Config.Charset);
            string sql_image_process = string.Format(
                                        "insert into image_process (image_name, date, error_code) values ('{0}', '{1}', {2})",
                                        imageName,
                                        DateTime.Now,
                                        error_code);
            Console.WriteLine("check isCrawledSQL: " + sql_image_process);
            var result = mc.ExeUpdate(sql_image_process);
            return result;
        }

        public enum ErrorCode
        {
            correct,
            multi_result,
            no_result,
            unhandle_error,
            old_result,
            API_error
        }
    }
}
