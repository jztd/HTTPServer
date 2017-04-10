using System;
using System.Text;
using System.IO;
using CS422;
namespace CS422
{
	public class FileWebService: WebService
	{
		private readonly FileSys422 _fs;
		public FileWebService(FileSys422 fs)
		{
			_fs = fs;
		}

		public override void Handler(WebRequest req)
		{
			if (!req.requestTarget.StartsWith("/files"))
			{
				throw new Exception();
			}

			if (req.requestTarget == "/files" || req.requestTarget == "files" || req.requestTarget == "/files/")
			{
				RespondWithList(_fs.GetRoot(), req);
				return;
			}

			var dir = _fs.GetRoot();
			string newRequestTarget = System.Uri.UnescapeDataString(req.requestTarget.Substring(ServiceURI.Length));
			string[] uriPieces = newRequestTarget.Split(new char[1] {'/'}, StringSplitOptions.RemoveEmptyEntries);

			if (null == uriPieces || uriPieces.Length == 0)
			{
				req.WriteNotFoundResponse("NOT FOUND");
			return;
			}
			for (int i = 0; i < uriPieces.Length - 1; i++)
			{
				string uriPiece = uriPieces[i];
				dir = dir.GetDir(uriPiece);
				if (null == dir)
				{
					req.WriteNotFoundResponse("NOT FOUND");
					return;
				}
			}		

			var file = dir.GetFile(uriPieces[uriPieces.Length - 1]);

			if (null != file)
			{
				RespondWithFile(file, req);
				return;
			}
			dir = dir.GetDir(uriPieces[uriPieces.Length - 1]);
			if (dir == null)
			{
				req.WriteNotFoundResponse("NOT FOUND");
				return;
			}

			RespondWithList(dir, req);
		}


		public override string ServiceURI
		{
			get
			{
				return "/files";
			}
		}

		private void RespondWithList(Dir422 dir, WebRequest req)
		{
			var html = new System.Text.StringBuilder("<html>");

			var files = dir.GetFiles();
			var dirs = dir.GetDirs();
			string dirPath = "";

			Dir422 temp = dir;
			dirPath = "/files";

			string tempPath = "";
			while (temp.Parent != null)
			{
				tempPath = "/" + temp.Name + tempPath;
				temp = temp.Parent;
			}
			dirPath = dirPath + tempPath + "/";



			html.Append("<h1> Folders</h1>");

			foreach(var d in dirs)
			{
				string href = dirPath + d.Name;
				html.AppendFormat("<a href=\"{0}\">{1}</a><br />",href,d.Name);
			}

			html.Append("<h1> Files</h1>");

			foreach (var f in files)
			{
				// makesure dirPath has only one / at the end
				// TODO
				string href = dirPath + f.Name;
				html.AppendFormat("<a href=\"{0}\">{1}</a><br />",href,f.Name);
			}

			html.Append("</HTML>");

			req.WriteHTMLResponse(html.ToString());

			Console.WriteLine("SENT LIST");
			return;
		}

		private void RespondWithFile(File422 file, WebRequest req)
		{
			StringBuilder resp = new StringBuilder();
			string ext = Path.GetExtension(file.Name).ToLower();
			var fileStream = file.OpenReadOnly();
			int bytesRead = -1;
			byte[] buffer = new byte[8192];
			string contentType = "text/plain";
			string statusCode = "200 OK";
			long rangeBegin = 0;
			long rangeEnd = (fileStream.Length - 1);
			long contentLength = fileStream.Length - 1;
			if(req.getHeader("range") != null)
			{
				statusCode = "206 Partial Content";

				// need to determine what bytes they want
				string r = req.getHeader("range");
				string[] firstSplit = r.Split('=');
				//firstSplit[1] now has the range

				string[] range = firstSplit[1].Split(new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries);

				//now range should have two pieces if it is a range
				// one piece if it is requesting from front or back of file
				// determine where the - is to figure out which, this is stored in firstSplit[1] still
				long a = 0;
				long b = 0;
				if(range.Length == 2)
				{
					long.TryParse(range[0], out a);
					long.TryParse(range[1], out b);
					rangeBegin = a;
					rangeEnd = b;
				}
				else
				{
					if(firstSplit[1][0] == '-')
					{
						//wants the back of the file
						long.TryParse(range[0], out a);
						rangeBegin = (fileStream.Length - 1 - a);
						rangeEnd = (fileStream.Length - 1);

					}
					else
					{
						// wants the front of the file
						long.TryParse(range[0], out a);
						rangeBegin = a;
						rangeEnd = (fileStream.Length - 1);

					}
				}
				contentLength = rangeEnd - rangeBegin;

			}
			// determine type of file
			if(ext == ".jpeg" || ext == ".jpg")
			{
				contentType = "image/jpeg";
			}
			else if(ext == ".png")
			{
				contentType = "image/png";
			}
			else if(ext == ".pdf")
			{
				contentType = "application/pdf";
			}
			else if(ext == ".mp4")
			{
				contentType = "video/mp4";
			}
			else if(ext == ".txt")
			{
				contentType = "text/plain";
			}
			else if(ext == ".html")
			{
				contentType = "text/html";
			}
			else if(ext == ".xml")
			{
				contentType = "application/xml";
			}

			if (contentLength == 0)
			{
				contentLength = 1;
			}

			resp.Append("HTTP/1.1 "+statusCode+"\r\nContent-Type:"+contentType+"\r\n"+"Content-Length:"+contentLength.ToString()+"\r\nAccept-Ranges: bytes\r\n");

			if(req.getHeader("range") != null)
			{
				// add on content-range
				resp.Append("Content-Range: bytes "+rangeBegin.ToString()+"-"+rangeEnd.ToString()+"/"+fileStream.Length.ToString()+"\r\n");
			}
			resp.Append("\r\n");

			Console.WriteLine("RESPONSE");
			Console.WriteLine(resp.ToString());

			req.WriteFileResponse(Encoding.ASCII.GetBytes(resp.ToString()));
	
			if(req.getHeader("range") != null)
			{
				
				fileStream.Position = rangeBegin;
				while(bytesRead != 0)
				{
					try{
					int readAmmount = Convert.ToInt32(Math.Max(buffer.Length, fileStream.Position - rangeEnd));
					bytesRead = fileStream.Read(buffer, 0, readAmmount);
					req.WriteFileResponse(buffer);
					}
					catch{
					}

				}
			}
			else
			{
				while(bytesRead != 0)
				{
					try{
					bytesRead = fileStream.Read(buffer, 0, buffer.Length);
					req.WriteFileResponse(buffer);
					}
					catch{
					}
				}
			}

			return;
		}
	}
}