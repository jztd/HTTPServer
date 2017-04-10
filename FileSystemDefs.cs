using System;
using System.IO;
using System.Collections.Generic;
namespace CS422
{
	public abstract class Dir422
	{
		public abstract string Name { get; }
		public abstract Dir422 Parent { get; }


		public abstract IList<Dir422> GetDirs();
		public abstract IList<File422> GetFiles();

		public abstract bool ContainsFile(string fileName, bool recursive);
		public abstract bool ContainsDir(string dirName, bool recursive);

		public abstract Dir422 GetDir(string dirName);
		public abstract File422 GetFile(string FileName); // gets the file if it is in the immediate directory

		public abstract Dir422 CreateDir(string dirName);// if the dir exists, return the one that exists right now
		public abstract File422 CreateFile(string fileName); // if the file/dir doesn't exist create it. if the file exists it truncates the file to 0

	}

	public abstract class File422
	{
		public abstract Dir422 Parent { get; }
		public abstract string Name { get; }
		public abstract Stream OpenReadOnly(); //must return a stream that cannot write
		public abstract Stream OpenReadWrite();
	}

	public abstract class FileSys422
	{
		public abstract Dir422 GetRoot();

		public virtual bool Contains(File422 file)
		{
			return Contains(file.Parent);

		}

		public virtual bool Contains(Dir422 dir)
		{
			if (dir != null)
			{
				while (dir.Parent != null)
				{
					dir = dir.Parent;
				}
			}

			return object.ReferenceEquals(dir,GetRoot());
		}
	}

	public class StdFSDir : Dir422
	{
		private string _path;
		private Dir422 _parent;

		public StdFSDir(string path, Dir422 parent)
		{
			if (!Directory.Exists(path))
			{
				throw new ArgumentException();
			}
			_path = path;

			_parent = parent;
		}

		public override string Name
		{
			get
			{
				if(Parent != null)
				{
					return new DirectoryInfo(_path).Name;
				}
				else
				{
					return String.Empty;
				}
			}
		}

		public override Dir422 Parent
		{
			get
			{
				return _parent;
			}
		}
			
		public override Dir422 CreateDir(string dirName)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			// good name lets see if it exists

			if (!Directory.Exists(Path.Combine(_path, dirName)))
			{
				Directory.CreateDirectory(Path.Combine(_path, dirName));
			}

			return new StdFSDir(Path.Combine(_path, dirName),this);


		}

		public override File422 CreateFile(string fileName)
		{
			if(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			// good name lets see if it exists
			System.IO.File.WriteAllText(Path.Combine(_path,fileName),string.Empty);

			return new StdFSFile(Path.Combine(_path, fileName),this);
		}

		public override IList<File422> GetFiles()
		{
			List<File422> files = new List<File422>();
			foreach (string file in Directory.GetFiles(_path))
			{
				files.Add(new StdFSFile(file, this));
			}
			return files;
		}

		public override IList<Dir422> GetDirs()
		{
			List<Dir422> dirs = new List<Dir422>();
			foreach(string dir in Directory.GetDirectories(_path))
			{
				dirs.Add(new StdFSDir(dir,this));
			}
			return dirs;
		}

		public override Dir422 GetDir(string dirName)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}
			
