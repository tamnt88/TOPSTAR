using System.Security.Cryptography;
using System.Text;
using Topstar.Application.Billing;

namespace Topstar.Application.Tests;

public sealed class VnPayProtocolTests
{
    [Fact]
    public void Canonical_orders_fields_encodes_spaces_and_excludes_signature()
    {
        var values=new Dictionary<string,string>{{"vnp_TxnRef","P123"},{"vnp_OrderInfo","Mua gói & KOC"},{"vnp_Amount","10000"},{"vnp_SecureHash","ignored"},{"vnp_SecureHashType","SHA512"},{"other","ignored"}};
        Assert.Equal("vnp_Amount=10000&vnp_OrderInfo=Mua+g%C3%B3i+%26+KOC&vnp_TxnRef=P123",VnPayProtocol.Canonical(values));
    }
    [Fact]
    public void Signature_verifies_but_tampered_amount_and_secret_fail()
    {
        var values=new Dictionary<string,string>{{"vnp_Amount","10000"},{"vnp_TxnRef","P123"}};
        values["vnp_SecureHash"]=VnPayProtocol.Sign(values,"test-secret");
        Assert.True(VnPayProtocol.Verify(values,"test-secret"));
        Assert.False(VnPayProtocol.Verify(values,"different-secret"));
        values["vnp_Amount"]="999999";
        Assert.False(VnPayProtocol.Verify(values,"test-secret"));
    }
    [Fact]
    public void Signature_uses_utf8_secret_and_hmac_sha512()
    {
        var values=new Dictionary<string,string>{{"vnp_Amount","10000"}};
        var expected=Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes("secret"),Encoding.UTF8.GetBytes("vnp_Amount=10000"))).ToLowerInvariant();
        Assert.Equal(expected,VnPayProtocol.Sign(values,"secret"));
    }
    [Theory]
    [InlineData("x")]
    [InlineData("")]
    [InlineData("ffffffff")]
    public void Invalid_signature_is_rejected(string hash)
        => Assert.False(VnPayProtocol.Verify(new Dictionary<string,string>{{"vnp_SecureHash",hash}},"secret"));
    [Theory]
    [InlineData("100000",1000,true)]
    [InlineData("100001",1000,false)]
    [InlineData("999999999",1000,false)]
    [InlineData("-100000",1000,false)]
    [InlineData("100000.0",1000,false)]
    [InlineData("0",0,false)]
    [InlineData("100000",1000.5,false)]
    public void Gateway_amount_must_exactly_match_server_snapshot(string raw,double expected,bool valid)
        => Assert.Equal(valid,VnPayProtocol.AmountMatches(raw,(decimal)expected));
}
