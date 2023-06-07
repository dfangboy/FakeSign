﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using LibTimeStamp;

namespace FakeStamping
{
    class Program
    {
        static TSResponder tsResponder;
        static readonly string listen_path = @"/TSA/";
        static readonly string listen_addr = @"localhost";
        static readonly string listen_port = @"1003";
        static readonly string server_full = @"http://time.pika.net.cn/fake/RSA/";
        static readonly string server_cert = @"TSA.crt";
        static readonly string server_keys = @"TSA.key";
        static readonly string supportFake = @"true";
        static void Main(string[] args)
        {
            PrintDesc();
            try
            {
                tsResponder = new TSResponder(File.ReadAllBytes((string)server_cert),
                                              File.ReadAllBytes((string)server_keys), "SHA1");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   [Error!!] Can NOT Find TimeStamp Cert File");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("   [Warning] Please Check Your Cert and Key!");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            HttpListener listener = new HttpListener();
            try
            {
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                listener.Prefixes.Add(@"http://"+ listen_addr + ":"+ listen_port + listen_path);
                listener.Start();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   [Error!!] TimeStamp Responder Can NOT Listen Port: " + listen_port);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("   [Warning] Please Run as Administrator and Check Port!");
                Console.ForegroundColor = ConsoleColor.White;
                //Console.ReadLine();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [Success] Your TimeStamp HTTP Server Started Successfully!");
            Console.WriteLine("   [Success] TimeStamp Responder: http://" + listen_addr + ":"+ listen_port + listen_path);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskProc), ctx);
            }
        }

        static void PrintDesc()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Title = "Pikachu TimeStamp Responder";
            Console.WriteLine(
                "==============================================================\r\n" +
                "|                                                            |\r\n" +
                "|           Pikachu RFC3161 Time Stamping Responder          |\r\n" +
                "|                 Last Updated: Feb 11 2023                  |\r\n" +
                "|                                                            |\r\n" +
                "|------------------------------------------------------------|\r\n" +
                "|  Notice: This Responder Should Run in Administrator Mode!! |\r\n" +
                "|  Server Accept UTC Time in the Form of yyyy-MM-ddTHH:mm:ss |\r\n" +
                "|  For Example: http://your_ip:port/path/2020-01-01T00:00:00 |\r\n" +
                "==============================================================\r\n"
                );
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("   [Message] Server Address: " + @"http://" + listen_addr + ":" + listen_port + listen_path);
            Console.WriteLine("   [Message] TimeStamp Cert File Name: " + server_cert);
            Console.WriteLine("   [Message] TimeStamp Keys File Name: " + server_keys);
        }
        static void TaskProc(object o)
        {
            HttpListenerContext ctx = (HttpListenerContext)o;
            ctx.Response.StatusCode = 200;
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            string usage_time;
            if (supportFake == "true")
                usage_time = "2020-01-01T00:00:00";
            else
                usage_time = "";
            if (ctx.Request.HttpMethod != "POST")
            {
                StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.ASCII);
                
                writer.WriteLine("<h1>Pikachu RFC3161 Time Stamping Responder</h1>" +
                                 "<h2>Server Details</h2>" +
                                 "ServerURL: " + server_full +
                                 "<br/>FakeTimer: " + supportFake +
                                 "<h2>Signing Usage</h2>" +
                                 "<h3>Microsoft SignTool</h3>" +
                                 "<code>signtool.exe sign /v /f \"your_cert.pfx\" /p \"pfx password\" /t "+ server_full + usage_time + " \"unsign file\" </code>" +
                                 "<h3>TrustAsia SignTool</h3>" +
                                 "<blockquote><ul>" +
                                 "<li><h5><span>Pikachu Fake CA TS: </span><a href='https://github.com/PIKACHUIM/FakeSign/raw/main/SignTool/Released/HookSigntool-PikaFakeTimers.zip'>" +
                                 "<span>TrustAsia SignTool - PikaFakeTimers</span></a></h5></li>" +
                                 "<li><h5><span>Pikachu Root CA TS: </span><a href='https://github.com/PIKACHUIM/FakeSign/raw/main/SignTool/Released/HookSigntool-PikaRealTimers.zip'>" +
                                 "<span>TrustAsia SignTool - PikaRealTimers</span></a></h5></li>" +
                                 "<li><h5><span>JemmyLoveJenny: </span><a href='https://github.com/PIKACHUIM/FakeSign/raw/main/SignTool/Released/HookSigntool-JemmyLoveJenny.zip'>" +
                                 "<span>TrustAsia SignTool - JemmyLoveJenny</span></a></h5></li>" +
                                 "</ul></blockquote></li></ul>"
                    );
                writer.Close();
                ctx.Response.Close();
            }
            else
            {
                string log = "";
                string date = request.RawUrl.Remove(0, listen_path.Length);
                DateTime signTime;
                signTime = DateTime.UtcNow;
                if (supportFake == "true")
                {
                    Console.WriteLine("   [Success] Fake Stamp Responder: " + supportFake);
                    if (!DateTime.TryParseExact(date, "yyyy-MM-dd'T'HH:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                                out signTime))
                    {
                        signTime = DateTime.UtcNow;
                        Console.WriteLine("   [Warning] Can Not Process Time: " + date);
                    }
                    else
                    {
                        Console.WriteLine("   [Success] Fake Stamp Responder: " + date);
                    }
                }
                else
                {
                    Console.WriteLine("   [Success] Real Stamp Responder!");
                }
                BinaryReader reader = new BinaryReader(request.InputStream);
                byte[] bRequest = reader.ReadBytes((int)request.ContentLength64);

                bool RFC;
                byte[] bResponse = tsResponder.GenResponse(bRequest, signTime, out RFC);
                if (RFC)
                {
                    response.ContentType = "application/timestamp-reply";
                    log += "   [Success] RFC3161 Time Stamping ";
                }
                else
                {
                    response.ContentType = "application/octet-stream";
                    log += "   [Success] Authenticode Stamping ";
                }
                log += signTime;
                BinaryWriter writer = new BinaryWriter(response.OutputStream);
                writer.Write(bResponse);
                writer.Close();
                ctx.Response.Close();
                Console.WriteLine(log);
            }
        }
    }
}
