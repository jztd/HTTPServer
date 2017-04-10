using System;
using System.IO;
namespace CS422
{
	public class ConcatStream : Stream
	{
		private Stream _stream1;
		private Stream _stream2;
		private long _length;
		private long _position;
		private long _stream2Pos;

		public ConcatStream( Stream first, Stream second)
		{
			long stream1Length;
			long stream2Length;
			_stream1 = first;
			_stream2 = second;
			_position = 0;
			_stream2Pos = 0;
			try
			{
				stream1Length = first.Length;
			}
			catch(NotSupportedException ex)
			{
				throw new System.ArgumentException("First stream must support length");
			}
			try
			{
				stream2Length = second.Length;
				_length = stream1Length+stream2Length;
			}
			catch(NotSupportedException ex)
			{
				_length = -1;
			}
		}

		public ConcatStream(Stream first, Stream second, long fixedLength)
		{
			long stream1Length;
			_stream1 = first;
			_stream2 = second;
			_length = fixedLength;

			try
			{
				stream1Length = first.Length;
			}
			catch(NotSupportedException ex)
			{
				throw new System.ArgumentException("First stream must support length");
			}
		}

		public override long Length
		{
			get
			{
				if(_length >= 0)
				{
					return _length;
				} 
				else
				{
					return 0;
				}
			}
		}

		public override long Position
		{
			get
			{
				return _position;
			}
			set
			{
				if(this.CanSeek)
				{
					_position = value;
				}
			}
		}

		public override bool CanSeek
		{
			get
			{
				return (_stream1.CanSeek && _stream2.CanSeek);
			}
		}

		public override bool CanRead
		{
			get
			{
				return (_stream1.CanRead && _stream2.CanRead);
			}
		}

		public override bool CanWrite
		{
			get
			{
				return (_stream1.CanWrite && _stream2.CanWrite);
			}
		}
		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
//			long newOffset = 0;
//			// our starting place is the begining of the whole show
//			if(origin == SeekOrigin.Begin)
//			{
//				//our new starting place realtive to our stream is
//				newOffset = offset;
//
//				// do we need to change stream 1 at all?
//
//				if(newOffset < (_position - _stream2Pos))
//				{
//					_stream2.Seek(0, origin);
//					_stream1.Seek(((_position - _stream2Pos) * -1) + offset, SeekOrigin.Current);
//
//					_stream2Pos = 0;
//					_position = offset;
//				}
//
//				// else all we are doing is changing stream 2
//				else
//				{
//					newOffset = offset - (_position - _stream2Pos);
//					_stream2.Seek(newOffset, origin);
//					_stream2Pos
//				}
//			}
//			else if(origin == SeekOrigin.Current)
//			{
//				// our reference point is the current place in the stream
//			}
//
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			 
			int bytesRead = 0;

			// first check to see if we have any left in the first stream
			if(_stream1.Length > 0)
			{
				// we still have data to read in the first stream
				// but how much
				long stream1Left = _stream1.Length - _stream1.Position;

				if(stream1Left > count)
				{

					// enough of stream1 is left so we just read from stream one
					bytesRead = _stream1.Read(buffer, offset, count);

				} 
				else
				{
					
					// not enough of stream 1 is left so we read what we can then read the rest from stream2
					//first we need to read from stream 1 as much as we can
					bytesRead = _stream1.Read(buffer,offset,count);

					// now we need to figure out how much is left and read that from stream2
					int countLeft = count - bytesRead;
					int secondRead = 0;
					// move the offest to the end of the data we already read  from stream1 and now read the rest of count
					secondRead = _stream2.Read(buffer, offset + bytesRead, countLeft);
					bytesRead += secondRead;
					_stream2Pos += secondRead;
				}

			} 
			else
			{
				// nothing left in stream1 just read from stream2
				bytesRead = _stream2.Read(buffer,offset,count);
				_stream2Pos += bytesRead;
			}

			this._position += bytesRead;
			return bytesRead;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			long leftInStreamOne = _stream1.Length;

			// first we need to know if we can write at all
			long totalSpace = _stream1.Length + _stream2Pos;
			if(totalSpace >= count)
			{
				if(leftInStreamOne > 0)
				{
					// okay there is room left in stream one, see if it will all fit
					if(leftInStreamOne > count)
					{
						// it all fit, we are done
						_stream1.Write(buffer, offset, count);

					} 
					else
					{
						// it wouldn't all fit so we put some there and some in stream2;
						_stream1.Write(buffer, offset, (int)leftInStreamOne);
						//now write the rest to stream2
						_stream2.Write(buffer, (int)(offset + leftInStreamOne), (int)(count - leftInStreamOne));
						_stream2Pos += (count - leftInStreamOne);
					}
				} 
				else
				{
					// else put the rest of it into stream 2
					_stream2.Write(buffer, offset, count);
					_stream2Pos += count;
				}
			}


			this._position += count;
		}
			
	}
}

