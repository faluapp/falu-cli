using Xunit;

namespace Falu.Tests;

public class ConstantsTests
{
    [Fact]
    public void MaxStatementFileSizeString_IsCorrect()
    {
        Assert.Equal("256 KiB", Constants.MaxStatementFileSizeString);
    }

    [Theory]
    [InlineData("fskl_602cd2747409e867a240d000")]
    [InlineData("fpkt_60ffe3f79c1deb8060f91312")]
    [InlineData("fskt_27e868O6xW4NYrQb3WvxDb8iW6D")]
    [InlineData("ftkt_27e868O6xW4NYrQb3WvxDb8iW6D")]
    [InlineData("ftkt_27e868O6xW4NYrQb3WvxDb8iW6D5555555")]
    public void ApiKeyFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.ApiKeyFormat, input);
    }

    [Theory]
    [InlineData("5C5F6FC9-DD6A-4C5D-A63A-DC96740CFE12")]
    [InlineData("dad11640-8f1c-4b91-8e61-051241204c8f")]
    [InlineData("pay:reload:202205091300")]
    [InlineData("pay_reload:202205091300")]
    public void IdempotencyKeyFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.IdempotencyKeyFormat, input);
    }

    [Theory]
    [InlineData("evt_602cd2747409e867a240d000")]
    [InlineData("evt_60ffe3f79c1deb8060f91312")]
    [InlineData("evt_27e868O6xW4NYrQb3WvxDb8iW6D")]
    public void EventIdFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.EventIdFormat, input);
    }

    [Theory]
    [InlineData("message.delivered")]
    [InlineData("payment_refund.*")]
    [InlineData("transfer_reversal.created")]
    [InlineData("test-test.test")]
    public void EventTypeWildcardFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.EventTypeWildcardFormat, input);
    }

    [Theory]
    [InlineData("we_602cd2747409e867a240d000")]
    [InlineData("we_60ffe3f79c1deb8060f91312")]
    [InlineData("we_27e868O6xW4NYrQb3WvxDb8iW6D")]
    public void WebhookEndpointIdFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.WebhookEndpointIdFormat, input);
    }

    [Theory]
    [InlineData("mtpl_602cd2747409e867a240d000")]
    [InlineData("tmpl_60ffe3f79c1deb8060f91312")]
    [InlineData("mtpl_27e868O6xW4NYrQb3WvxDb8iW6D")]
    public void MessageTemplateIdFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.MessageTemplateIdFormat, input);
    }

    [Theory]
    [InlineData("promo-message")]
    [InlineData("promo_message")]
    [InlineData("Birthday_Wishes_2022-05-10")]
    public void MessageTemplateAliasFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.MessageTemplateAliasFormat, input);
    }

    [Theory]
    [InlineData("+254722000000")]
    [InlineData("+254700000000")]
    [InlineData("+14155552671")]
    public void E164PhoneNumberFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.E164PhoneNumberFormat, input);
    }

    [Theory]
    [InlineData("P2W")]
    [InlineData("PT21M")]
    [InlineData("P3Y6M4DT12H30M5S")]
    [InlineData("P3,2Y6.5M4,7DT")]
    [InlineData("PT57H30.5M3,77262S")]
    public void Iso8061DurationFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.Iso8061DurationFormat, input);
    }

    [Theory]
    [InlineData("/v1/messages")]
    [InlineData("/v1/payments/*")]
    [InlineData("/v1/message_batches/*/redact")]
    public void RequestPathWildcardFormat_IsCorrect(string input)
    {
        Assert.Matches(Constants.RequestPathWildcardFormat, input);
    }
}
