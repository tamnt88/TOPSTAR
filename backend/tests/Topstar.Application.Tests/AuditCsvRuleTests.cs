using Topstar.Application.Auditing;
namespace Topstar.Application.Tests;
public sealed class AuditCsvRuleTests
{
 [Theory][InlineData("=SUM(A1:A2)")][InlineData(" +cmd")][InlineData("\t@import")][InlineData("-1+2")]
 public void Escape_BlocksFormula(string value)=>Assert.StartsWith("\"'",AuditCsvRules.Escape(value));
 [Fact]public void Escape_QuotesAndFlattensNewLines()=>Assert.Equal("\"a \"\"quoted\"\" line  next\"",AuditCsvRules.Escape("a \"quoted\" line\r\nnext"));
 [Fact]public void Escape_HandlesNull()=>Assert.Equal("\"\"",AuditCsvRules.Escape(null));
}
