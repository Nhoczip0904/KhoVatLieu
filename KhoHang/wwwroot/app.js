let html5QrCode;

window.startScanner = async (dotNetHelper) => {
    try {
        // Nếu đang chạy thì dừng lại trước khi tạo cái mới
        if (html5QrCode && html5QrCode.isScanning) {
            await html5QrCode.stop();
        }

        html5QrCode = new Html5Qrcode("reader");
        const config = { 
            fps: 10, 
            qrbox: { width: 250, height: 250 },
            aspectRatio: 1.0
        };

        await html5QrCode.start(
            { facingMode: "environment" },
            config,
            (decodedText) => {
                // Tách ID vật tư từ link
                const parts = decodedText.split('/');
                const id = parts[parts.length - 1];
                if (!isNaN(id)) {
                    dotNetHelper.invokeMethodAsync('OnQrScanned', parseInt(id));
                }
            },
            (errorMessage) => {
                // Bỏ qua các lỗi quét mờ
            }
        );
    } catch (err) {
        console.error("Camera error:", err);
        alert("Lỗi camera: " + err);
        dotNetHelper.invokeMethodAsync('ToggleScanner'); // Tự đóng UI nếu lỗi
    }
};

window.stopScanner = async () => {
    try {
        if (html5QrCode && html5QrCode.isScanning) {
            await html5QrCode.stop();
            console.log("Scanner stopped.");
        }
    } catch (err) {
        console.error("Stop error:", err);
    }
};

window.scanImage = (base64Data, dotNetHelper) => {
    const html5QrCodeScanner = new Html5Qrcode("reader");
    // We don't actually need to render to "reader" for file scan, but it's a safe place
    
    html5QrCodeScanner.scanFileV2(base64Data, false)
        .then(decodedText => {
            console.log("File QR Decoded:", decodedText);
            const parts = decodedText.split('/');
            const id = parts[parts.length - 1];
            if (!isNaN(id)) {
                dotNetHelper.invokeMethodAsync('OnQrScanned', parseInt(id));
            }
        })
        .catch(err => {
            console.error("File scan error:", err);
            alert("Không tìm thấy mã QR trong ảnh này. Hãy thử chụp rõ hơn!");
        });
};
window.printQR = (name, url) => {
    const printWin = window.open('', '', 'width=400,height=600');
    printWin.document.write(`
        <html>
            <head>
                <title>In mã QR - ${name}</title>
                <style>
                    body { text-align: center; font-family: 'Inter', sans-serif; padding: 40px; }
                    .qr-container { border: 2px solid #000; padding: 20px; display: inline-block; border-radius: 10px; }
                    h2 { margin: 0 0 10px 0; font-size: 24px; }
                    .info { margin-top: 10px; font-size: 14px; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class="qr-container">
                    <h2>${name}</h2>
                    <img src="${url}" style="width: 250px; height: 250px;" />
                    <div class="info">KHO VẬT LIỆU</div>
                </div>
                <script>
                    window.onload = () => {
                        setTimeout(() => {
                            window.print();
                            window.close();
                        }, 500);
                    };
                </script>
            </body>
        </html>
    `);
    printWin.document.close();
};
window.downloadExcel = (htmlContent, fileName) => {
    const template = `
        <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40">
        <head>
            <meta charset="utf-8" />
            <!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>Sheet1</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->
            <style>
                table { border-collapse: collapse; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
                .header { background-color: #10b981; color: white; font-weight: bold; text-align: center; }
                .label { font-weight: bold; background-color: #f8fafc; }
                .number { text-align: right; }
                .center { text-align: center; }
                td, th { border: 1px solid #e2e8f0; padding: 8px; }
                .title { font-size: 18px; font-weight: bold; color: #059669; }
                .footer { font-weight: bold; color: #ef4444; font-size: 14px; }
            </style>
        </head>
        <body>
            ${htmlContent}
        </body>
        </html>`;

    const blob = new Blob([template], { type: 'application/vnd.ms-excel' });
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);
    link.setAttribute("href", url);
    link.setAttribute("download", fileName + ".xls");
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
