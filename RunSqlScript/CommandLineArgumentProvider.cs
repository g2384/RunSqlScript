using System;
using System.Linq;

namespace RunSqlScript
{
    internal sealed class CommandLineArgumentProvider
    {
        private readonly string[] _args = Environment.GetCommandLineArgs();

        public string[] GetSqlFiles()
        {
            return _args.Skip(2).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        }

        public string GetConnectionString()
        {
            return _args.Length > 1 ? _args.Skip(1).Take(1).First() : null;
        }
    }
}
