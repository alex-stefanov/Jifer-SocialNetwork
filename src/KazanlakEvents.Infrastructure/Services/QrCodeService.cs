using KazanlakEvents.Application.Common.Interfaces;
using QRCoder;

namespace KazanlakEvents.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public Task<byte[]> GenerateQrCodePngAsync(string data)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        // 10 pixels per module → ~300 px for typical Guid-length QR codes
        var bytes = qrCode.GetGraphic(10);
        return Task.FromResult(bytes);
    }

    public async Task<string> GenerateQrCodeBase64Async(string data)
    {
        var bytes = await GenerateQrCodePngAsync(data);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
