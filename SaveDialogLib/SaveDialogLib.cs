using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace NativeLibTest;

public unsafe static class SaveDialogLib
{
    [UnmanagedCallersOnly(EntryPoint = "ShowSaveFileDialog")]
    public unsafe static IntPtr ShowSaveFileDialog(IntPtr initialDirectory, IntPtr defaultFileName, IntPtr filter, IntPtr title)
    {
        // 将传入的 IntPtr 参数转换为字符串（UTF-8 编码）
        string? initDir = Marshal.PtrToStringUTF8(initialDirectory);
        string? defFileName = Marshal.PtrToStringUTF8(defaultFileName);
        string? filt = Marshal.PtrToStringUTF8(filter);
        string? tit = Marshal.PtrToStringUTF8(title);

        // 调用原始逻辑
        string? result = ShowSaveFileDialog(initDir, defFileName, filt, tit);

        // 将结果字符串转换为 IntPtr 并返回
        return result != null ? Marshal.StringToCoTaskMemUTF8(result) : IntPtr.Zero;
    }

    public unsafe static string? ShowSaveFileDialog(string? initialDirectory = null, string? defaultFileName = null, string? filter = null, string? title = null)
    {
        // 高DPI感知
        PInvoke.SetProcessDPIAware();

        // Initialize COM.
        HRESULT hr = PInvoke.CoInitialize();
        if (hr.Failed)
        {
            throw new InvalidOperationException($"Failed to initialize COM. Result: {hr}");
        }

        // 创建 IFileSaveDialog 实例
        hr = PInvoke.CoCreateInstance<IFileSaveDialog>(
            typeof(FileSaveDialog).GUID,
            (IUnknown*)null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            out var fileSaveDialog);
        if (hr.Failed)
        {
            throw new InvalidOperationException($"Failed to create IFileSaveDialog instance. Result: {hr}");
        }

        // 设置标题
        if (!string.IsNullOrEmpty(title))
        {
            fileSaveDialog->SetTitle(title);
        }

        // 设置默认文件名
        if (!string.IsNullOrEmpty(defaultFileName))
        {
            fileSaveDialog->SetFileName(defaultFileName);
        }

        // 设置初始目录
        if (!string.IsNullOrEmpty(initialDirectory))
        {
            // 转换初始目录为 ITEMIDLIST
            hr = PInvoke.SHCreateItemFromParsingName(
                initialDirectory,
                null,
                typeof(IShellItem).GUID,
                out void* directoryShellItemRaw);
            fileSaveDialog->SetFolder((IShellItem*)directoryShellItemRaw);
            fileSaveDialog->SetDefaultFolder((IShellItem*)directoryShellItemRaw);
        }

        // 设置过滤器
        var filters = ParseFilter(filter);
        if (filters.Length > 0)
        {
            fixed (COMDLG_FILTERSPEC* filtersPtr = filters)
            {
                fileSaveDialog->SetFileTypes((uint)filters.Length, filtersPtr);
            }
            // 默认选择第一个过滤器
            fileSaveDialog->SetFileTypeIndex(1);
            // 设置默认扩展名. *.jpg -> jpg
            var spec = filter!.Split('|')[1][2..];
            fileSaveDialog->SetDefaultExtension((char*)Marshal.StringToHGlobalUni(spec));
        }

        // 显示对话框
        try
        {
            fileSaveDialog->Show(new HWND(IntPtr.Zero));
        }
        catch (COMException)
        {
            return null;
        }

        // 获取用户选择的文件路径
        IShellItem* resultItem = null;
        fileSaveDialog->GetResult(&resultItem);
        if (resultItem == null)
        {
            return null;
        }

        // 提取路径
        PWSTR filePathPtr = default;
        try
        {
            resultItem->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filePathPtr);

            // 将 PWSTR 转换为 .NET 字符串
            string filePath = filePathPtr.ToString();
            // 使用 filePath ...
            return filePath;
        }
        finally
        {
            // 释放分配的内存
            if (filePathPtr.Value != null)
            {
                Marshal.FreeCoTaskMem((IntPtr)filePathPtr.Value);
            }
        }
    }

    // 解析过滤器字符串为 COMDLG_FILTERSPEC[]
    // Word Documents|*.docx|JPEG Files|*.jpg
    private static COMDLG_FILTERSPEC[] ParseFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
            return [];

        var tokens = filter.Split('|');
        if (tokens.Length % 2 != 0)
            return [];

        var extensions = new List<COMDLG_FILTERSPEC>();

        // 偶数为Name, 奇数为扩展名
        for (int i = 1; i < tokens.Length; i += 2)
        {
            COMDLG_FILTERSPEC extension;

            extension.pszSpec = (char*)Marshal.StringToHGlobalUni(tokens[i]);
            extension.pszName = (char*)Marshal.StringToHGlobalUni(tokens[i - 1]);
            extensions.Add(extension);
        }

        return extensions.ToArray();
    }
}