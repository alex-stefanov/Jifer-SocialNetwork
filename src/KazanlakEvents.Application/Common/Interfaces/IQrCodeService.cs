namespace KazanlakEvents.Application.Common.Interfaces;

public interface IQrCodeService
{
    Task<byte[]> GenerateQrCodePngAsync(string data);
    Task<string> GenerateQrCodeBase64Async(string data);
}
