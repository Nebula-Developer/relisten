using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace relisten;

public static class Program {
    public static List<Tuple<string, DateTime>> Files = new List<Tuple<string, DateTime>>();
    public static List<string> DirectoryIgnore = new List<string>() { "bin", "obj", ".git" };
    public static List<string> FileIgnore = new List<string>() { };
    public static String ExecCommand = "help";

    public static bool SearchDirectory(String directory) {
        string[] files = Directory.GetFiles(directory);
        bool foundAny = false;
        for (int i = 0; i < files.Length; i++) {
            if (FileIgnore.Contains(Path.GetFileName(files[i]))) continue;
            // Check if Files has the file already
            int found = Files.FindIndex(x => x.Item1 == files[i]);

            if (found != -1) {
                if (File.GetLastWriteTime(files[i]) > Files[found].Item2) {
                    Files[found] = new Tuple<string, DateTime>(files[i], File.GetLastWriteTime(files[i]));
                    foundAny = true;
                }
            } else {
                Files.Add(new Tuple<string, DateTime>(files[i], File.GetLastWriteTime(files[i])));
            }
        }

        string[] directories = Directory.GetDirectories(directory);
        for (int i = 0; i < directories.Length; i++) {
            if (DirectoryIgnore.Contains(Path.GetFileName(directories[i]))) continue;
            SearchDirectory(Path.Combine(directory, Path.GetFileName(directories[i]) ?? "None"));
        }

        return foundAny;
    }

    static string reset = "\x1b[0m";
    static string red = "\x1b[38;5;9m";
    static string green = "\x1b[38;5;10m";
    static string yellow = "\x1b[38;5;11m";
    static string cyan = "\x1b[38;5;14m";

    public static string Bordered(string text, int color) {
        string border = new string('-', 5);
        string colored = "\x1b[38;5;" + color + "m";
        return colored + border + ' ' + text + ' ' + border + reset;
    }

    public static string Border(int color) {
        string border = new string('-', 10);
        string colored = "\x1b[38;5;" + color + "m";
        return colored + border + reset;
    }

    public static Process process = new Process();
    public static void OnExit() {
        int exitCode = process.ExitCode;

        string output = "";
        string error = "";
        if (output != "") {
            Console.WriteLine(Bordered("Output", 10));
            Console.WriteLine(output);
        }
        if (error != "") {
            Console.WriteLine(Bordered("Error", 9));
            if (error[error.Length - 1] == '\n')
                error = error.Substring(0, error.Length - 1);

            Console.WriteLine(error);
            Console.WriteLine(red + "-----------------\n" + reset);
        }

        if (exitCode == 1) {
            Console.WriteLine(red + "[ Process returned error code (1) ]" + reset);
            Console.WriteLine(yellow + "Waiting for file changes before restarting.." + reset);
        } else {
            Console.WriteLine(green + "[ Process exited successfully ]" + reset);
            Console.WriteLine(cyan + "Waiting for file changes before restarting.." + reset);
        }
    }

    public static void GotOutput(object? sender, DataReceivedEventArgs e) {
        Console.WriteLine(e.Data);
    }

    public static void GotError(object? sender, DataReceivedEventArgs e) {
        Console.WriteLine(red + e.Data + reset);
    }

    static string directory = ".";
    
    public static void Main(String[] args) {
        while(true) {
            if (SearchDirectory(directory)) {
                try {
                    if (!process.HasExited) {
                        process.Kill();
                        process.WaitForExit();
                        process.Dispose();
                        OnExit();
                    }
                } catch { }

                StartProc();
            }
            System.Threading.Thread.Sleep(100);
        }
    }

    public static void StartProc() {
        process = new Process();
        process.OutputDataReceived += GotOutput;
        process.ErrorDataReceived += GotError;

        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/C " + ExecCommand;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        process.Start();
        Console.WriteLine(green + "[ Process started (Call from file change) ]" + reset);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }
}