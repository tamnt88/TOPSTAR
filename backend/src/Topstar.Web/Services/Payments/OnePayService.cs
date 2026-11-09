using System.Globalization;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Payments;

public sealed record OnePayResult(bool Success, string Message, string? RedirectUrl = null);

public interface IOnePayService
{
    Task<OnePayResult> CreatePaymentUrlAsync(Guid brandUserId, Guid fundingId, string cardList, string clientIp, CancellationToken ct = default);
    Task<OnePayResult> ProcessResponseAsync(IReadOnlyDictionary<string, StringValues> values, CancellationToken ct = default);
}

public sealed class OnePayService(TopstarDbContext db, IDataProtectionProvider protection) : IOnePayService
{
    private static readonly HashSet<string> CardLists = new(StringComparer.Ordinal) { "DOMESTIC", "INTERNATIONAL", "QR" };

    public async Task<OnePayResult> CreatePaymentUrlAsync(Guid brandUserId, Guid fundingId, string cardList, string clientIp, CancellationToken ct = default)
    {
        cardList = (cardList ?? string.Empty).Trim().ToUpperInvariant();
        var config = await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is null || !config.OnePayEnabled) return new(false, "Thanh toán OnePAY hiện chưa được bật.");
        if (!CardLists.Contains(cardList) || !ChannelEnabled(config, cardList)) return new(false, "Kênh thanh toán OnePAY không hợp lệ hoặc đang tạm khóa.");
        if (string.IsNullOrWhiteSpace(config.OnePayMerchantId) || string.IsNullOrWhiteSpace(config.OnePayAccessCode) || string.IsNullOrWhiteSpace(config.OnePayBaseUrl) || string.IsNullOrWhiteSpace(config.OnePaySecureHashCiphertext))
            return new(false, "Cấu hình OnePAY chưa đầy đủ. Vui lòng liên hệ Topstar.");

