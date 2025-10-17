﻿namespace Test.ServerSentEvents
{
    using System;
    using System.Text;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Lite;

    public static class Program
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        static bool _UsingLite = false;
        static string _Hostname = "localhost";
        static int _Port = 8080;
        static WebserverSettings _Settings = null;
        static WebserverBase _Server = null;
        
        static async Task Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0].Equals("lite")) _UsingLite = true;
            }

            _Settings = new WebserverSettings
            {
                Hostname = _Hostname,
                Port = _Port
            };

            if (_UsingLite)
            {
                Console.WriteLine("Initializing webserver lite");
                _Server = new WatsonWebserver.Lite.WebserverLite(_Settings, DefaultRoute);
            }
            else
            {
                Console.WriteLine("Initializing webserver");
                _Server = new Webserver(_Settings, DefaultRoute);
            }

            Console.WriteLine("Listening on " + _Settings.Prefix);
            Console.WriteLine("Use /txt/test.txt");
            _Server.Start();

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        static async Task DefaultRoute(HttpContextBase ctx)
        {
            try
            {
                if (ctx.Request.Url.RawWithoutQuery.Equals("/txt/test.txt"))
                {
                    Console.WriteLine("- User requested /txt/test.txt using " + ctx.Request.ProtocolVersion);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ServerSentEvents = true;

                    long fileSize = new FileInfo("./txt/test.txt").Length;
                    Console.WriteLine("Sending file of size " + fileSize + " bytes");

                    long bytesSent = 0;

                    using (FileStream fs = new FileStream("./txt/test.txt", FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[16];
                        long bytesRemaining = fileSize;

                        int id = 0;

                        while (bytesRemaining > 0)
                        {
                            Thread.Sleep(500);
                            int bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);

                            id++;

                            if (bytesRead > 0)
                            {
                                bytesRemaining -= bytesRead;

                                if (bytesRemaining > 0)
                                {
                                    Console.WriteLine("- Sending event of size " + bytesRead);

                                    if (bytesRead == buffer.Length)
                                    {
                                        await ctx.Response.SendEvent(new ServerSentEvent
                                        {
                                            Id = id.ToString(),
                                            Data = Encoding.UTF8.GetString(buffer)
                                        }, false);
                                    }
                                    else
                                    {
                                        byte[] temp = new byte[bytesRead];
                                        Buffer.BlockCopy(buffer, 0, temp, 0, bytesRead);
                                        await ctx.Response.SendEvent(new ServerSentEvent
                                        {
                                            Id = id.ToString(),
                                            Data = Encoding.UTF8.GetString(temp)
                                        }, false);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("- Sending final chunk of size " + bytesRead);

                                    if (bytesRead == buffer.Length)
                                    {
                                        await ctx.Response.SendEvent(new ServerSentEvent
                                        {
                                            Id = id.ToString(),
                                            Data = Encoding.UTF8.GetString(buffer)
                                        }, true);
                                    }
                                    else
                                    {
                                        byte[] temp = new byte[bytesRead];
                                        Buffer.BlockCopy(buffer, 0, temp, 0, bytesRead);
                                        await ctx.Response.SendEvent(new ServerSentEvent
                                        {
                                            Id = id.ToString(),
                                            Data = Encoding.UTF8.GetString(temp)
                                        }, true);
                                    }
                                }

                                bytesSent += bytesRead;
                            }
                        }
                    }

                    Console.WriteLine("Sent " + bytesSent + " bytes");
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send("Watson says try using GET /txt/test.txt to see what happens!");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}