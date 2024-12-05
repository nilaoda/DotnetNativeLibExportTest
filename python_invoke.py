import ctypes
import os

# 加载编译好的 DLL
dll = ctypes.CDLL(os.path.join(os.path.dirname(os.path.realpath(__file__)), "SaveDialogLib.dll"))

# 定义函数签名
dll.ShowSaveFileDialog.argtypes = [ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p]
dll.ShowSaveFileDialog.restype = ctypes.POINTER(ctypes.c_char)  # 返回 c_char*，类似于字符串

# 调用非托管方法 字符串以字节形式传递
initial_dir = b"D:"
default_file = b"default.txt"
filter = b"Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
title = b"Save File Dialog"

# 调用保存文件对话框
result_ptr = dll.ShowSaveFileDialog(initial_dir, default_file, filter, title)

# 转换返回值
if result_ptr:
    # 解码返回的字符串
    result = ctypes.cast(result_ptr, ctypes.c_char_p).value.decode("utf-8")
    print(f"Selected File: {result}")
else:
    print("No file selected.")