using System.Runtime.InteropServices;
using NativeLibTest;

class Program
{
    [DllImport("SaveDialogLib.dll", EntryPoint = "ShowSaveFileDialog", CharSet = CharSet.Ansi)]
    public static extern IntPtr ShowSaveFileDialog(string initialDirectory, string defaultFileName, string filter, string title);

    [STAThread]
    static void Main(string[] args)
    {
        // LibTest();
        NativeLibTest();
    }

    static void NativeLibTest()
    {
        IntPtr resultPtr = ShowSaveFileDialog(
                initialDirectory: @"D:",
                defaultFileName: "newFile.txt",
                filter: "Text Files|*.txt|All Files|*.*",
                title: "Save As"
            );

        if (resultPtr != IntPtr.Zero)
        {
            string result = Marshal.PtrToStringUTF8(resultPtr) ?? string.Empty;
            Console.WriteLine($"Selected file path: {result}");
            Marshal.FreeCoTaskMem(resultPtr);
        }
        else
            Console.WriteLine("You canceled!");
    }

    static void LibTest()
    {
        string? result = SaveDialogLib.ShowSaveFileDialog(
                initialDirectory: @"D:",
                defaultFileName: "newFile.txt",
                filter: "Text Files|*.txt|All Files|*.*",
                title: "Save As"
            );

        if (result is not null)
            Console.WriteLine($"Selected file path: {result}");
        else
            Console.WriteLine("You canceled!");
    }
}