			if(ContainsDir(dirName, false))
			{
				return new StdFSDir(Path.Combine(_path, dirName), this);
			}
			return null;
		}

		public override File422 GetFile(string FileName)
		{
			if(FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			if(ContainsFile(FileName, false))
			{
				return new StdFSFile(Path.Combine(_path, FileName), this);
			}

			return null;
		}
		public override bool ContainsDir(string dirName, bool recursive)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return false;
			}
			if (recursive == false)
			{
				return Directory.Exists(Path.Combine(_path, dirName));
			}
			else
			{
				bool found = false;
				Queue<string> list = new Queue<string>();
				if (Directory.Exists(Path.Combine(_path, dirName)))
				{
					return true;
				}
				list.Enqueue(_path);

				while (!found && list.Count > 0)
				{

					foreach (string subDirPath in Directory.GetDirectories(list.Dequeue()))
					{
						found = Directory.Exists(Path.Combine(subDirPath, dirName));
						if (!found)
						{
							foreach (string subSubPath in Directory.GetDirectories(subDirPath))
							{
								list.Enqueue(subSubPath);
							}
						}
						else
						{
							return found;
						}

					}
				}
				return found;
			}
		}

		public override bool ContainsFile(string fileName, bool recursive)
		{
			if(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return false;
			}

			if (recursive == false)
			{
				return File.Exists(Path.Combine(_path, fileName));
			}
			else
			{
				bool found = false;
				Queue<string> list = new Queue<string>();
				if (File.Exists(Path.Combine(_path, fileName)))
				{
					return true;
				}
				list.Enqueue(_path);

				while (!found && list.Count > 0)
				{

					foreach (string subDirPath in Directory.GetDirectories(list.Dequeue()))
					{
						found = File.Exists(Path.Combine(subDirPath, fileName));
						if (!found)
						{
							foreach (string subSubPath in Directory.GetDirectories(subDirPath))
							{
								list.Enqueue(subSubPath);
							}
						}
						else
						{
							return found;
						}

					}
				}
				return found;
			}
		}

	}

	public class StdFSFile : File422
	{
		private string _path;
		private Dir422 _parent;

		public StdFSFile(string path, Dir422 parent)
		{
			_path = path;
			_parent = parent;
		}

		public override Dir422 Parent
		{
			get
			{
				return _parent;
			}
		}
		public override string Name
		{
			get
			{
				return Path.GetFileName(_path);
			}
		}

		public override Stream OpenReadOnly()
		{
			try{
			return new FileStream(_path, FileMode.Open, FileAccess.Read);
			}
			catch
			{
				return null;
			}
		}

		public override Stream OpenReadWrite()
		{
			try{
			return new FileStream(_path, FileMode.Open, FileAccess.ReadWrite);
			}
			catch{
				return null;
			}
		}
	}

	public class StandardFileSystem : FileSys422
	{
		private Dir422 _root;

		public StandardFileSystem(string root)
		{
			_root = new StdFSDir(root, null);
		}
		public override Dir422 GetRoot()
		{
			return _root;
		}

		public static StandardFileSystem Create(string rootDir)
		{
			
			return new StandardFileSystem(rootDir);
		}
	}


	public class MemoryFileSystem : FileSys422
	{
		private MemFSDir _root;

		public MemoryFileSystem()
		{
			_root = new MemFSDir("/", null);
		}

		public override Dir422 GetRoot()
		{
			return _root;
		}

	}

	public class MemFSFile : File422
	{
		private object updateLock = new object();
		private string _path;
		private MemFSDir _parent;

		private MemoryStream _content;

		int readStreams = 0;
		bool writeStreamOpen = false;

		public MemFSFile(string path, MemFSDir parent)
		{
			_path = path;
			_parent = parent;
			_content = new MemoryStream();

		}

		public override Dir422 Parent
		{
			get
			{
				return _parent;
			}
		}

		public override string Name
		{
			get
			{
				return Path.GetFileName(_path); 
			}
		}

		public override Stream OpenReadOnly()
		{
			lock(updateLock)
			{
				if(writeStreamOpen == false)
				{
					readStreams++;
					var newstream = new MemoryStream();
					_content.Position = 0;
					_content.CopyTo(newstream);
					newstream.Position = 0;
					ObservableMemoryStream temp = new ObservableMemoryStream(newstream, false);
					Subscribe(temp);
					return temp;
				}
				return null;
			}
		}

		public override Stream OpenReadWrite()
		{
			lock(updateLock)
			{
				if(readStreams == 0 && !writeStreamOpen)
				{
					writeStreamOpen = true;
					_content.Position = 0;
					ObservableMemoryStream temp = new ObservableMemoryStream(_content, true);
					Subscribe(temp);
					return temp;
				}
				else
				{
					return null;
				}
			}
		}

		public void Size(int size)
		{
			_content.SetLength(size);
		}

		public void Subscribe(ObservableMemoryStream s)
		{
			s.closeHandler += new ObservableMemoryStream.CloseHandler(closeStream);
		}


		private void closeStream(bool writeStream)
		{
			lock(updateLock)
			{
				if(!writeStream)
				{
					readStreams--;
				}
				else
				{
					writeStreamOpen = false;
				}
			}
		}

	}

	public class MemFSDir : Dir422
	{
		private string _path;
		private MemFSDir _parent;

		private List<Dir422> _directories;
		private List<File422> _files;

		public MemFSDir(string path, MemFSDir parent)
		{
			_path = path;
			_parent = parent;
			_directories = new List<Dir422>();
			_files = new List<File422>();
		}

		public override string Name
		{
			get
			{
				return new DirectoryInfo(_path).Name;
			}
		}

		public override Dir422 Parent
		{
			get
			{
				return _parent;
			}
		}

		public override IList<Dir422> GetDirs()
		{
			return _directories;
		}

		public override IList<File422> GetFiles()
		{
			return _files;
		}

		public override bool ContainsDir(string dirName, bool recursive)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return false;
			}

			bool found = false;
			if(recursive == false)
			{
				for(int i = 0; i < _directories.Count && found != true; i++)
				{
					if(_directories[i].Name == dirName)
					{
						found = true;
					}
				}
				return found;
			}
			Queue<Dir422> Dirs = new Queue<Dir422>();
			Dirs.Enqueue(this);
			while(!found && Dirs.Count > 0)
			{
				//get top folder
				Dir422 temp = Dirs.Dequeue();

				//look through it's directories and see if they are what we want
				foreach(var d in temp.GetDirs())
				{
					if(dirName == d.Name)
					{
						found = true;
					}
					// enqueue all the directories so we can contiune searching deeper
					Dirs.Enqueue(d);
				}
			}
			return found;

		}

		public override bool ContainsFile(string fileName, bool recursive)
		{

			if(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return false;
			}
			bool found = false;
			if(recursive == false)
			{
				for(int i = 0; i < _files.Count && found != true; i++)
				{
					if(_directories[i].Name == fileName)
					{
						found = true;
					}
				}
				return found;
			}
			Queue<Dir422> Dirs = new Queue<Dir422>();
			Dirs.Enqueue(this);
			while(!found && Dirs.Count > 0)
			{
				//get top folder
				Dir422 temp = Dirs.Dequeue();

				//look through it's directories and see if they are what we want
				foreach(var d in temp.GetFiles())
				{
					if(fileName == d.Name)
					{
						found = true;
					}
					// enqueue all the directories so we can contiune searching deeper
					foreach(var s in temp.GetDirs())
					{
						Dirs.Enqueue(s);
					}
				}
			}
			return found;
		}

		public override Dir422 GetDir(string dirName)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			foreach(var d in _directories)
			{
				if(d.Name == dirName)
				{
					return d;
				}

			}

			return null;
		}

		public override File422 GetFile(string FileName)
		{

			if(FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}
			foreach(var d in _files)
			{
				if(d.Name == FileName)
				{
					return d;
				}

			}

			return null;
		}

		public override Dir422 CreateDir(string dirName)
		{
			if(dirName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			foreach(var d in _directories)
			{
				if(d.Name == dirName)
				{
					return d;
				}
			}
			MemFSDir temp = new MemFSDir(Path.Combine(_path, dirName), this);
			_directories.Add(temp);
			return temp;
		}

		public override File422 CreateFile(string fileName)
		{
			if(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				return null;
			}

			foreach (var f in _files)
			{
				if(f.Name == fileName)
				{
					// found file
					var T = f as MemFSFile;
					T.Size(0);
					return f;
				}
			}
			MemFSFile temp = new MemFSFile(Path.Combine(_path, fileName), this);
			_files.Add(temp);
			return temp;

		}


	}


	public class ObservableMemoryStream : MemoryStream
	{
		public event CloseHandler closeHandler;
		public delegate void CloseHandler(bool writeStream);

		private MemoryStream _stream;
		private bool _writable;
		public ObservableMemoryStream(MemoryStream stream, bool writable)
		{
			_stream = stream;
			_writable = writable;
		}

		public override void Close()
		{
			closeHandler(_writable);
			base.Close();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			if(_writable)
			{
				_stream.Write(buffer, offset, count);
			}
		}
		public override bool CanWrite
		{
			get
			{
				return _writable;
			}
		}

		public override long Seek(long offset, SeekOrigin loc)
		{
			return _stream.Seek(offset, loc);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}
		public override long Position
		{
			get
			{
				return _stream.Position;
			}
			set
			{
				_stream.Position = value;
			}
		}
	}
}
