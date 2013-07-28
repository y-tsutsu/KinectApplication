using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace StandAloneComplex
{
    static class TraceLogger
    {
        /// <summary>
        /// トレースファイル名
        /// </summary>
        private static readonly string Filename = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + ".log";

        /// <summary>
        /// トレースソース
        /// </summary>
        private static TraceSource traceSource = new TraceSource(Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]), SourceLevels.All);

        /// <summary>
        /// トレースリスナー
        /// </summary>
        private static TextWriterTraceListener listener = new TextWriterTraceListener(TraceLogger.Filename, "LogFile");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        static TraceLogger()
        {
            File.Delete(TraceLogger.Filename);
            listener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId;
            traceSource.Listeners.Add(listener);
        }

        /// <summary>
        /// トレースを出力する（Error）
        /// </summary>
        /// <param name="message"></param>
        public static void WriteError(string message)
        {
            traceSource.TraceEvent(TraceEventType.Error, 0, message);
            traceSource.Flush();
        }

        /// <summary>
        /// トレースを出力する（Warning）
        /// </summary>
        /// <param name="message"></param>
        public static void WriteWarning(string message)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0, message);
            traceSource.Flush();
        }

        /// <summary>
        /// トレースを出力する（Information）
        /// </summary>
        /// <param name="message"></param>
        public static void WriteInformation(string message)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, message);
            traceSource.Flush();
        }
    }
}
