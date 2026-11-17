// ContentHealthDashboard.razor.js -- hands the "Export to xlsx" workbook (built on the server,
// Services/ContentHealthExport.cs) to the browser as a download. Nothing is sent anywhere.
export async function downloadFile(fileName, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.target = '_self';   // App.razor sends links inside a frame to the whole window; a download stays where it is
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Copies the API key to the clipboard. Returns false if the browser refused (the key is still on screen to copy by hand).
export async function copyText(text) {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
}
