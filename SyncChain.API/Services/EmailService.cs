using System.Net;
using System.Net.Mail;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void SendOtp(string toEmail, string otp)
    {
        var section = _config.GetSection("Email");
        var host = section["SmtpHost"];
        var port = int.Parse(section["SmtpPort"] ?? "587");
        var user = section["Username"] ?? "";
        var pass = section["Password"] ?? "";
        var from = section["FromAddress"] ?? "no-reply@syncchain.vn";
        var fromName = section["FromName"] ?? "SyncChain";

        // Log OTP để dev test khi chưa cấu hình SMTP
        _logger.LogInformation("[OTP] {Email} → {Otp} (hết hạn 5 phút)", toEmail, otp);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            return;

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };

            var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = "[SyncChain] Mã OTP đặt lại mật khẩu",
                Body = $"Mã OTP của bạn là: <b>{otp}</b><br/>Mã có hiệu lực trong 5 phút. Không chia sẻ mã này với ai.",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            client.Send(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Gửi email thất bại: {Message}", ex.Message);
        }
    }

    public void SendOrderConfirmation(string toEmail, string name, int orderId, decimal total, string method)
    {
        var subject = $"[SyncChain] Xác nhận đơn hàng #{orderId}";
        var body = BuildTemplate(
            $"Đơn hàng #{orderId} đã được xác nhận",
            $"Xin chào <b>{name}</b>,<br/><br/>" +
            $"Đơn hàng <b>#{orderId}</b> của bạn đã được xác nhận thành công.<br/>" +
            $"Tổng tiền: <b>{total:N0} đ</b> | Thanh toán: <b>{method.ToUpperInvariant()}</b><br/><br/>" +
            "Chúng tôi sẽ liên hệ khi đơn hàng sẵn sàng giao.");
        Send(toEmail, subject, body);
    }

    public void SendPaymentResult(string toEmail, string name, int orderId, bool success, string method)
    {
        var subject = success
            ? $"[SyncChain] Thanh toán thành công đơn #{orderId}"
            : $"[SyncChain] Thanh toán thất bại đơn #{orderId}";
        var body = BuildTemplate(
            success ? "Thanh toán thành công" : "Thanh toán thất bại",
            $"Xin chào <b>{name}</b>,<br/><br/>" +
            (success
                ? $"Thanh toán đơn hàng <b>#{orderId}</b> qua <b>{method.ToUpperInvariant()}</b> thành công."
                : $"Thanh toán đơn hàng <b>#{orderId}</b> qua <b>{method.ToUpperInvariant()}</b> không thành công. Vui lòng thử lại."));
        Send(toEmail, subject, body);
    }

    public void SendOrderStatusChanged(string toEmail, string name, int orderId, string newStatus)
    {
        var subject = $"[SyncChain] Cập nhật trạng thái đơn #{orderId}";
        var statusVi = newStatus switch
        {
            OrderStatuses.Pending    => "Chờ xử lý",
            OrderStatuses.Processing => "Đang xử lý",
            OrderStatuses.Shipping   => "Đang giao",
            OrderStatuses.Done       => "Hoàn tất",
            OrderStatuses.Cancel     => "Đã hủy",
            _                        => newStatus
        };
        var body = BuildTemplate(
            $"Đơn hàng #{orderId}: {statusVi}",
            $"Xin chào <b>{name}</b>,<br/><br/>" +
            $"Trạng thái đơn hàng <b>#{orderId}</b> đã cập nhật: <b>{statusVi}</b>.");
        Send(toEmail, subject, body);
    }

    private void Send(string toEmail, string subject, string htmlBody)
    {
        var section  = _config.GetSection("Email");
        var host     = section["SmtpHost"];
        var port     = int.Parse(section["SmtpPort"] ?? "587");
        var user     = section["Username"] ?? "";
        var pass     = section["Password"] ?? "";
        var from     = section["FromAddress"] ?? "no-reply@syncchain.vn";
        var fromName = section["FromName"] ?? "SyncChain";

        _logger.LogInformation("[Email] To={Email} Subject={Subject}", toEmail, subject);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            return;

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };
            var message = new MailMessage
            {
                From       = new MailAddress(from, fromName),
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);
            client.Send(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Gửi email thất bại: {Message}", ex.Message);
        }
    }

    private static string BuildTemplate(string title, string content) =>
        $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
          <div style="background:#213145;padding:20px 24px">
            <h2 style="color:#fff;margin:0">SyncChain</h2>
          </div>
          <div style="padding:24px;background:#f9fafb">
            <h3 style="color:#213145">{title}</h3>
            <p style="color:#374151;line-height:1.6">{content}</p>
          </div>
          <div style="padding:12px 24px;background:#e5e7eb;font-size:12px;color:#6b7280">
            © 2025 SyncChain. Không trả lời email này.
          </div>
        </div>
        """;
}
