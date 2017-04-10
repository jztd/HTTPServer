using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using CS422;
namespace CS422
{
	public class WebServer
	{
		private static BlockingCollection<TcpClient> cList = new BlockingCollection<TcpClient>();
		private static List<Thread> threadPool = new List<Thread>();
		private static  ConcurrentBag<CS422.WebService> services = new ConcurrentBag<CS422.WebService>();
		private static int threadPoolSize = 0;
		private static bool serverRunning = true;
		private const int firstLineTimeout = 2048; // bytes
		private const int HeadersTimeout = 102400; // bytes
		public WebServer()
		{
			
		}

		public static bool Start(int port, int numThreads)
		{
			serverRunning = true;
			threadPoolSize = numThreads;

			// build threadpool
			for(int i = 0; i < numThreads; i++)
			{
				threadPool.Add(new Thread(threadDoWork));
				threadPool[i].Start();
			}

			TcpClient client = null;
			TcpListener listener = new TcpListener(IPAddress.Any, port);

			//start the server
			try{
				listener.Start();
			}
			catch(Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			//wait for a client to connect
			while(serverRunning)
			{
				try
				{

					client = listener.AcceptTcpClient();
				}
				catch(Exception e)
				{
					Console.WriteLine(e.ToString());
				}

				// add client to the collection and continue listening
				Console.WriteLine("CLIENT CONNECTED");
				cList.Add(client);
			}

			return true;
		}

		public static void threadDoWork()
		{
			TcpClient client = null;
			bool firstRun = true;
			CS422.WebRequest request = null;
			while(client != null || firstRun == true)
			{
				if(firstRun == true)
				{
					firstRun = false;
				}

				client = cList.Take();
				if(client != null)
				{
					request = WebServer.BuildRequest(client);
					if(request != null)
					{
						// good request and client so just find a handler for it
					
//						for(int i = 0; i < services.Count; i++)
//						{
//							if(request.requestTarget.StartsWith(services[i].ServiceURI))
//							{
//								services[i].Handler(request);
//								break;
//							}
//						}
						foreach(var x in services)
						{
							if(request.requestTarget.StartsWith(x.ServiceURI))
							{
								x.Handler(request);
								break;
							}
						}
						
					}
					else
					{
						Console.WriteLine("bad request, connection closed");
						client.GetStream().Close();
						client.Close();

					}

				}

			}

		}


		public static void AddService(WebService service)
		{
			services.Add(service);

		}
			
		private static CS422.WebRequest BuildRequest(TcpClient client)
		{
			
			MemoryStream data = new MemoryStream();
			StreamReader sr = new StreamReader(data);
			DateTime start = DateTime.Now;
			TimeSpan duration = TimeSpan.FromSeconds(10);

			int currentRead = -1;
			long totalRead = 0;

			byte[] buffer = new byte[1024];

			// flags for peices of the request
			bool goodRequest = false;
			bool goodUrl = false;
			bool goodVersion = false;
			bool goodMethod = false;
			int urlIndex = -1;
			int lengthOfMethod = 0;

			// list of regex rules to match different parts of the request.
			//Regex requestBeginMatch = new Regex("^GET /[^ ]* HTTP/1.1");
			Regex requestEndMatch = new Regex("\r\n\r\n");
			//Regex requestUriMatch = new Regex("/[^ ]* ");

			System.Net.Sockets.NetworkStream stream = client.GetStream();
			stream.ReadTimeout = 2000;

			// peices needed to build the request object
			string method;
			string url;
			string version;
			ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();

			while(currentRead != 0 && totalRead < HeadersTimeout)
			{

				// first thing check to make sure the entire thing hasn't taken too long to read
				if(DateTime.Now - start > duration)
				{
					// took to long, abort

					return null;
				}
				if(totalRead > firstLineTimeout && !goodVersion)
				{
					Console.WriteLine("URL TO LONG");
					return null;
				}
				//read one KB from the client stream
				try{
				currentRead = stream.Read(buffer, 0, 1024);
				}
				catch(IOException ex)
				{
					// took too long to read any data;

					Console.WriteLine("client took too long to respond");
					return null;
				}

				//store the information read from the client in our memory buffer
				data.Write(buffer, 0, currentRead);

				//update how much in total we have received from the client
				totalRead += currentRead;

				// if we have more than 4 bytes check method
				if(totalRead >= 4 && goodMethod == false)
				{
					// bad method
					if(!checkMethod(data, out lengthOfMethod))
					{

						return null;
					}
					else
					{

						goodMethod = true;

					}
				}

				// begin checking for a valid http request after 16 bytes
				if(totalRead >= 16 && goodMethod == true)
				{
					// first check url
					if(goodUrl == false)
					{
						urlIndex = UrlEndIndex(data, 3);
						if(urlIndex > 0)
						{


							// we have a good url
							goodUrl = true;
						}
					} 

					if(goodUrl == true)
					{
						if(checkVersion(data, urlIndex))
						{

							goodVersion = true;
						}
					}

					if(goodVersion)
					{
						data.Position = 0;
						string d = sr.ReadToEnd();
						if(requestEndMatch.IsMatch(d))
						{

							goodRequest = true;
							currentRead = 0;
						}
					}
					else
					{
						if(totalRead > urlIndex + 10 && goodUrl)
						{
							// bad version


							return null;
						}
					}
				}
			}

			if(totalRead+100 >= HeadersTimeout)
			{
				Console.WriteLine("headers were too big");
			}
			if(goodRequest)
			{
				//if we found a good request build and return the web request object (also need to get the header stuff

				// here we need to build the request stuff soooooooooo
				string dataAsString = System.Text.Encoding.ASCII.GetString(data.ToArray());

				method = dataAsString.Substring(0, lengthOfMethod);
				url = dataAsString.Substring(lengthOfMethod,urlIndex - lengthOfMethod);
				version = dataAsString.Substring(urlIndex, 11);
				Console.WriteLine(dataAsString);
				// now the fun begins, we need to read all the headers in to the dictionary
				// where is the begginging on of the headers;
				string[] peices = dataAsString.Split(new string[] {"\r\n"}, StringSplitOptions.None);
				for(int i = 1; i < peices.Length; i++)
				{
					if(peices[i] != "")
					{
						string[] header = peices[i].Split(':');
						headers.TryAdd(header[0].ToLower(), header[1]);
					}
					else
					{
						i = peices.Length + 1;
					}
				}

				// okay now we just need to set the stream to the right place
				data.Position = requestEndMatch.Matches(dataAsString)[0].Index + 4;

				// finally let's build that request object like a boss;
				return new CS422.WebRequest(data,stream,headers,method,url,version, stream);
			} 
			else
			{
				//request was bad close the client return null

				return null;
			}
		}
			
		// checks the first 4 characters for "GET "
		private static bool checkMethod(MemoryStream requestStream, out int len)
		{
			requestStream.Position = 0;
			StreamReader sr = new StreamReader(requestStream);

			string requestString = sr.ReadToEnd();
			// make sure the string is long enough
			if(requestString.Length >= 4)
			{
				//is first 4 characters a proper method
				if(requestString.Substring(0, 4).ToString() == "GET ")
				{
					len = 4;
					return true;
				}
			}
			len = 0;
			return false;
		}

		// assumes a good method and the 0 based index value of the end of the method is passed. returns -1 for bad url or the position of the end space of the url
		private static int UrlEndIndex(MemoryStream requestStream, int endOfMethodIndex)
		{
			requestStream.Position = 0;
			StreamReader sr = new StreamReader(requestStream);

			string requestString = sr.ReadToEnd();

			int index = -1;

			// breaks off the method of the request string then finds the next space character
			index = requestString.Substring(endOfMethodIndex+1, requestString.Length - endOfMethodIndex - 1).IndexOf(" ");
			if(index > 0)
			{
				return index + endOfMethodIndex + 1;
			} else
			{
				return index;
			}
		}

		// assumes a good method and a good url. starts at the index value of the end of the url and checks the next 10 chars for "HTTP/1.1\r\n"
		private static bool checkVersion(MemoryStream requestStream, int endOfUrlIndex)
		{
			requestStream.Position = 0;
			StreamReader sr = new StreamReader(requestStream);

			string requestString = sr.ReadToEnd();

			// first check to see if the string is long enough
			if(requestString.Length >= endOfUrlIndex + 11)
			{
				// check the version substring
				if(requestString.Substring(endOfUrlIndex + 1, 10) == "HTTP/1.1\r\n")
				{
					return true;
				}
			}

			return false;
		}

		public static void Stop()
		{
			serverRunning = false;
			for(int i = 0; i < threadPoolSize; i++)
			{
				cList.Add(null);
			}
			foreach(var T in threadPool)
			{
				T.Join();
			}
		}


	}
}

