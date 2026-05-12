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
