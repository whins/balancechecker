using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BalanceChecker
{
    public class HttpProcessor
    {
        public TcpClient Socket;
        public HttpServer Srv;

        private Stream _inputStream;
        public StreamWriter OutputStream;

        public string HttpMethod;
        public string HttpUrl;
        public string HttpProtocolVersionString;

        public Hashtable HttpHeaders = new Hashtable();

        private const int MaxPostSize = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            Socket = s;
            this.Srv = srv;
        }

        private string StreamReadLine(Stream inputStream)
        {
            var data = "";
            while (true)
            {
                var nextChar = inputStream.ReadByte();
                if (nextChar != '\n')
                {
                    switch (nextChar)
                    {
                        case '\r':
                            continue;
                        case -1:
                            Thread.Sleep(1);
                            continue;
                    }
                    data += Convert.ToChar(nextChar);
                }
                else
                {
                    break;
                }
            }
            return data;
        }

        public void Process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            _inputStream = new BufferedStream(Socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            OutputStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
            try
            {
                ParseRequest();
                ReadHeaders();
                if (HttpMethod.Equals("GET"))
                {
                    HandleGetRequest();
                }
                else if (HttpMethod.Equals("POST"))
                {
                    HandlePostRequest();
                }
            }
            catch (Exception)
            {
                WriteFailure();
            }
            OutputStream.Flush();
            _inputStream = null; OutputStream = null;
            Socket.Close();
        }

        public void ParseRequest()
        {
            var request = StreamReadLine(_inputStream);
            var tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            HttpMethod = tokens[0].ToUpper();
            HttpUrl = tokens[1];
            HttpProtocolVersionString = tokens[2];

            Console.WriteLine("starting: " + request);
        }

        public void ReadHeaders()
        {
            Console.WriteLine("readHeaders()");
            String line;
            while ((line = StreamReadLine(_inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("got headers");
                    return;
                }

                var separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                var name = line.Substring(0, separator);
                var pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                var value = line.Substring(pos, line.Length - pos);
                Console.WriteLine("header: {0}:{1}", name, value);
                HttpHeaders[name] = value;
            }
        }

        public void HandleGetRequest()
        {
            Srv.HandleGetRequest(this);
        }

        private const int BufSize = 4096;

        public void HandlePostRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream
            // we hand him needs to let him see the "end of the stream" at this content
            // length, because otherwise he won't know when he's seen it all!

            var memoryStream = new MemoryStream();
            if (HttpHeaders.ContainsKey("Content-Length"))
            {
                var contentLen = Convert.ToInt32(HttpHeaders["Content-Length"]);
                if (contentLen > MaxPostSize)
                {
                    throw new Exception(
                        $"POST Content-Length({contentLen}) too big for this simple server");
                }
                var buf = new byte[BufSize];
                var toRead = contentLen;
                while (toRead > 0)
                {
                    Console.WriteLine("starting Read, to_read={0}", toRead);

                    var numread = _inputStream.Read(buf, 0, Math.Min(BufSize, toRead));
                    Console.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (toRead == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    toRead -= numread;
                    memoryStream.Write(buf, 0, numread);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            Srv.HandlePostRequest(this, new StreamReader(memoryStream));
        }

        public void WriteSuccess(string contentType = "text/html")
        {
            OutputStream.WriteLine("HTTP/1.0 200 OK");
            OutputStream.WriteLine("Content-Type: " + contentType);
            OutputStream.WriteLine("Connection: close");
            OutputStream.WriteLine("");
        }

        public void WriteFailure()
        {
            OutputStream.WriteLine("HTTP/1.0 404 File not found");
            OutputStream.WriteLine("Connection: close");
            OutputStream.WriteLine("");
        }

        internal void Show(string uri)
        {
            using (var sr = File.OpenText(uri))
            {
                WriteSuccess();
                OutputStream.Write(sr.ReadToEnd());
            }
        }
    }

    public abstract class HttpServer
    {
        protected int Port;
        private TcpListener _listener;
        private readonly bool _isActive = true;

        public delegate void GetRequestHandler(HttpProcessor p);

        public event GetRequestHandler OnGetRequest;

        public delegate void PostRequestHandler(HttpProcessor p, StreamReader inputData);

        public event PostRequestHandler OnPostRequest;

        public delegate void HttpServerStartedHandler(TcpListener listener);

        public event HttpServerStartedHandler OnHttpServerStarted;

        protected HttpServer(int port)
        {
            this.Port = port;
        }

        public void Listen()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();

            OnHttpServerStarted?.Invoke(_listener);

            while (_isActive)
            {
                var s = _listener.AcceptTcpClient();
                var processor = new HttpProcessor(s, this);
                var thread = new Thread(new ThreadStart(processor.Process));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public virtual void HandleGetRequest(HttpProcessor p)
        {
            OnGetRequest?.Invoke(p);
        }

        public virtual void HandlePostRequest(HttpProcessor p, StreamReader inputData)
        {
            OnPostRequest?.Invoke(p, inputData);
        }
    }
}