using System;
using System.IO;

namespace TemporalDecompositionExample
{
    public class FileProcessor
    {
        private FileStream _stream;
        private StreamReader _reader;
        private bool _isOpen = false;
        private bool _isClosed = false;

        public void Open(string path)
        {
            if (_isOpen) throw new InvalidOperationException("Already open");
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            _reader = new StreamReader(_stream);
            _isOpen = true;
        }

        public string ReadLine()
        {
            if (!_isOpen) throw new InvalidOperationException("File not open");
            if (_isClosed) throw new InvalidOperationException("File already closed");
            return _reader.ReadLine();
        }

        public void Close()
        {
            if (!_isOpen) throw new InvalidOperationException("File not open");
            if (_isClosed) throw new InvalidOperationException("Already closed");
            _reader.Close();
            _stream.Close();
            _isClosed = true;
        }
    }

    public class Example
    {
        public void ProcessFile(string path)
        {
            var processor = new FileProcessor();
            processor.Open(path);
            string line;
            while ((line = processor.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
            processor.Close();
        }
    }
}
