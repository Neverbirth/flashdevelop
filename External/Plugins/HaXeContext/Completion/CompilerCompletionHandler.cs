﻿using System;
using System.Diagnostics;
using System.Threading;
using HaXeContext.Helpers;

namespace HaXeContext
{
    public class CompilerCompletionHandler : IHaxeCompletionHandler
    {
        private readonly ThreadLocal<ProcessStartInfo> haxeProcessStartInfo;

        public CompilerCompletionHandler(ProcessStartInfo haxeProcessStartInfo)
        {
            if (haxeProcessStartInfo != null)
                this.haxeProcessStartInfo = new ThreadLocal<ProcessStartInfo>(haxeProcessStartInfo.Clone);
            Environment.SetEnvironmentVariable("HAXE_SERVER_PORT", "0");
        }

        public string GetCompletion(string[] args)
        {
            return GetCompletion(args, null);
        }
        public string GetCompletion(string[] args, string fileContent)
        {
            if (args == null || haxeProcessStartInfo == null)
                return string.Empty;
            try
            {
                using (var haxeProcess = new Process())
                {
                    haxeProcess.StartInfo = haxeProcessStartInfo.Value;
                    haxeProcess.StartInfo.Arguments = String.Join(" ", args);
                    haxeProcess.EnableRaisingEvents = true;
                    haxeProcess.Start();
                    var lines = haxeProcess.StandardError.ReadToEnd();
                    return lines;
                }
            }
            catch 
            { 
                return string.Empty;
            }
        }

        public void Stop()
        {
        }
    }
}
