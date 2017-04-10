using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CS422;

namespace CS422
{
	public class WebRequest
	{
		private ConcatStream _body;
		private ConcurrentDictionary<string,string> _headers;
		private string _method;
		private string _requestTarget;
		private string _httpVersion;
		private System.Net.Sockets.NetworkStream _response;

		/// <summary>
		/// Creates a new isntance of a WebRequest object
		/// parameters: 
		/// 			front: is a seekable stream to represent the first part of the request body with it position set to be just after the double line break
		/// 			back: is another stream that contains the rest of the body
		/// 			headers: is a concurent dictionary of normalized headers from the http request
		/// 			method: is a string representing the method from the HTTP Request (GET, POST, DELETE ect...)
		/// 			requestTarget: is a the desired uri from the HTTP Request
		/// 			httpVersion: is the version from the HTTP Request
		/// 			
		/// </summary>

		public WebRequest(Stream front, Stream back, ConcurrentDictionary<string, string> headers , string method, string requestTarget, string httpVersion, System.Net.Sockets.NetworkStream nStream)
		{
			if(headers.ContainsKey("content-length"))
			{
				long length = Convert.ToInt64(headers["content-length"]);
				_body = new ConcatStream(front, back, length);
			}
			else
			{
				_body = new ConcatStream(front, back);
			}

			_headers = headers;
			_method = method;
			_requestTarget = requestTarget;
			_httpVersion = httpVersion;
			_response = nStream;
		}

		public ConcatStream body
		{
			get{
				return _body;
			}
		}

		public string method
		{
			get
			{
				return _method;
			}
		}

		public string requestTarget
		{
			get
			{
				return _requestTarget;
			}
		}

		public string httpVersion
		{
			get
			{
				return _httpVersion;
			}
		}

		/// <summary>
		/// 
		/// returns the value for specified header value
		/// returns null if the value does not exist
		/// </summary>
		///
		/// 

		public string getHeader(string header)
		{
			if(_headers.ContainsKey(header.ToLower()))
			{
				return _headers[header.ToLower()];
			}
			else
			{
				return null;
			}
		}

		public void WriteNotFoundResponse(string pageHTML)
		{
			String clen = pageHTML.Length.ToString();
			String response = "HTTP/1.1 404 Not Found\r\nContent-Type:text/html\r\nContent-Length:"+clen+"\r\n\r\n" + pageHTML;

			_response.Write(Encoding.ASCII.GetBytes(response), 0, response.Length);

		}

		public void WriteHTMLResponse(string htmlString)
		{
			String clen = htmlString.Length.ToString();
			String response = "HTTP/1.1 200 OK\r\nContent-Type:text/html\r\nContent-Length:"+clen+"\r\n\r\n" + htmlString;

			_response.Write(Encoding.ASCII.GetBytes(response), 0, response.Length);

		}
		public void WriteFileResponse(byte[] data)
		{
			


			byte[] buffer = new byte[4096];
			try
			{
				_response.Write(data, 0, data.Length);
//				_response.Read(buffer, 0, buffer.Length);
//				Console.WriteLine("RESPONSE FROM CLIENT");
//				Console.WriteLine(Encoding.ASCII.GetString(buffer));
			}
			catch
			{
			}
		}
	}
}

