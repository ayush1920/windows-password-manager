import time
import win32gui
import win32ui
import win32process
import psutil
from PIL import Image

def find_hwnd_by_proc_name(proc_name):
    target_pids = [p.pid for p in psutil.process_iter(['name']) if p.info['name'] and proc_name.lower() in p.info['name'].lower()]
    print(f"Target PIDs for {proc_name}: {target_pids}")
    found = []
    def enum_cb(hwnd, _):
        if win32gui.IsWindowVisible(hwnd):
            _, pid = win32process.GetWindowThreadProcessId(hwnd)
            if pid in target_pids:
                rect = win32gui.GetWindowRect(hwnd)
                w = rect[2] - rect[0]
                h = rect[3] - rect[1]
                if w > 100 and h > 100:
                    found.append(hwnd)
    win32gui.EnumWindows(enum_cb, None)
    return found[0] if found else None

hwnd = find_hwnd_by_proc_name("PasswordManager")
if not hwnd:
    print("Could not find PasswordManager window!")
    exit(1)

win32gui.SetForegroundWindow(hwnd)
time.sleep(0.5)

rect = win32gui.GetWindowRect(hwnd)
x, y, r, b = rect
w = r - x
h = b - y
print(f"Found HWND {hwnd}: bounds {x},{y} to {r},{b} ({w}x{h})")

hwndDC = win32gui.GetWindowDC(hwnd)
mfcDC  = win32ui.CreateDCFromHandle(hwndDC)
saveDC = mfcDC.CreateCompatibleDC()

saveBitMap = win32ui.CreateBitmap()
saveBitMap.CreateCompatibleBitmap(mfcDC, w, h)
saveDC.SelectObject(saveBitMap)

# PrintWindow PW_RENDERFULLCONTENT (2)
win32gui.ctypes.windll.user32.PrintWindow(hwnd, saveDC.GetSafeHdc(), 2)

bmpinfo = saveBitMap.GetInfo()
bmpstr = saveBitMap.GetBitmapBits(True)
im = Image.frombuffer('RGB', (bmpinfo['bmWidth'], bmpinfo['bmHeight']), bmpstr, 'raw', 'BGRX', 0, 1)

win32gui.DeleteObject(saveBitMap.GetHandle())
saveDC.DeleteDC()
mfcDC.DeleteDC()
win32gui.ReleaseDC(hwnd, hwndDC)

output_path = "screenshot.png"
im.save(output_path)
print(f"Saved window screenshot to {output_path}")