        var funding = await (from f in db.CampaignFundings.AsNoTracking()
                             join c in db.Campaigns.AsNoTracking() on f.CampaignId equals c.Id
                             join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                             where f.Id == fundingId && b.UserId == brandUserId
                             select f).FirstOrDefaultAsync(ct);
        if (funding is null || funding.PaymentMethod != CampaignPaymentMethods.OnePay || funding.Status != CampaignFundingStatuses.Pending || string.IsNullOrWhiteSpace(funding.OrderNumber))
            return new(false, "Yêu cầu thanh toán không tồn tại hoặc không còn ở trạng thái chờ thanh toán.");

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vpc_AccessCode"] = config.OnePayAccessCode,
            ["vpc_Amount"] = decimal.Round(funding.TotalPayableSnapshot * 100, 0).ToString("0", CultureInfo.InvariantCulture),
            ["vpc_CallbackURL"] = config.OnePayCallbackUrl,
            ["vpc_CardList"] = cardList,
            ["vpc_Command"] = "pay",
            ["vpc_Locale"] = "vn",
            ["vpc_MerchTxnRef"] = funding.OrderNumber,
            ["vpc_Merchant"] = config.OnePayMerchantId,
            ["vpc_OrderInfo"] = funding.OrderNumber,
            ["vpc_ReturnURL"] = config.OnePayReturnUrl,
            ["vpc_TicketNo"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
            ["vpc_Version"] = "2"
        };
        parameters["vpc_SecureHash"] = Sign(parameters, Unprotect(config.OnePaySecureHashCiphertext));
        var url = QueryHelpers.AddQueryString(config.OnePayBaseUrl, parameters!);
        return new(true, "Đang chuyển đến cổng thanh toán OnePAY.", url);
    }

    public async Task<OnePayResult> ProcessResponseAsync(IReadOnlyDictionary<string, StringValues> values, CancellationToken ct = default)
    {
        var config = await db.PaymentMethodConfigurations.FirstOrDefaultAsync(ct);
        if (config is null || !config.OnePayEnabled || string.IsNullOrWhiteSpace(config.OnePaySecureHashCiphertext)) return new(false, "OnePAY chưa được cấu hình.");
        var suppliedHash = Value(values, "vpc_SecureHash");
        var signedValues = values.Where(x => (x.Key.StartsWith("vpc_", StringComparison.Ordinal) || x.Key.StartsWith("user_", StringComparison.Ordinal)) && !x.Key.Equals("vpc_SecureHash", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);
        var expectedHash = Sign(signedValues, Unprotect(config.OnePaySecureHashCiphertext));
        if (string.IsNullOrWhiteSpace(suppliedHash) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expectedHash), Encoding.ASCII.GetBytes(suppliedHash.ToUpperInvariant())))
            return new(false, "Chữ ký phản hồi OnePAY không hợp lệ.");
        if (!string.Equals(Value(values, "vpc_Merchant"), config.OnePayMerchantId, StringComparison.Ordinal)) return new(false, "Merchant OnePAY không hợp lệ.");

        var orderNumber = Value(values, "vpc_MerchTxnRef");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var funding = await db.CampaignFundings.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, ct);
        if (funding is null) return new(false, "Không tìm thấy yêu cầu thanh toán.");
        if (!decimal.TryParse(Value(values, "vpc_Amount"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) || amount != decimal.Round(funding.TotalPayableSnapshot * 100, 0))
            return new(false, "Số tiền OnePAY trả về không khớp yêu cầu thanh toán.");
        if (funding.Status == CampaignFundingStatuses.Paid) return new(true, "Giao dịch đã được ghi nhận trước đó.");
        if (funding.Status != CampaignFundingStatuses.Pending) return new(false, "Yêu cầu thanh toán không còn hiệu lực.");
        if (Value(values, "vpc_TxnResponseCode") != "0") return new(false, "Giao dịch OnePAY chưa thành công.");

        var now = DateTime.UtcNow;
        var reference = Value(values, "vpc_TransactionNo");
        funding.Status = CampaignFundingStatuses.Paid;
        funding.PaidAtUtc = now;
        funding.PaymentReference = string.IsNullOrWhiteSpace(reference) ? orderNumber : reference;
        funding.UpdatedAtUtc = now;
        var campaign = await db.Campaigns.FirstAsync(x => x.Id == funding.CampaignId, ct);
        if (campaign.Status == Topstar.Application.Campaigns.CampaignStatuses.Published)
        {
            campaign.Status = Topstar.Application.Campaigns.CampaignStatuses.InProgress;
            campaign.UpdatedAtUtc = now;
            db.CampaignStatusHistories.Add(new CampaignStatusHistoryEntity { Id = Guid.NewGuid(), CampaignId = campaign.Id, FromStatus = Topstar.Application.Campaigns.CampaignStatuses.Published, ToStatus = Topstar.Application.Campaigns.CampaignStatuses.InProgress, Note = "Đã thanh toán OnePAY và ký quỹ đầy đủ.", CreatedAtUtc = now });
        }
        db.CampaignFundLedgers.Add(new CampaignFundLedgerEntity { Id = Guid.NewGuid(), FundingId = funding.Id, EntryType = "PaidOnePay", Amount = funding.TotalPayableSnapshot, Note = funding.PaymentReference, CreatedAtUtc = now });
        db.AuditLogs.Add(new AuditLogEntity { EntityName = "CampaignFunding", EntityId = funding.Id.ToString(), ActionName = "OnePayPaid", PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { orderNumber, reference }), CreatedAtUtc = now });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "Thanh toán OnePAY thành công. Chiến dịch đã được ký quỹ.");
    }

    private string Unprotect(string value) => protection.CreateProtector("Topstar.OnePay.SecureHash.v1").Unprotect(value);
    private static bool ChannelEnabled(PaymentMethodConfigurationEntity c, string value) => value switch { "DOMESTIC" => c.OnePayDomesticEnabled, "INTERNATIONAL" => c.OnePayInternationalEnabled, "QR" => c.OnePayEWalletEnabled, _ => false };
    private static string Value(IReadOnlyDictionary<string, StringValues> values, string key) => values.TryGetValue(key, out var value) ? value.ToString() : string.Empty;
    private static string Sign(IEnumerable<KeyValuePair<string, string>> values, string hexSecret)
    {
        var hashData = string.Join('&', values.Where(x => x.Key.StartsWith("vpc_", StringComparison.Ordinal) || x.Key.StartsWith("user_", StringComparison.Ordinal)).OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"));
        using var hmac = new HMACSHA256(Convert.FromHexString(hexSecret.Trim()));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData)));
    }
}
