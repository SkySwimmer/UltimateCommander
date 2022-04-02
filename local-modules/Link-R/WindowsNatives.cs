using System;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Pipes;

namespace link_r
{
    public class WindowsNatives : NativeInterace
    {
        public class WrappedPipeStream : Stream
        {
            private NamedPipeServerStream stream;
            private string file;
            public WrappedPipeStream(string file) {
                this.stream = new NamedPipeServerStream(file, PipeDirection.InOut);
                this.file = file;
            }

            public override bool CanRead => stream.CanRead;

            public override bool CanSeek => stream.CanSeek;

            public override bool CanWrite => stream.CanWrite;

            public override long Length => stream.Length;

            public override long Position { get => stream.Position; set => stream.Position = value; }

            public override void Flush()
            {
                stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                try {
                    if (!stream.IsConnected)
                        stream.WaitForConnection();
                } catch {
                    stream.Close();
                    stream = new NamedPipeServerStream(file, PipeDirection.InOut);
                    stream.WaitForConnection();
                }
                return stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                try {
                    if (!stream.IsConnected)
                        stream.WaitForConnection();
                } catch {
                    stream.Close();
                    stream = new NamedPipeServerStream(file, PipeDirection.InOut);
                    stream.WaitForConnection();
                }
                stream.Write(buffer, offset, count);
            }
        }
        public override Stream CreatePipeFile(string file)
        {
            return new WrappedPipeStream(file);
        }
    }
    public abstract class NativeInterace {
        private static NativeInterace inter = null;
        public static NativeInterace GetWindowsNativeInterface() {
            if (inter == null)
                inter = new WindowsNatives();
            return inter;
        }

        public abstract Stream CreatePipeFile(string file);
    }
}