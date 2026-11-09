using Topstar.Application.Notifications;
namespace Topstar.Application.Tests;
public sealed class NotificationTemplateRuleTests
{
 [Fact]public void Render_ReplacesBothSupportedTokens()=>Assert.Equal("Mới: A / B",NotificationTemplateRules.Render("Mới: {Title} / {Body}","A","B",200));
 [Fact]public void Render_ClampsToDatabaseLength()=>Assert.Equal(5,NotificationTemplateRules.Render("{Body}","",new string('x',10),5).Length);
 [Theory][InlineData("{Title} {Body}",true)][InlineData("Tĩnh",true)][InlineData("{UserName}",false)][InlineData("{Title",false)]
 public void PlaceholderValidation(string value,bool expected)=>Assert.Equal(expected,NotificationTemplateRules.HasOnlySupportedPlaceholders(value));
}
