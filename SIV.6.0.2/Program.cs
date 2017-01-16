using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace SIV._6._0._2
{
    class Program
    {
        #region Main
        static void Main(string[] args)
        {
            try
            {
                System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader("SIV.6.0.2.config");
                XmlDocument xDL = new XmlDocument();
                xDL.Load("SIV.6.0.2.config"); //Load XML檔
                XmlNodeList NodeList = xDL.SelectNodes("//RULE"); //取得RULE區段
                XmlNodeList NodeListLog = xDL.SelectNodes("//LOG_PATH"); //取得LOG路徑
                String LOG_PATH = NodeListLog[0].InnerText;
                XmlNodeList NodeListPROCDATE = xDL.SelectNodes("//PROC_DATE"); //取得執行日期
                DateTime PROCDATE = DateTime.Now;
                if (NodeListPROCDATE[0].InnerText != "")
                {
                    PROCDATE = Convert.ToDateTime(NodeListPROCDATE[0].InnerText);
                }

                WriteLog("程式開始", LOG_PATH);

                Dictionary<string, string> keyValues = new Dictionary<string, string>();
                for (int i = 0; i < NodeList.Count; i++)
                {
                    int _SortType = 0;  // 0:由小到大排序 1:由大到小排序
                    int _FileLimit = 0;

                    for (int j = 0; j < NodeList[i].ChildNodes.Count; j++)
                    {
                        string key = NodeList[i].ChildNodes[j].LocalName;
                        if (key == "FILE_LIMIT")
                            _FileLimit = 0 - Convert.ToInt16(NodeList[i].ChildNodes[j].InnerText);

                        if (key == "SortType")
                            _SortType = Convert.ToInt16(NodeList[i].ChildNodes[j].InnerText);

                        if (key == "PATHS")
                        {
                            for (int k = 0; k < NodeList[i].ChildNodes[j].ChildNodes.Count; k++)
                            {
                                string PATHkey = NodeList[i].ChildNodes[j].ChildNodes[k].LocalName;

                                //取得確認路徑
                                if (PATHkey == "PATH")
                                {
                                    //string FolderPath = NodeList[i].ChildNodes[j].ChildNodes[k].InnerText;
                                    string FilePath = NodeList[i].ChildNodes[j].ChildNodes[k].InnerText;
                                    //if (File.Exists(FilePath))
                                    //{
                                    //FileInfo fi = new FileInfo(FilePath);

                                    DirectoryInfo info = new DirectoryInfo(FilePath);
                                    //FileInfo[] files;    //--2017/01/16
                                    FileSystemInfo[] files = info.GetFileSystemInfos();
                                    if (_SortType == 1)
                                    {
                                        //files = info.GetFiles().OrderByDescending(p => p.LastWriteTime).ToArray();    //--2017/01/16
                                       
                                        Array.Sort<FileSystemInfo>(files, delegate(FileSystemInfo a, FileSystemInfo b)
                                        {
                                            return b.LastWriteTime.CompareTo(a.LastWriteTime);
                                        });
                                    }
                                    else
                                    {
                                        //files = info.GetFiles().OrderBy(p => p.LastWriteTime).ToArray();     //--2017/01/16
                                        Array.Sort<FileSystemInfo>(files, delegate(FileSystemInfo a, FileSystemInfo b)
                                        {
                                            return a.LastWriteTime.CompareTo(b.LastWriteTime);
                                        });
                                    }

                                    foreach (FileInfo fi in files)
                                    {
                                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                                            fi.Attributes = FileAttributes.Normal;
                                        //判斷小於設定分鐘數
                                        if (fi.LastWriteTime < PROCDATE.AddMinutes(_FileLimit))
                                        {
                                            Console.WriteLine(FilePath);
                                            WriteLog(String.Format("逾時檔案:{0}", fi.FullName), LOG_PATH);
                                            CallMoniMessage("逾時檔案:" + FilePath);
                                        }
                                        break;
                                    }


                                    //}

                                }//end if (PATHkey == "PATH")

                            }//end for 
                        }//end if (key == "PATHS")
                    }//end for
                }//end for

                WriteLog("程式結束", LOG_PATH);


            }
            catch (System.Exception ex)
            {
                WriteLog("執行錯誤:" + ex.Message.ToString(), "");
                CallMoniMessage(ex.Message.ToString());
            }


            Console.WriteLine("已完成");
        }
        #endregion Main

        /// <summary>
        /// 取得階層
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static int CountWord(string str1, string str2)
        {
            int startFlag = -2;
            int counter = 0;
            while (startFlag != -1)
            {
                startFlag = (startFlag == -2) ? -1 : startFlag;
                startFlag = str1.IndexOf(str2, startFlag + 1);
                counter = (startFlag == -1) ? counter : counter + 1;
            }
            return counter;
        }

        #region Log
        /// <summary>
        /// 寫Log
        /// </summary>
        /// <param name="message"></param>
        private static void WriteLog(string message, string LOG_PATH)
        {
            lock (typeof(int))
            {   //取得LOGPATH

                if (LOG_PATH == "")
                {
                    LOG_PATH = "LOG";
                }
                string fxLogPath = LOG_PATH;
                string logPath = Path.Combine(fxLogPath, string.Format("SIV.6.0.2_{0}.log", DateTime.Now.ToString("yyyyMMdd")));

                if (!Directory.Exists(fxLogPath))
                {
                    Directory.CreateDirectory(fxLogPath);
                }

                using (StreamWriter sw = new StreamWriter(logPath, true))
                {
                    sw.WriteLine(string.Format("{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), message));
                    sw.Close();
                }
            }
        }


        #endregion Log

        #region Moni
        private static void CallMoniMessage(String Message)
        {

            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader("SIV.6.0.2.config");
            XmlDocument xDL = new XmlDocument();
            xDL.Load("SIV.6.0.2.config"); //Load XML檔
            XmlNodeList NodeList = xDL.SelectNodes("//MONI_ID"); //MONI_ID
            XmlNodeList NodeListIS = xDL.SelectNodes("//IS_MONI"); //IS_MONI
            string ISMoni = NodeListIS[0].InnerText; //是否呼叫監控 Y=是
            string moniURL = "http://126.1.101.30:8080/CON03/moniMsg";
            string moniHostIP = "126.1.101.30";
            string MoniName = NodeList[0].InnerText;
            string URL;
            URL = string.Format("{0}?HOST={1}&ACC=tfm&PROGNAME={2}&EXECSTATUS=-s&PROGID={3}&ENDSTATUS=&ERRORDESC=", moniURL, moniHostIP, MoniName, Message);
            CallMoni(ISMoni, URL);//開始呼叫
        }



        private static void CallMoni(string ISMoni, string URL)
        {
            if (ISMoni == "Y")
            {
                System.Net.WebRequest request = System.Net.HttpWebRequest.Create(URL);
                System.Net.WebResponse response = request.GetResponse();
                System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream());
                string urlText = reader.ReadToEnd();
            }
        }

        #endregion
    }
}
