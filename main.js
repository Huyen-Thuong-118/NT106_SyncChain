const { app, BrowserWindow, dialog, Menu } = require("electron");
const { spawn } = require("child_process");
const path = require("path");

let mainWin;
let serverProcess;

/**
 * Khởi động server Node.js backend
 */
const startServer = () => {
    return new Promise((resolve, reject) => {
        // Chạy server Node.js
        serverProcess = spawn("node", ["src/index.js"], {
            cwd: __dirname,
            stdio: "pipe"
        });

        serverProcess.stdout.on("data", (data) => {
            console.log(`[Server]: ${data}`);
            if (data.toString().includes("Server đang chạy")) {
                resolve();
            }
        });

        serverProcess.stderr.on("data", (data) => {
            console.error(`[Server Error]: ${data}`);
        });

        serverProcess.on("error", (err) => {
            reject(err);
        });

        // Timeout sau 10 giây
        setTimeout(() => resolve(), 10000);
    });
};

/**
 * Tạo cửa sổ chính
 */
const createWindow = () => {
    mainWin = new BrowserWindow({
        width: 1200,
        height: 800,
        minWidth: 800,
        minHeight: 600,
        icon: path.join(__dirname, "public", "icon.ico"),
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
        },
        title: "SyncChain - Hệ thống quản lý chuỗi cung ứng",
        backgroundColor: "#f5f5f5"
    });

    // Xóa menu mặc định (theo bài tham khảo)
    mainWin.removeMenu();

    // Tải web app đang chạy trên localhost
    mainWin.loadURL("http://localhost:3000/login.html");

    // Mở DevTools nếu cần debug (comment lại khi build)
    // mainWin.webContents.openDevTools();

    // Xử lý khi đóng cửa sổ
    mainWin.on("closed", () => {
        mainWin = null;
    });
};

/**
 * Khởi động ứng dụng
 */
const init = async () => {
    try {
        // Khởi động server backend
        console.log("Đang khởi động server...");
        await startServer();
        console.log("Server đã sẵn sàng!");
        
        // Mở cửa sổ desktop
        createWindow();
    } catch (error) {
        console.error("Lỗi khởi động:", error);
        dialog.showErrorBox("Lỗi", "Không thể khởi động server!");
        app.quit();
    }
};

// Sau khi Electron khởi động xong
app.whenReady().then(init);

// Xử lý khi đóng tất cả cửa sổ
app.on("window-all-closed", () => {
    // Tắt server
    if (serverProcess) {
        serverProcess.kill();
    }
    app.quit();
});

// Xử lý khi app active (MacOS)
app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});